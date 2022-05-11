namespace ConsensusAlgorithm.DTO.AppendEntries
{
	public class HeartbeatResponse
	{
		/// <summary>
		/// Current term, for leader to update itself
		/// </summary>
		public int Term { get; set; }

		/// <summary>
		/// Indicator if follower updated timer
		/// </summary>
		public bool Success { get; set; }
	}
}