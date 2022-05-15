using ConsensusAlgorithm.Core.Configuration;
using ConsensusAlgorithm.Core.Services.TimerService;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.RequestVote;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ConsensusAlgorithm.Core.ApiClient
{
    public class ConsensusApiClient : IConsensusApiClient
    {
        public const string RequestVoteUrl = "api/consensus/requestVote";
        public const string AppendEntriesUrl = "api/consensus/appendEntries";
        public const string AppendEntriesExternalUrl = "api/consensus/appendEntriesExternal";
        public const string HeartbeatUrl = "api/consensus/heartbeat";

        private readonly HttpClient _client;
        private readonly Dictionary<string, string> _serverList;
        private readonly ITimerService _timerService;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public ConsensusApiClient(HttpClient httpClient, ConsensusClusterConfig config, ITimerService timerService)
        {
            _client = httpClient;
            _serverList = config.ServerList;
            _timerService = timerService;
        }

        public async Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(
            string serverId,
            AppendEntriesExternalRequest request,
            CancellationToken? cancellationToken = null)
        {
            return await PostRequest<AppendEntriesExternalRequest, AppendEntriesExternalResponse>(request,
                    new Uri(new Uri(_serverList[serverId]), AppendEntriesExternalUrl), cancellationToken ?? CancellationToken.None)
                ?? new () { Success = false };
        }

        public async Task<AppendEntriesResponse> AppendEntriesAsync(
            string serverId,
            AppendEntriesRequest request,
            CancellationToken? cancellationToken = null)
        {
            return await PostRequest<AppendEntriesRequest, AppendEntriesResponse>(request,
                    new Uri(new Uri(_serverList[serverId]), AppendEntriesUrl), cancellationToken ?? CancellationToken.None)
                ?? new () { Success = false };
        }

        public async Task<VoteResponse?> RequestVoteAsync(
            string serverId,
            VoteRequest request,
            CancellationToken? cancellationToken = null)
        {
            return await PostRequest<VoteRequest, VoteResponse>(request,
                new Uri(new Uri(_serverList[serverId]), RequestVoteUrl), cancellationToken ?? CancellationToken.None);
        }

        public async Task<HeartbeatResponse> SendHeartbeatAsync(
            string serverId,
            HeartbeatRequest request,
            CancellationToken? cancellationToken = null)
        {
            return await PostRequest<HeartbeatRequest, HeartbeatResponse>(request,
                    new Uri(new Uri(_serverList[serverId]), HeartbeatUrl), cancellationToken ?? CancellationToken.None)
                ?? new () { Success = false };
        }

        private async Task<TResponse?> PostRequest<TRequest, TResponse>(
            TRequest request,
            Uri url,
            CancellationToken cancellationToken)
        {
            try
            {
                var data = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                _stopwatch.Restart();
                var response = await _client.PostAsync(url, data, cancellationToken);
                _stopwatch.Stop();
                _timerService.SubmitBroadcastLatency(_stopwatch.ElapsedMilliseconds);
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<TResponse>(responseString);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
