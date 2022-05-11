using ConsensusAlgorithm.Core.Services.TimeoutService;
using FluentAssertions;
using NUnit.Framework;

namespace ConsensusAlgorithm.UnitTests.Providers
{
    [TestFixture]
    public class TimeoutServiceTests
    {
        private ITimeoutService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new TimeoutService();
        }

        [Test]
        public void GetRandomTimeoutTest()
        {
            var rnd1 = _service.GetRandomTimeout();
            var rnd2 = _service.GetRandomTimeout();
            rnd1.Should().NotBe(rnd2);
        }
    }
}
