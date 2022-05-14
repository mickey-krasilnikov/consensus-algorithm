namespace ConsensusAlgorithm.Core.Services.TimerService
{
    public interface ITimerService : IDisposable
    {
        void Initialize(TimerCallback electionCallback, TimerCallback sendHeartbeatCallback);

        void StopAll();

        void StartHeartbeatTimer();

        void StopHeartbeatTimer();

        void StartElectionTimer();

        void ResetElectionTimeout();

        void StopElectionTimer();

        void SubmitBroadcastLatency(long elapsedMilliseconds);
    }
}
