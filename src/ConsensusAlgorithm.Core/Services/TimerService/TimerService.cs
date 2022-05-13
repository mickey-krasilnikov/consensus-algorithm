namespace ConsensusAlgorithm.Core.Services.TimerService
{
    public class TimerService : ITimerService
    {
        private const int _electionTimeoutMin = 1000;
        private const int _electionTimeoutMax = 5000;

        private Timer _electionTimer = null!;
        private Timer _heartbeatTimer = null!;
        private int _electionTimeout;
        private bool _isInitialized = false;

        private readonly Random _rnd = new();


        public void Initialize(TimerCallback electionCallback, TimerCallback sendHeartbeatCallback)
        {
            _electionTimeout = GetRandomElectionTimeout();
            if (_isInitialized) StopAll();

            _electionTimer = new Timer(electionCallback, null, _electionTimeout, Timeout.Infinite);
            _heartbeatTimer = new Timer(sendHeartbeatCallback, null, Timeout.Infinite, 0);
            _isInitialized = true;
        }

        public void StopAll()
        {
            _electionTimer?.Change(Timeout.Infinite, 0);
            _heartbeatTimer?.Change(Timeout.Infinite, 0);
        }

        public void StartHeartbeatTimer()
        {
            _heartbeatTimer?.Change(0, _electionTimeout / 2);
        }

        public void StopHeartbeatTimer()
        {
            _heartbeatTimer?.Change(Timeout.Infinite, 0);
        }

        public void StartElectionTimer()
        {
            _electionTimeout = GetRandomElectionTimeout();
            _electionTimer?.Change(_electionTimeout, Timeout.Infinite);
        }

        public void ResetElectionTimeout()
        {
            _electionTimer?.Change(_electionTimeout, Timeout.Infinite);
        }

        public void StopElectionTimer()
        {
            _electionTimer?.Change(Timeout.Infinite, 0);
        }

        private int GetRandomElectionTimeout()
        {
            return _rnd.Next(_electionTimeoutMin, _electionTimeoutMax);
        }

        public void Dispose()
        {
            _electionTimer?.Dispose();
            _heartbeatTimer?.Dispose();
        }
    }
}
