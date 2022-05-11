using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.RequestVote;
using System.Text;
using System.Text.Json;

namespace ConsensusAlgorithm.Core.ApiClient
{
    public class ConsensusApiClient : IConsensusApiClient
    {
        public const string RequestVoteUrl = "requestVote";
        public const string AppendEntriesUrl = "appendEntries";
        public const string AppendEntriesExternalUrl = "appendEntriesExternal";
        public const string HeartbeatUrl = "heartbeat";

        private readonly string _baseUrl;
        private readonly HttpClient _client = new();

        public string Id { get; }

        public ConsensusApiClient(string serverId, string baseURL)
        {
            Id = serverId;
            _baseUrl = baseURL;
        }

        public async Task<RequestVoteResponse?> RequestVoteInternalAsync(RequestVoteRequest request)
        {
            return await PostRequest<RequestVoteRequest, RequestVoteResponse>(request, RequestVoteUrl);
        }

        public async Task<AppendEntriesResponse> AppendEntriesInternalAsync(AppendEntriesRequest request)
        {
            return await PostRequest<AppendEntriesRequest, AppendEntriesResponse>(request, AppendEntriesUrl)
                ?? new AppendEntriesResponse { Success = false };
        }

        public async Task<AppendEntriesExternalResponse> AppendEntriesExternalAsync(AppendEntriesExternalRequest request)
        {
            return await PostRequest<AppendEntriesExternalRequest, AppendEntriesExternalResponse>(request, AppendEntriesExternalUrl)
                ?? new AppendEntriesExternalResponse { Success = false };
        }

        public async Task<HeartbeatResponse> SendHeartbeatAsync(HeartbeatRequest request)
        {
            return await PostRequest<HeartbeatRequest, HeartbeatResponse>(request, HeartbeatUrl)
                ?? new HeartbeatResponse { Success = false };
        }

        private async Task<TResponse?> PostRequest<TRequest, TResponse>(TRequest request, string url)
        {
            try
            {
                var data = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync($"{_baseUrl}/{url}", data);
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
