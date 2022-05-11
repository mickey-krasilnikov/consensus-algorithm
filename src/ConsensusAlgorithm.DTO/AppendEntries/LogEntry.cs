namespace ConsensusAlgorithm.DTO.AppendEntries
{
	public class LogEntry
	{
		/// <summary>
		/// Term when entry was received by leader
		/// </summary>
		public int Term { get; set; }

		/// <summary>
		/// Position of entry in the log
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		/// Command for state machine
		/// </summary>
		public string Command { get; set; } = null!;
	}
}