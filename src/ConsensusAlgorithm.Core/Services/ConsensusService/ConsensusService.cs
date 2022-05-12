using Microsoft.Extensions.Logging;
using ConsensusAlgorithm.DTO.RequestVote;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DataAccess;
using ConsensusAlgorithm.Core.ApiClient;
using ConsensusAlgorithm.Core.Mappers;
using ConsensusAlgorithm.Core.StateMachine;
using ConsensusAlgorithm.Core.Services.TimeoutService;
using ConsensusAlgorithm.Core.Services.ServerStatusService;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;

namespace ConsensusAlgorithm.Core.Services.ConsensusService
{
    public class ConsensusService : IConsensusService
    {
        private readonly IConsensusRepository _repo;
        private readonly IStateMachine _stateMachine;
        private readonly ILogger<ConsensusService> _logger;
        private readonly IList<IConsensusApiClient> _otherServers;
        private readonly ITimeoutService _timeoutService;
        private readonly IServerStatusService _status;

        private IList<LogEntry> _logs = new List<LogEntry>();
        private Timer _electionTimer = null!;
        private Timer _heartbeatTimer = null!;
        private int _electionTimeout;

        public ConsensusService(
            IConsensusRepository repo,
            IStateMachine stateMachine,
            ILogger<ConsensusService> logger,
            IList<IConsensusApiClient> otherServers,
            ITimeoutService timeoutService,
            IServerStatusService statusService)
        {
            _repo = repo;
            _stateMachine = stateMachine;
            _logger = logger;
            _otherServers = otherServers;
            _timeoutService = timeoutService;
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
                Parallel.ForEach(_otherServers, async s =>
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

            // reset election timeout
            _electionTimer?.Change(_electionTimeout, Timeout.Infinite);

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

            if (votedFor == null &&
                voteRequest.LastLogIndex >= lastLogIndex &&
                voteRequest.LastLogTerm >= lastLogTerm)
            {
                // reset election timeout
                _electionTimer?.Change(_electionTimeout, Timeout.Infinite);
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
            _electionTimer?.Change(_electionTimeout, Timeout.Infinite);

            return new HeartbeatResponse { Success = true, Term = currentTerm };
        }

        #region IHostedService implementation

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Consensus Server running.");

            _logs = _repo.GetLogEntries().ToLogEntries();
            _electionTimeout = _timeoutService.GetRandomTimeout();
            _electionTimer = new Timer(RunElection, null, _electionTimeout, Timeout.Infinite);
            _heartbeatTimer = new Timer(SendHeartbeat, null, Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Consensus Server is stopping.");
            _electionTimer?.Change(Timeout.Infinite, 0);
            _heartbeatTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _electionTimer?.Dispose();
            _heartbeatTimer?.Dispose();
        }

        #endregion IHostedService implementation

        private void RunElection(object? state)
        {
            // Follower -> Candidate
            _status.State = ServerStatus.Candidate;

            var currentTerm = _repo.GetCurrentTerm();

            // increment current term
            _repo.SetCurrentTerm(currentTerm++);
            _logger.LogInformation("Running Election. Current Term: {Term}", currentTerm);

            // vote for himself
            _repo.SetCandidateIdVotedFor(_status.Id, currentTerm);
            var votes = 1;
            var serversAlive = 1;

            // reset election timeout
            _electionTimer?.Change(_electionTimeout, Timeout.Infinite);

            // send request vote to all other servers
            Parallel.ForEach(_otherServers, async s =>
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
                    if (response.Term > currentTerm)
                    {
                        // step down
                        _status.State = ServerStatus.Follower;
                        _repo.SetCurrentTerm(response.Term);
                    }
                    if (response.VoteGranted) Interlocked.Increment(ref votes);
                }
            });

            if (_status.State == ServerStatus.Candidate && votes >= Math.Ceiling(decimal.Divide(serversAlive, 2)))
            {
                _status.State = ServerStatus.Leader;

                // stop election timer
                _electionTimer?.Change(Timeout.Infinite, 0);

                // start heartbeat timer                                
                _heartbeatTimer?.Change(0, _electionTimeout / 2);
            }
        }

        private void SendHeartbeat(object? state)
        {
            var currentTerm = _repo.GetCurrentTerm();

            // send heartbeat to all other servers
            Parallel.ForEach(_otherServers, async s =>
            {
                var response = await s.SendHeartbeatAsync(new HeartbeatRequest
                {
                    LeaderId = _status.Id,
                    Term = currentTerm
                });

                if (response.Term > currentTerm)
                {
                    // step down
                    _status.State = ServerStatus.Follower;
                    _repo.SetCurrentTerm(response.Term);

                    // stop heartbeat timer
                    _heartbeatTimer?.Change(Timeout.Infinite, 0);

                    // reset timeout
                    _electionTimeout = _timeoutService.GetRandomTimeout();

                    // start election timer
                    _electionTimer?.Change(Timeout.Infinite, 0);
                }
            });
        }
    }
}
