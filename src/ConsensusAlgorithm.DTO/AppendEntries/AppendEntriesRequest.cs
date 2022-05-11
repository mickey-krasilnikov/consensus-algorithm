namespace ConsensusAlgorithm.DTO.AppendEntries
{
	public class AppendEntriesRequest
	{
		/// <summary>
		/// Leader's term
		/// </summary>
		public int Term { get; set; }

		/// <summary>
		/// So followers can redirect clients
		/// </summary>
		public string LeaderId { get; set; } = null!;

		/// <summary>
		/// Index of log entry immediately preceding new ones
		/// </summary>
		public int PrevLogIndex { get; set; }

		/// <summary>
		/// Term of PrevLogIndex entry
		/// </summary>
		public int PrevLogTerm { get; set; }

		/// <summary>
		/// Log entries to store (empty for heartbeat)
		/// </summary>
		public List<LogEntry> Entries { get; set; } = null!;

		/// <summary>
		/// Last entry known to be commited
		/// </summary>
		public int CommitIndex { get; set; }
	}
}