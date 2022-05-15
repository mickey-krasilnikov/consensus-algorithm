using ConsensusAlgorithm.Core.ApiClient;
using ConsensusAlgorithm.Core.Services.ServerStatusService;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.IntegrationTests.TestServices;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ConsensusAlgorithm.IntegrationTests
{
    [TestFixture]
    public class IntegrationTests : IntegrationTestBase
    {
        internal override int DesiredAmountOfServers => 5;

        [Test]
        public async Task HealthCheckTest()
        {
            // Arrange

            // Act
            var response = await Client.GetAsync(MaintenanceApiUrlConstants.HealthCheck);

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task OnStartup_LeaderSelectedTest()
        {
            Thread.Sleep(500); // to skip first election timeout

            // Arrange

            // Act
            var leaderId = JsonSerializer.Deserialize<string>(await Client.GetStringAsync(MaintenanceApiUrlConstants.GetLeaderId));

            var state = JsonSerializer.Deserialize<string>(await Client.GetStringAsync(MaintenanceApiUrlConstants.GetState));

            // Assert
            leaderId.Should().NotBeNullOrEmpty();
            state.Should().NotBeNullOrEmpty();
            if (leaderId == CurrentServerId)
            {
                state.Should().Be(ServerStatus.Leader.ToString());
                TestApiClient.Statuses.All(s => s.Value.State == ServerStatus.Follower).Should().BeTrue();
            }
            else
            {
                state.Should().NotBe(ServerStatus.Leader.ToString());
                TestApiClient.Statuses.All(s => s.Value.State == ServerStatus.Follower).Should().BeFalse();
            }
        }

        [Test]
        public async Task HeartbeatTest()
        {
            // Arrange
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = new HeartbeatRequest { LeaderId = "3", Term = 100 };
            var data = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync(ConsensusApiUrlConstants.HeartbeatUrl, data);
            var heartbeat = JsonSerializer.Deserialize<HeartbeatResponse>(await response.Content.ReadAsStringAsync(), options);
            var leaderId = JsonSerializer.Deserialize<string>(await Client.GetStringAsync(MaintenanceApiUrlConstants.GetLeaderId));
            var state = JsonSerializer.Deserialize<string>(await Client.GetStringAsync(MaintenanceApiUrlConstants.GetState));

            // Assert
            response.EnsureSuccessStatusCode();
            leaderId.Should().Be("3");
            state.Should().Be(ServerStatus.Follower.ToString());
            heartbeat.Term.Should().Be(100);
            heartbeat.Success.Should().BeTrue();
        }
    }
}
