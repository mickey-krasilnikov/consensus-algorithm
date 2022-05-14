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
        public const string HealthCheckUrl = "api/maintenance/healthz";

        private readonly Uri _baseUrl;
        private readonly HttpClient _client = new();
        private readonly ITimerService _timerService;
        private Stopwatch _stopwatch = new Stopwatch();

        public string Id { get; }

        public ConsensusApiClient(string serverId, string baseURL, ITimerService timerService)
        {
            Id = serverId;
            _timerService = timerService;
            _baseUrl = new Uri(baseURL);
        }

        public async Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest request)
        {
            return await PostRequest<AppendEntriesExternalRequest, AppendEntriesExternalResponse>(request, AppendEntriesExternalUrl)
                ?? new AppendEntriesExternalResponse { Success = false };
        }

        public async Task<AppendEntriesResponse> AppendEntriesAsync(AppendEntriesRequest request)
        {
            return await PostRequest<AppendEntriesRequest, AppendEntriesResponse>(request, AppendEntriesUrl)
                ?? new AppendEntriesResponse { Success = false };
        }

        public async Task<VoteResponse?> RequestVoteAsync(VoteRequest request)
        {
            return await PostRequest<VoteRequest, VoteResponse>(request, RequestVoteUrl);
        }

        public async Task<HeartbeatResponse> SendHeartbeatAsync(HeartbeatRequest request)
        {
            return await PostRequest<HeartbeatRequest, HeartbeatResponse>(request, HeartbeatUrl)
                ?? new HeartbeatResponse { Success = false };
        }

        public async Task<bool> HealthCheckAsync()
        {
            return (await _client.GetAsync(new Uri(_baseUrl, HealthCheckUrl))).IsSuccessStatusCode;
        }

        private async Task<TResponse?> PostRequest<TRequest, TResponse>(TRequest request, string url)
        {
            try
            {
                var data = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                _stopwatch.Restart();
                var response = await _client.PostAsync(new Uri(_baseUrl, url), data);
                _stopwatch.Stop();
                _timerService.SubmitBroadcastLatency(_stopwatch.ElapsedMilliseconds);
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseString);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
