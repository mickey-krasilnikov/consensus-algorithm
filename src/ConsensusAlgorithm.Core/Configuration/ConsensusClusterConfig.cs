namespace ConsensusAlgorithm.Core.Configuration
{
	public class ConsensusClusterConfig
    {
        private const int _defaultRetryCount = 5;
        private const int _defaultRetryDelayMilliseconds = 1000;

        public string CurrentServerId { get; set; } = null!;

		public Dictionary<string, string> ServerList { get; set; } = new();

        public int RetryCount { get; set; } = _defaultRetryCount;

        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(_defaultRetryDelayMilliseconds);
    }
}
