using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.RequestVote;

namespace ConsensusAlgorithm.Core.ApiClient
{
    public interface IConsensusApiClient
	{
		string Id { get; }

		Task<VoteResponse?> RequestVoteInternalAsync(VoteRequest request);

		Task<AppendEntriesResponse> AppendEntriesInternalAsync(AppendEntriesRequest request);

		Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest request);

		Task<HeartbeatResponse> SendHeartbeatAsync(HeartbeatRequest request);
	}
}
