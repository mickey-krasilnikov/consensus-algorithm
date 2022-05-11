namespace ConsensusAlgorithm.DTO.AppendEntriesExternal
{
    public class AppendEntriesExternalResponse
    {
        /// <summary>
        /// Current term, for leader to update itself
        /// </summary>
        public int Term { get; set; }

        /// <summary>
        /// True if follower contained entry matching prevLogIndex and prevLogTerm
        /// </summary>
        public bool Success { get; set; }
    }
}