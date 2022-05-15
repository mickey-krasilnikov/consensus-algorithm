using ConsensusAlgorithm.Core.Services.ServerStatusService;
using FluentAssertions;
using NUnit.Framework;
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
            var response = await Client.GetAsync(ApiMaintenanceUrlConstants.HealthCheck);

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task OnStartup_LeaderSelectedTest()
        {
            Thread.Sleep(500); // to skip first election timeout

            // Arrange

            // Act
            var leaderId = JsonSerializer.Deserialize<string>(await Client.GetStringAsync(ApiMaintenanceUrlConstants.GetLeaderId));

            var state = JsonSerializer.Deserialize<string>(await Client.GetStringAsync(ApiMaintenanceUrlConstants.GetState));

            // Assert
            leaderId.Should().NotBeNullOrEmpty();
            state.Should().NotBeNullOrEmpty();
            if (leaderId == CurrentServerId)
                state.Should().Be(ServerStatus.Leader.ToString());
            else
                state.Should().NotBe(ServerStatus.Leader.ToString());
        }
    }
}
