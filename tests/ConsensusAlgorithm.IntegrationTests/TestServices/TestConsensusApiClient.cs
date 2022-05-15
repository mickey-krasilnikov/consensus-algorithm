using ConsensusAlgorithm.Core.ApiClient;
using ConsensusAlgorithm.Core.Configuration;
using ConsensusAlgorithm.Core.Services.ConsensusService;
using ConsensusAlgorithm.Core.Services.ServerStatusService;
using ConsensusAlgorithm.Core.Services.TimerService;
using ConsensusAlgorithm.Core.StateMachine;
using ConsensusAlgorithm.DataAccess;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.RequestVote;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConsensusAlgorithm.IntegrationTests.TestServices
{
    internal class TestConsensusApiClient : IConsensusApiClient
    {
        private readonly Dictionary<string, IConsensusService> _cluster;

        internal static Mock<ILogger<ConsensusService>> LoggerMock { get; } = new();

        internal static Mock<IConsensusApiClient> InternalConsensusApiClientMock { get; } = new();

        internal static Dictionary<string, ServerStatusService> Statuses { get; } = new();

        internal TestConsensusApiClient(Dictionary<string, string> serverList)
        {
            var entries = serverList.Select(s =>
            {
                var statusService = new ServerStatusService(s.Key);
                Statuses.Add(s.Key, statusService);
                return new KeyValuePair<string, IConsensusService>(s.Key, new ConsensusService(
                    new ConsensusInMemoryRepository(),
                    new DictionaryStateMachine(),
                    LoggerMock.Object,
                    InternalConsensusApiClientMock.Object,
                    new TimerService(),
                    statusService,
                    new ConsensusClusterConfig()
                    {
                        CurrentServerId = s.Key,
                        ServerList = serverList
                    }));
            });
            _cluster = new Dictionary<string, IConsensusService>(entries);
        }

        public Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(string serverId, AppendEntriesExternalRequest request, CancellationToken? cancellationToken = null)
        {
            return _cluster[serverId].AppendEntriesExternalAsync(request);
        }

        public Task<AppendEntriesResponse> AppendEntriesAsync(string serverId, AppendEntriesRequest request, CancellationToken? cancellationToken = null)
        {
            return Task.FromResult(_cluster[serverId].AppendEntries(request));
        }

        public Task<VoteResponse?> RequestVoteAsync(string serverId, VoteRequest request, CancellationToken? cancellationToken = null)
        {
            return Task.FromResult(_cluster[serverId].RequestVote(request))!;
        }

        public Task<HeartbeatResponse> SendHeartbeatAsync(string serverId, HeartbeatRequest request, CancellationToken? cancellationToken = null)
        {
            return Task.FromResult(_cluster[serverId].Heartbeat(request));
        }
    }
}