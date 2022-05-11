namespace ConsensusAlgorithm.DTO.RequestVote
{
	public class RequestVoteRequest
	{
		/// <summary>
		/// Candidate requesting vote
		/// </summary>
		public string CandidateId { get; set; } = null!;

		/// <summary>
		/// Candidate's term
		/// </summary>
		public int Term { get; set; }

		/// <summary>
		/// Index of candidate's last log entry
		/// </summary>
		public int LastLogIndex { get; set; }

		/// <summary>
		/// Term of candidate's last log entry
		/// </summary>
		public int LastLogTerm { get; set; }
	}
}