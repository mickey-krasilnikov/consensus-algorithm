namespace ConsensusAlgorithm.DTO.RequestVote
{
	public class HeartbeatRequest
	{
		/// <summary>
		/// Heartbeating Leader ID
		/// </summary>
		public string LeaderId { get; set; } = null!;

		/// <summary>
		/// Leader's term
		/// </summary>
		public int Term { get; set; }
	}
}