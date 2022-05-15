namespace ConsensusAlgorithm.Core.ApiClient
{
    public static class ConsensusApiUrlConstants
    {
        public const string RequestVoteUrl = "api/consensus/requestVote";
        public const string AppendEntriesUrl = "api/consensus/appendEntries";
        public const string AppendEntriesExternalUrl = "api/consensus/appendEntriesExternal";
        public const string HeartbeatUrl = "api/consensus/heartbeat";
    }
}
