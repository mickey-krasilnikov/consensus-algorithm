using ConsensusAlgorithm.Core.Services.ServerStatusService;

namespace ConsensusAlgorithm.Core.Services.ServerStateService
{
    public interface IServerStatusService
    {
        string Id { get; }

        ServerState State { get; set; }
    }
}
