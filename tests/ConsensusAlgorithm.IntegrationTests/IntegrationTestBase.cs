using ConsensusAlgorithm.DTO.Heartbeat;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConsensusAlgorithm.IntegrationTests
{
    public abstract class IntegrationTestBase
    {
        private const string _localhost = "http://localhost";

        public int DesiredAmountOfServers => 5;

        public Dictionary<int, HttpClient> Cluster { get; } = new ();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var urls = BuildUrlDictionary();
            var currentServerId = 1;
            while (currentServerId <= DesiredAmountOfServers)
            {
                var factory = new WebApplicationFactory<Program>()
                    .WithWebHostBuilder(builder =>
                    {
                        builder.UseEnvironment("IntegrationTesting");
                        builder.UseSetting("ConsensusCluster:CurrentServerId", $"{currentServerId}");
                        for (var i = 1; i <= DesiredAmountOfServers; i++)
                        {
                            builder.UseSetting($"ConsensusCluster:ServerList:{i}", $"{urls[i]}");
                        }
                    });

                var client = factory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri($"{urls[currentServerId]}")
                });

                Cluster.Add(currentServerId, client);
                currentServerId++;
            }
        }

        private Dictionary<int, string> BuildUrlDictionary()
        {
            var urlDictionary = new Dictionary<int, string>();
            for (var i = 1; i <= DesiredAmountOfServers; i++)
            {
                urlDictionary.Add(i, $"{_localhost}:717{i}");
            }
            return urlDictionary;
        }
    }
}


