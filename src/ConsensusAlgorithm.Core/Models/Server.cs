using ConsensusAlgorithm.DTO.AppendEntries;

namespace ConsensusAlgorithm.Core.Models
{
	public class Server
	{
		public string Id { get; }

		public ServerState State { get; set; }

		public IList<LogEntry> Logs { get; set; } = new List<LogEntry>();

		public Server(string id)
		{
			Id = id;
			State = ServerState.Follower;
		}
	}
}
