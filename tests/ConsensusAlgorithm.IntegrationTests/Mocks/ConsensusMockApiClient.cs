using ConsensusAlgorithm.Core.ApiClient;
using ConsensusAlgorithm.Core.Services.ConsensusService;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.RequestVote;
using System.Threading.Tasks;

namespace ConsensusAlgorithm.IntegrationTests.Mocks
{
    internal class ConsensusMockApiClient : IConsensusApiClient
    {
        private readonly IConsensusService _consensusService;

        public string Id { get; }

        public ConsensusMockApiClient(string id, IConsensusService consensusService)
        {
            Id = id;
            _consensusService = consensusService;
        }

        public Task<VoteResponse?> RequestVoteAsync(VoteRequest request)
        {
            return Task.FromResult(_consensusService.RequestVote(request))!;
        }

        public Task<AppendEntriesResponse> AppendEntriesAsync(AppendEntriesRequest request)
        {
            return Task.FromResult(_consensusService.AppendEntries(request));
        }

        public Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest request)
        {
            return _consensusService.AppendEntriesExternalAsync(request);
        }

        public Task<HeartbeatResponse> SendHeartbeatAsync(HeartbeatRequest request)
        {
            return Task.FromResult(_consensusService.Heartbeat(request));
        }
    }
}
