using Microsoft.Extensions.Logging;
using ConsensusAlgorithm.DTO.RequestVote;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.Core.Models;
using ConsensusAlgorithm.DataAccess;
using ConsensusAlgorithm.Core.ApiClient;
using ConsensusAlgorithm.Core.Configuration;
using ConsensusAlgorithm.Core.Mappers;
using ConsensusAlgorithm.Core.StateMachine;
using ConsensusAlgorithm.Core.Services.TimeoutService;

namespace ConsensusAlgorithm.Core.Services.ConsensusService
{
    public class ConsensusService : IConsensusService
    {
        private readonly IConsensusRepository _repo;
        private readonly IStateMachine _stateMachine;
        private readonly ILogger<ConsensusService> _logger;
        private readonly IList<IConsensusApiClient> _otherServers;
        private readonly ITimeoutService _timeout;
        private IConsensusApiClient _leaderServer = null!;
        private readonly Server _currentServer;

        private Timer _electionTimer = null!;
        private Timer _heartbeatTimer = null!;
        private int _electionTimeout;

        public ConsensusService(
            IConsensusRepository repo,
            IStateMachine stateMachine,
            ILogger<ConsensusService> logger,
            IList<IConsensusApiClient> otherServers,
            ITimeoutService timeoutProvider,
            ConsensusClusterConfig config)
        {
            _repo = repo;
            _stateMachine = stateMachine;
            _logger = logger;
            _otherServers = otherServers;
            _timeout = timeoutProvider;
            _currentServer = new Server(config.CurrentServerId);
        }

        public async Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest appendRequest)
        {
            var currentTerm = _repo.GetCurrentTerm();
            if (_currentServer.State != ServerState.Leader)
                return _leaderServer != null
                    ? await _leaderServer.AppendEntriesExternalAsync(appendRequest)
                    : new AppendEntriesExternalResponse { Success = false, Term = currentTerm };
            else
            {
                var prevLog = _currentServer.Logs.LastOrDefault();
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
                    _currentServer.Logs.Append(entry);
                    _repo.AppendLogEntry(entry.ToLogEntity());
                }

                var serversAppendedEntries = 1;
                Parallel.ForEach(_otherServers, async s =>
                {
                    var response = await s.AppendEntriesInternalAsync(new AppendEntriesRequest
                    {
                        LeaderId = _currentServer.Id,
                        Term = currentTerm,
                        PrevLogIndex = prevLogIndex,
                        PrevLogTerm = prevLogTerm,
                        //CommitIndex = ??,
                        Entries = entriesToAppend
                    });
                    if (response.Success) Interlocked.Increment(ref serversAppendedEntries);
                });
                return new AppendEntriesExternalResponse { Success = true, Term = currentTerm };
            }
        }

        public AppendEntriesResponse AppendEntriesInternal(AppendEntriesRequest appendRequest)
        {
            var currentTerm = _repo.GetCurrentTerm();

            if (appendRequest.Term < currentTerm)
                return new AppendEntriesResponse { Success = false, Term = currentTerm };

            if (appendRequest.Term > currentTerm)
            {
                _repo.SetCurrentTerm(appendRequest.Term);
                currentTerm = appendRequest.Term;
            }

            // stepdown
            _currentServer.State = ServerState.Follower;

            // reset election timeout
            _electionTimer?.Change(_electionTimeout, Timeout.Infinite);

            var prevLog = _currentServer.Logs.LastOrDefault();
            var prevLogTerm = prevLog != null ? prevLog.Term : -1;
            if (prevLogTerm != appendRequest.PrevLogTerm)
                return new AppendEntriesResponse { Success = false, Term = currentTerm };

            var isConflict = false;
            var toAppend = new List<LogEntry>();
            foreach (var entry in appendRequest.Entries.OrderBy(l => l.Index))
                if (isConflict)
                    toAppend.Add(entry);
                else
                {
                    var existingEntry = _currentServer.Logs.ElementAtOrDefault(entry.Index);
                    if (existingEntry == null) break;
                    if (existingEntry.Index != entry.Index ||
                        existingEntry.Term != entry.Term ||
                        existingEntry.Command != entry.Command)
                    {
                        isConflict = true;
                        toAppend.Add(entry);
                        _repo.RemoveStartingFrom(entry.Index);
                        break;
                    }
                }

            foreach (var entry in toAppend.OrderBy(e => e.Index))
            {
                _currentServer.Logs.Append(entry);
                _repo.AppendLogEntry(entry.ToLogEntity());
            }

            // advance state machine with newly commited entries
            //_stateMachine.Apply();

            return new AppendEntriesResponse { Success = true, Term = currentTerm };
        }

        public RequestVoteResponse RequestVoteInternal(RequestVoteRequest voteRequest)
        {
            _logger.LogInformation("Server '{CandidateId}' (with term {Term}) requested vote.", voteRequest.CandidateId, voteRequest.Term);

            if (voteRequest.Term > _repo.GetCurrentTerm())
            {
                // step down
                _currentServer.State = ServerState.Follower;
                _repo.SetCurrentTerm(voteRequest.Term);
            }

            var currentTerm = _repo.GetCurrentTerm();
            var votedFor = _repo.GetCandidateIdVotedFor(currentTerm);
            var lastLogListItemIndex = _currentServer.Logs.Count - 1;
            var lastLogIndex = lastLogListItemIndex >= 0 ? _currentServer.Logs[lastLogListItemIndex].Index : -1;
            var lastLogTerm = lastLogListItemIndex >= 0 ? _currentServer.Logs[lastLogListItemIndex].Term : default;

            if (voteRequest.Term == currentTerm &&
                votedFor == null &&
                voteRequest.LastLogIndex >= lastLogIndex &&
                voteRequest.LastLogTerm >= lastLogTerm)
            {
                // reset election timeout
                _electionTimer?.Change(_electionTimeout, Timeout.Infinite);
                _repo.SetCandidateIdVotedFor(voteRequest.CandidateId, voteRequest.Term);
                _leaderServer = _otherServers.Single(s => s.Id == voteRequest.CandidateId);
                return new RequestVoteResponse
                {
                    VoteGranted = true,
                    Term = currentTerm
                };
            }

            return new RequestVoteResponse
            {
                VoteGranted = false,
                Term = currentTerm
            };
        }

        public HeartbeatResponse Heartbeat(HeartbeatRequest heartbeat)
        {
            var currentTerm = _repo.GetCurrentTerm();

            // obsolete request
            if (heartbeat.Term < currentTerm) return new HeartbeatResponse { Success = true, Term = currentTerm };

            // received from new leader -> stepdown
            if (heartbeat.Term > currentTerm)
            {
                _currentServer.State = ServerState.Follower;
                _repo.SetCurrentTerm(heartbeat.Term);
                currentTerm = heartbeat.Term;
            }
            if (_leaderServer == null || _leaderServer.Id != heartbeat.LeaderId)
            {
                var newLeader = _otherServers.FirstOrDefault(s => s.Id == heartbeat.LeaderId);
                if (newLeader != null) _leaderServer = newLeader;
            }

            // reset election timeout
            _electionTimer?.Change(_electionTimeout, Timeout.Infinite);

            return new HeartbeatResponse { Success = true, Term = currentTerm };
        }

        #region IHostedService implementation

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Consensus Server running.");

            _currentServer.Logs = _repo.GetLogEntries().ToLogEntries();
            _electionTimeout = _timeout.GetRandomTimeout();
            _electionTimer = new Timer(RunElection, null, _electionTimeout, Timeout.Infinite);
            _heartbeatTimer = new Timer(SendHeartbeat, null, Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void RunElection(object? state)
        {
            // Follower -> Candidate
            _currentServer.State = ServerState.Candidate;

            var currentTerm = _repo.GetCurrentTerm();

            // increment current term
            _repo.SetCurrentTerm(currentTerm++);
            _logger.LogInformation("Running Election. Current Term: {Term}", currentTerm);

            // vote for himself
            _repo.SetCandidateIdVotedFor(_currentServer.Id, currentTerm);
            var votes = 1;
            var serversAlive = 1;

            // reset election timeout
            _electionTimer?.Change(_electionTimeout, Timeout.Infinite);

            // send request vote to all other servers
            Parallel.ForEach(_otherServers, async s =>
            {
                var lastLog = _currentServer.Logs.LastOrDefault();
                var lastLogIndex = lastLog != null ? lastLog.Index : -1;
                var lastLogTerm = lastLog != null ? lastLog.Term : -1;

                var response = await s.RequestVoteInternalAsync(new RequestVoteRequest
                {
                    CandidateId = _currentServer.Id,
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
                        _currentServer.State = ServerState.Follower;
                        _repo.SetCurrentTerm(response.Term);
                    }
                    if (response.VoteGranted)
                        Interlocked.Increment(ref votes);
                }
            });

            if (_currentServer.State == ServerState.Candidate && votes >= Math.Ceiling(decimal.Divide(serversAlive, 2)))
            {
                _currentServer.State = ServerState.Leader;

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
                    LeaderId = _currentServer.Id,
                    Term = currentTerm
                });

                if (response.Success && response.Term > currentTerm)
                {
                    // step down
                    _currentServer.State = ServerState.Follower;
                    _repo.SetCurrentTerm(response.Term);

                    // stop heartbeat timer
                    _heartbeatTimer?.Change(Timeout.Infinite, 0);

                    // reset timeout
                    _electionTimeout = _timeout.GetRandomTimeout();

                    // start election timer
                    _electionTimer?.Change(Timeout.Infinite, 0);
                }
            });
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
    }
}
