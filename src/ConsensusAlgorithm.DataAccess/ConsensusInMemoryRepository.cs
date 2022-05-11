using ConsensusAlgorithm.DataAccess.Entities;

namespace ConsensusAlgorithm.DataAccess
{
    public class ConsensusInMemoryRepository : IConsensusRepository
    {
        private readonly object _termLock = new();
        private readonly object _voteLock = new();
        private readonly object _logsLock = new();

        private int _currentTerm;
        private VoteEntity? _vote;
        private readonly IList<LogEntity> _logs = new List<LogEntity>();

        public int GetCurrentTerm()
        {
            lock (_termLock) return _currentTerm;
        }

        public void SetCurrentTerm(int term)
        {
            lock (_termLock) _currentTerm = term;
        }

        public string? GetCandidateIdVotedFor(int term)
        {
            lock (_voteLock) return _vote != null && _vote.Term == term ? _vote.CandidateId : null;
        }

        public void SetCandidateIdVotedFor(string candidateId, int term)
        {
            lock (_voteLock) _vote = new VoteEntity { CandidateId = candidateId, Term = term };
        }

        public IList<LogEntity> GetLogEntries()
        {
            lock (_logsLock) return _logs.OrderBy(l => l.Index).ToList();
        }

        public void AppendLogEntry(LogEntity log)
        {
            lock (_logsLock) _logs.Add(log);
        }

        public void RemoveStartingFrom(int index)
        {
            lock (_logsLock)
            {
                var lastLogIndex = _logs.Count - 1;
                for (var i = lastLogIndex; i >= index; i--)
                {
                    _logs.RemoveAt(i);
                }
            }
        }
    }
}