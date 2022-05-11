using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.RequestVote;

namespace ConsensusAlgorithm.Core.ApiClient
{
	public interface IConsensusApiClient
	{
		string Id { get; }

		Task<RequestVoteResponse?> RequestVoteInternalAsync(RequestVoteRequest request);

		Task<AppendEntriesResponse> AppendEntriesInternalAsync(AppendEntriesRequest request);

		Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest request);

		Task<HeartbeatResponse> SendHeartbeatAsync(HeartbeatRequest request);
	}
}
