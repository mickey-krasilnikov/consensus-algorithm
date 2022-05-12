namespace ConsensusAlgorithm.DTO.RequestVote
{
	public class VoteResponse
	{
		/// <summary>
		/// Current term, for candidate to update itself
		/// </summary>
		public int Term { get; set; }

		/// <summary>
		/// True means candidate received vote
		/// </summary>
		public bool VoteGranted { get; set; }
	}
}