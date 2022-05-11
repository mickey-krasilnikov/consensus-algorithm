using ConsensusAlgorithm.Core.Models;
using NUnit.Framework;

namespace ConsensusAlgorithm.UnitTests.Models
{
    [TestFixture]
    public class ServerTests
    {
        [TestCaseSource(nameof(ServerTestCases))]
        public void ServerTest(string id)
        {
            Assert.DoesNotThrow(() => new Server(id));
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
