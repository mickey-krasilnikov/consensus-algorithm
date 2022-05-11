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
using ConsensusAlgorithm.Core.Services.ServerStatusService;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using System.Threading.Tasks;
using FluentAssertions;
using ConsensusAlgorithm.DTO.AppendEntries;

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
            _otherServer1.SetupGet(s => s.Id).Returns("remote_server_1");

            _otherServer2 = new Mock<IConsensusApiClient>();
            _otherServer2.SetupGet(s => s.Id).Returns("remote_server_2");

            _otherServer3 = new Mock<IConsensusApiClient>();
            _otherServer3.SetupGet(s => s.Id).Returns("remote_server_3");

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
                    { _otherServer1.Object.Id, "mockUrl1" },
                    { _otherServer2.Object.Id, "mockUrl2" },
                    { _otherServer3.Object.Id, "mockUrl3" },
                }
            };

            _timeoutMock = new Mock<ITimeoutService>();
            _statusMock = new Mock<IServerStatusService>();
            _statusMock.SetupGet(s => s.Id).Returns("local_server");

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

        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public async Task WhenStatusIsNotLeader_ActiveLeaderUnknown_ReturnNotSuccessfullResponse_AppendEntriesExternalTestAsync(ServerStatus state)
        {
            // Assign
            _statusMock.SetupGet(s => s.State).Returns(state);
            _statusMock.SetupProperty(s => s.LeaderId, null);
            _statusMock.SetupGet(s => s.IsLeader).Returns(false);
            _statusMock.SetupGet(s => s.HasLeader).Returns(false);

            // Act
            var result = await _service.AppendEntriesExternalAsync(new AppendEntriesExternalRequest
            {
                Commands = new List<string> { "CLEAR X X", "SET X X" }
            });

            // Assert
            result.Success.Should().Be(false);

            // checking that request is not forwarded
            _otherServer1.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            _otherServer2.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            _otherServer3.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());

            //checking that it's not acting as a leader
            _otherServer1.Verify(s => s.AppendEntriesInternalAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
            _otherServer2.Verify(s => s.AppendEntriesInternalAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
            _otherServer3.Verify(s => s.AppendEntriesInternalAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
        }

        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public async Task WhenStatusIsNotLeader_ActiveLeaderKnown_ForwardRequestToLeader_AppendEntriesExternalTestAsync(ServerStatus state)
        {
            // Assign
            _statusMock.SetupGet(s => s.State).Returns(state);
            _statusMock.SetupProperty(s => s.LeaderId, _otherServer1.Object.Id);
            _statusMock.SetupGet(s => s.IsLeader).Returns(false);
            _statusMock.SetupGet(s => s.HasLeader).Returns(true);
            _otherServer1
                .Setup(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()))
                .ReturnsAsync(new AppendEntriesExternalResponse { Success = true });

            // Act
            var result = await _service.AppendEntriesExternalAsync(new AppendEntriesExternalRequest
            {
                Commands = new List<string> { "CLEAR X X", "SET X X" }
            });

            //Assert
            result.Success.Should().Be(true);
            // checking that it has been redirected only to leader
            _otherServer1.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Once());
            _otherServer2.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            _otherServer3.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());

            //checking that it's not acting as a leader
            _otherServer1.Verify(s => s.AppendEntriesInternalAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
            _otherServer2.Verify(s => s.AppendEntriesInternalAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
            _otherServer3.Verify(s => s.AppendEntriesInternalAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
        }

        [TestCase(ServerStatus.Leader)]
        public async Task WhenStatusIsNotLeader_AppendLogsAndDoNotForwardRequest_AppendEntriesExternalTestAsync(ServerStatus state)
        {
            // Assign
            _statusMock.SetupGet(s => s.State).Returns(state);
            _statusMock.SetupProperty(s => s.LeaderId, _statusMock.Object.Id);
            _statusMock.SetupGet(s => s.IsLeader).Returns(true);
            _statusMock.SetupGet(s => s.HasLeader).Returns(true);

            // Act
            var result = await _service.AppendEntriesExternalAsync(new AppendEntriesExternalRequest
            {
                Commands = new List<string> { "CLEAR X X", "SET X X" }
            });

            //AssertAppendEntriesInternalAsync
            result.Success.Should().Be(true);

            // checking that request is not forwarded
            _otherServer1.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            _otherServer2.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            _otherServer3.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());

            // checking that it distribute the logs between followers
            _otherServer1.Verify(s => s.AppendEntriesInternalAsync(It.IsAny<AppendEntriesRequest>()), Times.Once());
            _otherServer2.Verify(s => s.AppendEntriesInternalAsync(It.IsAny<AppendEntriesRequest>()), Times.Once());
            _otherServer3.Verify(s => s.AppendEntriesInternalAsync(It.IsAny<AppendEntriesRequest>()), Times.Once());
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
