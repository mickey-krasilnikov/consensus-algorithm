using ConsensusAlgorithm.Core.ApiClient;
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

        internal IConsensusApiClient ApiClient { get; set; } = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _currentServerId = 1.ToString();
            for (var i = 0; i <= DesiredAmountOfServers; i++) ServerList.Add($"{i}", $"http://test{i}.com");

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTesting");
                builder.UseSetting("ConsensusCluster:CurrentServerId", $"{_currentServerId}");
                foreach (var item in ServerList)
                {
                    builder.UseSetting($"ConsensusCluster:ServerList:{item.Key}", $"https://testhost{item.Key}");
                }

                builder.ConfigureServices(s =>
                {
                    var descriptor = s.SingleOrDefault(d => d.ServiceType == typeof(IConsensusApiClient));
                    if (descriptor != null) s.Remove(descriptor);

                    s.AddSingleton<IConsensusApiClient, TestConsensusApiClient>(_ => 
                    {
                        var apiClient = new TestConsensusApiClient(ServerList);
                        ApiClient = apiClient;
                        return apiClient;
                    });
                });
            });

            _client = _factory.CreateClient();
        }
    }
}