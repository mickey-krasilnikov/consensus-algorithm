namespace ConsensusAlgorithm.DataAccess.Entities
{
	public class LogEntity
	{
		public int Term { get; set; }

		public int Index { get; set; }

		public string Command { get; set; } = null!;
    }
}