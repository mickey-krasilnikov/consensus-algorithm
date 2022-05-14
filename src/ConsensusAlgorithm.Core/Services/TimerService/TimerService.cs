namespace ConsensusAlgorithm.Core.Services.TimerService
{
    public class TimerService : ITimerService
    {
        private Timer _electionTimer = null!;
        private Timer _heartbeatTimer = null!;
        private bool _isInitialized;
        private long _averageBroadcast;

        private readonly Random _rnd = new();

        public void Initialize(TimerCallback electionCallback, TimerCallback sendHeartbeatCallback)
        {
            if (_isInitialized) StopAll();

            _electionTimer = new Timer(electionCallback, null, GetRandomElectionTimeout(), Timeout.Infinite);
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
            _heartbeatTimer?.Change(0, GetRandomElectionTimeout() / 2);
        }

        public void StopHeartbeatTimer()
        {
            _heartbeatTimer?.Change(Timeout.Infinite, 0);
        }

        public void StartElectionTimer()
        {
            _electionTimer?.Change(GetRandomElectionTimeout(), Timeout.Infinite);
        }

        public void ResetElectionTimeout()
        {
            _electionTimer?.Change(GetRandomElectionTimeout(), Timeout.Infinite);
        }

        public void StopElectionTimer()
        {
            _electionTimer?.Change(Timeout.Infinite, 0);
        }

        private long GetRandomElectionTimeout()
        {
            return _averageBroadcast != default 
                ? _rnd.NextInt64(_averageBroadcast, 2 * _averageBroadcast) 
                : _rnd.NextInt64(100, 500);
        }

        public void Dispose()
        {
            _electionTimer?.Dispose();
            _heartbeatTimer?.Dispose();
        }

        public void SubmitBroadcastLatency(long elapsedMilliseconds)
        {
            _averageBroadcast = _averageBroadcast != default
                ? (_averageBroadcast + elapsedMilliseconds) / 2
                : elapsedMilliseconds;
        }
    }
}
