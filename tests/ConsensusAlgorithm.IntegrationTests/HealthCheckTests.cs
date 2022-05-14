using NUnit.Framework;
using System.Threading.Tasks;

namespace ConsensusAlgorithm.IntegrationTests
{
    [TestFixture]
    public class HealthCheckTests : IntegrationTestBase
    {
        [TestCase("/api/consensus/healthz")]
        public async Task HealthCheckTest(string url)
        {
            // Arrange

            // Act
            var response1 = await Cluster[1].GetAsync(url);
            var response2 = await Cluster[2].GetAsync(url);
            var response3 = await Cluster[3].GetAsync(url);
            var response4 = await Cluster[4].GetAsync(url);
            var response5 = await Cluster[5].GetAsync(url);

            // Assert
            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();
            response3.EnsureSuccessStatusCode();
            response4.EnsureSuccessStatusCode();
            response5.EnsureSuccessStatusCode();
        }
    }
}
