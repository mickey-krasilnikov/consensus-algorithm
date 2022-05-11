namespace ConsensusAlgorithm.DataAccess.Entities
{
	public class VoteEntity
	{
		public string? CandidateId { get; set; }

		public int Term { get; set; }
	}
}