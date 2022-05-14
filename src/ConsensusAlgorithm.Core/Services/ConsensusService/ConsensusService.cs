using Microsoft.Extensions.Logging;
using ConsensusAlgorithm.DTO.RequestVote;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DataAccess;
using ConsensusAlgorithm.Core.ApiClient;
using ConsensusAlgorithm.Core.Mappers;
using ConsensusAlgorithm.Core.StateMachine;
using ConsensusAlgorithm.Core.Services.ServerStatusService;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using ConsensusAlgorithm.Core.Services.TimerService;

namespace ConsensusAlgorithm.Core.Services.ConsensusService
{
    public class ConsensusService : IConsensusService
    {
        private readonly IConsensusRepository _repo;
        private readonly IStateMachine _stateMachine;
        private readonly ILogger<ConsensusService> _logger;
        private readonly IList<IConsensusApiClient> _otherServers;
        private readonly ITimerService _timerService;
        private readonly IServerStatusService _status;
        private readonly object _runElectionMaxTermLock = new();
        private readonly object _sendHeartbeatMaxTermLock = new();
        private IList<LogEntry> _logs = new List<LogEntry>();

        public ConsensusService(
            IConsensusRepository repo,
            IStateMachine stateMachine,
            ILogger<ConsensusService> logger,
            IList<IConsensusApiClient> otherServers,
            ITimerService timerService,
            IServerStatusService statusService)
        {
            _repo = repo;
            _stateMachine = stateMachine;
            _logger = logger;
            _otherServers = otherServers;
            _timerService = timerService;
            _status = statusService;
        }

        public async Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest appendRequest)
        {
            var currentTerm = _repo.GetCurrentTerm();
            if (!_status.IsLeader)
            {
                return _status.HasLeader
                   ? await _otherServers.First(s => s.Id == _status.LeaderId).AppendEntriesExternalAsync(appendRequest)
                   : new AppendEntriesExternalResponse { Success = false, Term = currentTerm };
            }
            else
            {
                var prevLog = _logs.LastOrDefault();
                var prevLogIndex = prevLog != null ? prevLog.Index : -1;
                var prevLogTerm = prevLog != null ? prevLog.Term : -1;
                var nextIndex = prevLogIndex + 1;

                var entriesToAppend = appendRequest.Commands.Select(c => new LogEntry
                {
                    Command = c,
                    Term = currentTerm,
                    Index = nextIndex++
                }).OrderBy(e => e.Index).ToList();

                foreach (var entry in entriesToAppend)
                {
                    _logs.Add(entry);
                    _repo.AppendLogEntry(entry.ToLogEntity());
                    _stateMachine.Apply(entry.Command);
                }

                var serversAppendedEntries = 1;
                var tasks = _otherServers.Select(async s =>
                {
                    var response = await s.AppendEntriesAsync(new AppendEntriesRequest
                    {
                        LeaderId = _status.Id,
                        Term = currentTerm,
                        PrevLogIndex = prevLogIndex,
                        PrevLogTerm = prevLogTerm,
                        CommitIndex = prevLogIndex,
                        Entries = entriesToAppend
                    });
                    if (response.Success) Interlocked.Increment(ref serversAppendedEntries);
                });
                await Task.WhenAll(tasks);
                return new AppendEntriesExternalResponse { Success = true, Term = currentTerm };
            }
        }

        public AppendEntriesResponse AppendEntries(AppendEntriesRequest appendRequest)
        {
            var currentTerm = _repo.GetCurrentTerm();

            if (appendRequest.Term < currentTerm)
            {
                return new AppendEntriesResponse { Success = false, Term = currentTerm };
            }

            if (appendRequest.Term > currentTerm)
            {
                _repo.SetCurrentTerm(appendRequest.Term);
                currentTerm = appendRequest.Term;
            }

            // stepdown
            _status.State = ServerStatus.Follower;
            _timerService.ResetElectionTimeout();

            var prevLog = _logs.LastOrDefault();
            var prevLogTerm = prevLog != null ? prevLog.Term : -1;
            if (prevLogTerm != appendRequest.PrevLogTerm)
            {
                return new AppendEntriesResponse { Success = false, Term = currentTerm };
            }

            var isConflict = false;
            var toAppend = new List<LogEntry>();
            foreach (var entry in appendRequest.Entries.OrderBy(l => l.Index))
            {
                if (isConflict)
                {
                    toAppend.Add(entry);
                }
                else
                {
                    var existingEntry = _logs.ElementAtOrDefault(entry.Index);
                    if (existingEntry == null ||
                        existingEntry.Index != entry.Index ||
                        existingEntry.Term != entry.Term ||
                        existingEntry.Command != entry.Command)
                    {
                        isConflict = true;
                        toAppend.Add(entry);
                        _repo.RemoveStartingFrom(entry.Index);
                    }
                }
            }

            foreach (var entry in toAppend.OrderBy(e => e.Index))
            {
                _logs.Add(entry);
                _repo.AppendLogEntry(entry.ToLogEntity());
                _stateMachine.Apply(entry.Command);
            }

            return new AppendEntriesResponse { Success = true, Term = currentTerm };
        }

        public VoteResponse RequestVote(VoteRequest voteRequest)
        {
            var currentTerm = _repo.GetCurrentTerm();

            if (voteRequest.Term < currentTerm)
            {
                return new VoteResponse { VoteGranted = false, Term = currentTerm };
            }

            if (voteRequest.Term > currentTerm)
            {
                // step down
                _status.State = ServerStatus.Follower;
                _repo.SetCurrentTerm(voteRequest.Term);
                currentTerm = voteRequest.Term;
            }

            var votedFor = _repo.GetCandidateIdVotedFor(currentTerm);
            var lastLog = _logs.LastOrDefault();
            var lastLogIndex = lastLog != null ? lastLog.Index : -1;
            var lastLogTerm = lastLog != null ? lastLog.Term : -1;

            if ((votedFor == null || votedFor == voteRequest.CandidateId) &&
                voteRequest.LastLogIndex >= lastLogIndex &&
                voteRequest.LastLogTerm >= lastLogTerm)
            {
                _timerService.ResetElectionTimeout();
                _repo.SetCandidateIdVotedFor(voteRequest.CandidateId, voteRequest.Term);
                return new VoteResponse { VoteGranted = true, Term = currentTerm };
            }
            return new VoteResponse { VoteGranted = false, Term = currentTerm };
        }

        public HeartbeatResponse Heartbeat(HeartbeatRequest heartbeat)
        {
            var currentTerm = _repo.GetCurrentTerm();

            if (heartbeat.Term < currentTerm)
            {
                return new HeartbeatResponse { Success = false, Term = currentTerm };
            }

            // received from new leader -> stepdown
            if (heartbeat.Term > currentTerm)
            {
                _repo.SetCurrentTerm(heartbeat.Term);
                currentTerm = heartbeat.Term;
            }

            _status.State = ServerStatus.Follower;
            _status.LeaderId = heartbeat.LeaderId;

            // reset election timeout
            _timerService.ResetElectionTimeout();

            return new HeartbeatResponse { Success = true, Term = currentTerm };
        }

        #region IHostedService implementation

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Consensus Server running.");
            _logs = _repo.GetLogEntries().ToLogEntries();
            _timerService.Initialize(RunElectionAsync, SendHeartbeatAsync);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Consensus Server is stopping.");
            _timerService.StopAll();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timerService?.Dispose();
        }

        #endregion IHostedService implementation

        internal async void RunElectionAsync(object? state)
        {
            // Follower -> Candidate
            _status.State = ServerStatus.Candidate;

            var currentTerm = _repo.GetCurrentTerm();

            // increment current term
            _repo.SetCurrentTerm(++currentTerm);

            // vote for himself
            _repo.SetCandidateIdVotedFor(_status.Id, currentTerm);
            var votes = 1;
            var serversAlive = 1;
            var maxTermFromResponses = currentTerm;
            _timerService.ResetElectionTimeout();

            // send request vote to all other servers
            var tasks = _otherServers.Select(async s =>
            {
                var lastLog = _logs.LastOrDefault();
                var lastLogIndex = lastLog != null ? lastLog.Index : -1;
                var lastLogTerm = lastLog != null ? lastLog.Term : -1;

                var response = await s.RequestVoteAsync(new VoteRequest
                {
                    CandidateId = _status.Id,
                    Term = currentTerm,
                    LastLogIndex = lastLogIndex,
                    LastLogTerm = lastLogTerm
                });
                if (response != null)
                {
                    Interlocked.Increment(ref serversAlive);
                    if (maxTermFromResponses < response.Term)
                    {
                        lock (_runElectionMaxTermLock)
                        {
                            if (maxTermFromResponses < response.Term) maxTermFromResponses = response.Term;
                        }
                    }
                    if (response.VoteGranted) Interlocked.Increment(ref votes);
                }
            });
            await Task.WhenAll(tasks);

            if (maxTermFromResponses > currentTerm)
            {
                _status.State = ServerStatus.Follower;
                _repo.SetCurrentTerm(maxTermFromResponses);
            }

            if (_status.State == ServerStatus.Candidate && votes >= Math.Ceiling(decimal.Divide(serversAlive, 2)))
            {
                _status.State = ServerStatus.Leader;
                _timerService.StopElectionTimer();
                _timerService.StartHeartbeatTimer();
            }
        }

        internal async void SendHeartbeatAsync(object? state)
        {
            var currentTerm = _repo.GetCurrentTerm();
            var maxTermFromResponses = currentTerm;

            // send heartbeat to all other servers
            var tasks = _otherServers.Select(async s =>
            {
                var response = await s.SendHeartbeatAsync(new HeartbeatRequest
                {
                    LeaderId = _status.Id,
                    Term = currentTerm
                });
                if (maxTermFromResponses < response.Term)
                {
                    lock (_sendHeartbeatMaxTermLock)
                    {
                        if (maxTermFromResponses < response.Term) maxTermFromResponses = response.Term;
                    }
                }
            });
            await Task.WhenAll(tasks);

            if (maxTermFromResponses > currentTerm)
            {
                _status.State = ServerStatus.Follower;
                _repo.SetCurrentTerm(maxTermFromResponses);
                _timerService.StopHeartbeatTimer();
                _timerService.StartElectionTimer();
            }
        }
    }
}
