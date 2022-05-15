namespace ConsensusAlgorithm.Core.Services.ServerStatusService
{
    public class ServerStatusService : IServerStatusService
    {
        private ServerStatus _state;

        public string Id { get; }

        public string? LeaderId { get; set; }

        public bool IsLeader { get => State == ServerStatus.Leader; }

        public ServerStatus State
        {
            get => _state;
            set
            {
                _state = value;
                if (_state == ServerStatus.Leader) LeaderId = Id;
            }
        }

        public ServerStatusService(string id)
        {
            Id = id;
            State = ServerStatus.Follower;
        }
    }
}
