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
        internal Dictionary<string, IConsensusService> Cluster { get; } = new();

        internal Mock<ILogger<ConsensusService>> LoggerMock { get; } = new();

        internal Mock<IConsensusApiClient> InternalConsensusApiClientMock { get; } = new();

        internal Dictionary<string, ServerStatusService> Statuses { get; } = new();

        internal TestConsensusApiClient(Dictionary<string, string> serverList)
        {
            foreach (var s in serverList)
            {
                var statusService = new ServerStatusService(s.Key);
                Statuses.Add(s.Key, statusService);
                Cluster.Add(s.Key,
                    new ConsensusService(
                        new ConsensusInMemoryRepository(),
                        new DictionaryStateMachine(),
                        LoggerMock.Object,
                        this,
                        new TimerService(),
                        statusService,
                        new ConsensusClusterConfig()
                        {
                            CurrentServerId = s.Key,
                            ServerList = serverList
                        }));
            }
        }

        public Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(string serverId, AppendEntriesExternalRequest request, CancellationToken? cancellationToken = null)
        {
            return Cluster[serverId].AppendEntriesExternalAsync(request);
        }

        public Task<AppendEntriesResponse> AppendEntriesAsync(string serverId, AppendEntriesRequest request, CancellationToken? cancellationToken = null)
        {
            return Task.FromResult(Cluster[serverId].AppendEntries(request));
        }

        public Task<VoteResponse?> RequestVoteAsync(string serverId, VoteRequest request, CancellationToken? cancellationToken = null)
        {
            return Task.FromResult(Cluster[serverId].RequestVote(request))!;
        }

        public Task<HeartbeatResponse> SendHeartbeatAsync(string serverId, HeartbeatRequest request, CancellationToken? cancellationToken = null)
        {
            return Task.FromResult(Cluster[serverId].Heartbeat(request));
        }
    }
}