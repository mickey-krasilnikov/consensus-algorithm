using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.RequestVote;

namespace ConsensusAlgorithm.Core.ApiClient
{
    public interface IConsensusApiClient
    {
        Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(string serverId, AppendEntriesExternalRequest request, CancellationToken? cancellationToken = null);

        Task<AppendEntriesResponse> AppendEntriesAsync(string serverId, AppendEntriesRequest request, CancellationToken? cancellationToken = null);

        Task<VoteResponse?> RequestVoteAsync(string serverId, VoteRequest request, CancellationToken? cancellationToken = null);

        Task<HeartbeatResponse> SendHeartbeatAsync(string serverId, HeartbeatRequest request, CancellationToken? cancellationToken = null);
    }
}
