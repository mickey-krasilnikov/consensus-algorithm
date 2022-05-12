using Microsoft.Extensions.Hosting;
using ConsensusAlgorithm.DTO.RequestVote;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;

namespace ConsensusAlgorithm.Core.Services.ConsensusService
{
    public interface IConsensusService : IHostedService, IDisposable
    {
        Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest appendRequest);

        AppendEntriesResponse AppendEntries(AppendEntriesRequest appendRequest);

        VoteResponse RequestVote(VoteRequest voteRequest);

        HeartbeatResponse Heartbeat(HeartbeatRequest request);
    }
}
