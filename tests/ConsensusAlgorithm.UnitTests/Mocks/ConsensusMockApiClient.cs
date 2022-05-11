using ConsensusAlgorithm.Core.ApiClient;
using ConsensusAlgorithm.Core.Services;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.RequestVote;
using System.Threading.Tasks;

namespace ConsensusAlgorithm.UnitTests.Mocks
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

        public Task<RequestVoteResponse?> RequestVoteInternalAsync(RequestVoteRequest request)
        {
            return Task.FromResult(_consensusService.RequestVoteInternal(request))!;
        }

        public Task<AppendEntriesResponse> AppendEntriesInternalAsync(AppendEntriesRequest request)
        {
            return Task.FromResult(_consensusService.AppendEntriesInternal(request));
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
