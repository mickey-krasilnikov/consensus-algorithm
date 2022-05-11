namespace ConsensusAlgorithm.Core.Services.TimeoutService
{
    public interface ITimeoutService
    {
        int GetRandomTimeout();
    }

    public class TimeoutService : ITimeoutService
    {
        private const int _electionTimeoutMin = 1000;
        private const int _electionTimeoutMax = 5000;

        private readonly Random _rnd = new();

        public int GetRandomTimeout()
        {
            return _rnd.Next(_electionTimeoutMin, _electionTimeoutMax);
        }
    }
}
