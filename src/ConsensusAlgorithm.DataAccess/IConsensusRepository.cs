using ConsensusAlgorithm.DataAccess.Entities;

namespace ConsensusAlgorithm.DataAccess
{
	public interface IConsensusRepository
	{
		/// <summary>
		/// Get current term for current server
		/// </summary>
		/// <returns>Latest term server has seen</returns>
		int GetCurrentTerm();

		/// <summary>
		/// Set current term for current server
		/// </summary>
		/// <param name="term">Term</param>
		void SetCurrentTerm(int term);

		/// <summary>
		/// Get candidate id voted for in specified term
		/// </summary>
		/// <param name="term">Term</param>
		/// <returns>Candidate ID that received vote in specified term (or null if none)</returns>
		string? GetCandidateIdVotedFor(int term);

		/// <summary>
		/// Set candidate id voted for in specified term
		/// </summary>
		/// <param name="candidateId">Candidate ID</param>
		/// <param name="term">Term</param>
		void SetCandidateIdVotedFor(string candidateId, int term);

		/// <summary>
		/// Get log entries on current server
		/// </summary>
		/// <returns>log entries</returns>
		IList<LogEntity> GetLogEntries();

		/// <summary>
		/// Append log entry to the logs
		/// </summary>
		/// <param name="log">log entry</param>
		void AppendLogEntry(LogEntity log);

		/// <summary>
		/// Remove Logs starting from provided index (including)
		/// </summary>
		/// <param name="index">Index, starting from which logs will be removed</param>
		void RemoveStartingFrom(int index);
	}
}