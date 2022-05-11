using ConsensusAlgorithm.Core.Services.ServerStatusService;
using NUnit.Framework;

namespace ConsensusAlgorithm.UnitTests.Services
{
    [TestFixture]
    public class ServerStatusServiceTests
    {
        [TestCaseSource(nameof(ServerTestCases))]
        public void ServerTest(string id)
        {
            Assert.DoesNotThrow(() => new ServerStatusService(id));
        }

        public readonly static object[] ServerTestCases =
        {
            "1",
            "A",
            "2B",
            "21F27B26",
        };
    }
}
