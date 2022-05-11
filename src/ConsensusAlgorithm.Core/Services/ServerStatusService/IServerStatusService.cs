namespace ConsensusAlgorithm.Core.Services.ServerStatusService
{
    public interface IServerStatusService
    {
        string Id { get; }

        public string? LeaderId { get; set; }

        public bool HasLeader { get; }

        public bool IsLeader { get; }

        ServerStatus State { get; set; }
    }
}
