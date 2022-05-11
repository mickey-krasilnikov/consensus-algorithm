namespace ConsensusAlgorithm.DTO.AppendEntriesExternal
{
    public class AppendEntriesExternalRequest
    {
        /// <summary>
        /// Commands for state machine
        /// </summary>
        public List<string> Commands { get; set; } = null!;
    }
}