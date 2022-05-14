using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.RequestVote;

namespace ConsensusAlgorithm.Core.ApiClient
{
    public interface IConsensusApiClient
	{
		string Id { get; }

		Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest request);

		Task<AppendEntriesResponse> AppendEntriesAsync(AppendEntriesRequest request);

		Task<VoteResponse?> RequestVoteAsync(VoteRequest request);

		Task<HeartbeatResponse> SendHeartbeatAsync(HeartbeatRequest request);

        Task<bool> HealthCheckAsync();
    }
}
