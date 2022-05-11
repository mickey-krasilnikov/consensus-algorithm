using ConsensusAlgorithm.Core.Services.ServerStatusService;

namespace ConsensusAlgorithm.Core.Services.ServerStateService
{
    public class ServerStatusService : IServerStatusService
    {
        public string Id { get; }

        public ServerState State { get; set; }

        public ServerStatusService(string id)
        {
            Id = id;
            State = ServerState.Follower;
        }
    }
}
