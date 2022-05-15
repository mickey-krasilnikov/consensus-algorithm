using ConsensusAlgorithm.Core.ApiClient;
using ConsensusAlgorithm.Core.Services.ConsensusService;
using ConsensusAlgorithm.IntegrationTests.TestServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace ConsensusAlgorithm.IntegrationTests
{
    public abstract class IntegrationTestBase
    {
        private string _currentServerId = null!;
        private WebApplicationFactory<Program> _factory = null!;
        private HttpClient _client = null!;

        internal HttpClient Client { get => _client; }

        internal abstract int DesiredAmountOfServers { get; }

        internal string CurrentServerId => _currentServerId;

        internal Dictionary<string, string> ServerList { get; } = new();

        internal TestConsensusApiClient TestApiClient { get; set; } = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _currentServerId = 1.ToString();
            for (var i = 2; i <= DesiredAmountOfServers; i++)
            {
                ServerList.Add($"{i}", $"http://test{i}.com");
            }

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTesting");
                builder.UseSetting("ConsensusCluster:CurrentServerId", $"{_currentServerId}");
                foreach (var item in ServerList)
                {
                    builder.UseSetting($"ConsensusCluster:ServerList:{item.Key}", item.Value);
                }
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConsensusApiClient));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddSingleton<IConsensusApiClient, TestConsensusApiClient>(_ =>
                    {
                        var apiClient = new TestConsensusApiClient(ServerList);
                        TestApiClient = apiClient;
                        return apiClient;
                    });
                    var consensusService = services.BuildServiceProvider().GetRequiredService<IConsensusService>();
                    TestApiClient.Cluster.Add(_currentServerId, consensusService);
                    ServerList.Add(_currentServerId, "http://localhost/");
                });
            });
            _client = _factory.CreateClient();
        }
    }
}