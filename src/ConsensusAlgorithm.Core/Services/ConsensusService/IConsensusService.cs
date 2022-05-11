using Microsoft.Extensions.Hosting;
using ConsensusAlgorithm.DTO.RequestVote;
using ConsensusAlgorithm.DTO.AppendEntries;

namespace ConsensusAlgorithm.Core.Services.ConsensusService
{
    public interface IConsensusService : IHostedService, IDisposable
    {
        Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest appendRequest);

        AppendEntriesResponse AppendEntriesInternal(AppendEntriesRequest appendRequest);

        RequestVoteResponse RequestVoteInternal(RequestVoteRequest voteRequest);

        HeartbeatResponse Heartbeat(HeartbeatRequest request);
    }
}
