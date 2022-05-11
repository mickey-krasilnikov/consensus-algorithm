using NUnit.Framework;
using ConsensusAlgorithm.DataAccess;
using Moq;
using ConsensusAlgorithm.Core.StateMachine;
using Microsoft.Extensions.Logging;
using ConsensusAlgorithm.Core.ApiClient;
using System.Collections.Generic;
using ConsensusAlgorithm.Core.Configuration;
using System.Threading;
using ConsensusAlgorithm.DataAccess.Entities;
using ConsensusAlgorithm.Core.Services.ConsensusService;
using ConsensusAlgorithm.Core.Services.TimeoutService;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.Core.Services.ServerStatusService;

namespace ConsensusAlgorithm.UnitTests.Services
{
    [TestFixture]
    public class ConsensusServiceTests : TestBase
    {
        private readonly CancellationTokenSource _cts = new();

        private Mock<IConsensusRepository> _repoMock = null!;
        private Mock<IStateMachine> _stateMachineMock = null!;
        private Mock<ILogger<ConsensusService>> _loggerMock = null!;
        private Mock<IConsensusApiClient> _otherServer1 = null!;
        private Mock<IConsensusApiClient> _otherServer2 = null!;
        private Mock<IConsensusApiClient> _otherServer3 = null!;
        private ConsensusClusterConfig _config = null!;
        private Mock<ITimeoutService> _timeoutMock = null!;
        private Mock<IServerStatusService> _statusMock = null!;
        private IConsensusService _service = null!;
        private IList<IConsensusApiClient> _otherServers = null!;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IConsensusRepository>();
            _stateMachineMock = new Mock<IStateMachine>();
            _loggerMock = new Mock<ILogger<ConsensusService>>();
            _otherServer1 = new Mock<IConsensusApiClient>();
            _otherServer2 = new Mock<IConsensusApiClient>();
            _otherServer3 = new Mock<IConsensusApiClient>();
            _otherServers = new List<IConsensusApiClient>
            {
                _otherServer1.Object,
                _otherServer2.Object,
                _otherServer3.Object,
            };
            _config = new ConsensusClusterConfig
            {
                CurrentServerId = "1",
                ServerList = new Dictionary<string, string>()
                {
                    { _otherServer1.Name, "mockUrl1" },
                    { _otherServer2.Name, "mockUrl2" },
                    { _otherServer3.Name, "mockUrl3" },
                }
            };
            _timeoutMock = new Mock<ITimeoutService>();
            _statusMock = new Mock<IServerStatusService>();
            _service = new ConsensusService
            (
                _repoMock.Object,
                _stateMachineMock.Object,
                _loggerMock.Object,
                _otherServers,
                _timeoutMock.Object,
                _statusMock.Object
            );
        }

        [Test]
        public void StartTest()
        {
            _repoMock.Setup(r => r.GetLogEntries()).Returns(new List<LogEntity>());
            _timeoutMock.Setup(r => r.GetRandomTimeout()).Returns(Timeout.Infinite);
            Assert.DoesNotThrowAsync(() => _service.StartAsync(_cts.Token));
            _cts.Cancel();
        }

        [Test]
        public void StopTest()
        {
            Assert.DoesNotThrowAsync(() => _service.StopAsync(CancellationToken.None));
        }

        public void AppendEntriesExternalTest()
        {
            _service.AppendEntriesExternalAsync(new AppendEntriesExternalRequest
            {
                Commands = new List<string> { "CLEAR X X", "SET X X" }
            });
        }

        public void AppendEntriesInternalTest()
        {
        }

        public void RequestVoteInternalTest()
        {
        }

        public void HeartbeatTest()
        {
        }
    }
}
