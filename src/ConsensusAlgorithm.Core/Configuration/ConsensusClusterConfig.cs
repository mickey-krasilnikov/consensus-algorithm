namespace ConsensusAlgorithm.Core.Configuration
{
	public class ConsensusClusterConfig
	{
		public string CurrentServerId { get; set; } = null!;

		public Dictionary<string, string> ServerList { get; set; } = new();
	}
}
