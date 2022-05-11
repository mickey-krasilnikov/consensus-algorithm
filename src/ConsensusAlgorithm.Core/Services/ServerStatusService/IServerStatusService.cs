namespace ConsensusAlgorithm.Core.Services.ServerStatusService
{
    public interface IServerStatusService
    {
        string Id { get; }

        ServerState State { get; set; }
    }
}
