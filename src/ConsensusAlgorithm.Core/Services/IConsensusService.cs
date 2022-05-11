using Microsoft.Extensions.Hosting;
using ConsensusAlgorithm.DTO.RequestVote;
using ConsensusAlgorithm.DTO.AppendEntries;

namespace ConsensusAlgorithm.Core.Services
{
	public interface IConsensusService  : IHostedService, IDisposable
	{
		Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest appendRequest);

		Task<AppendEntriesResponse> AppendEntriesInternalAsync(AppendEntriesRequest appendRequest);

		RequestVoteResponse RequestVoteInternal(RequestVoteRequest voteRequest);

		HeartbeatResponse Heartbeat(HeartbeatRequest request);
	}
}
