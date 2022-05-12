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
using ConsensusAlgorithm.DTO.RequestVote;
using ConsensusAlgorithm.DTO.Heartbeat;

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

        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public async Task AppendEntriesExternal_WhenStatusIsNotLeader_ActiveLeaderUnknown_ReturnNotSuccessfullResponse(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _statusMock.SetupProperty(s => s.LeaderId, null);
            _statusMock.SetupGet(s => s.IsLeader).Returns(false);
            _statusMock.SetupGet(s => s.HasLeader).Returns(false);
            var request = new AppendEntriesExternalRequest
            {
                Commands = new List<string> { "CLEAR X X", "SET X X" }
            };

            // Act
            var result = await _service.AppendEntriesExternalAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            // checking that request is not forwarded
            _otherServer1.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            _otherServer2.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            _otherServer3.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            //checking that it's not acting as a leader
            _otherServer1.Verify(s => s.AppendEntriesAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
            _otherServer2.Verify(s => s.AppendEntriesAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
            _otherServer3.Verify(s => s.AppendEntriesAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
        }

        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public async Task AppendEntriesExternal_HappyPath_CandidateOrFollower_RequestForwardedToLeader(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _statusMock.SetupProperty(s => s.LeaderId, _otherServer1.Object.Id);
            _statusMock.SetupGet(s => s.IsLeader).Returns(false);
            _statusMock.SetupGet(s => s.HasLeader).Returns(true);
            _otherServer1
                .Setup(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()))
                .ReturnsAsync(new AppendEntriesExternalResponse { Success = true });
            var request = new AppendEntriesExternalRequest
            {
                Commands = new List<string> { "CLEAR X X", "SET X X" }
            };

            // Act
            var result = await _service.AppendEntriesExternalAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            // checking that it has been redirected only to leader
            _otherServer1.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Once());
            _otherServer2.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            _otherServer3.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            //checking that it's not acting as a leader
            _otherServer1.Verify(s => s.AppendEntriesAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
            _otherServer2.Verify(s => s.AppendEntriesAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
            _otherServer3.Verify(s => s.AppendEntriesAsync(It.IsAny<AppendEntriesRequest>()), Times.Never());
        }

        [TestCase(ServerStatus.Leader)]
        public async Task AppendEntriesExternal_HappyPath_Leader_LogsAppendedAndAppliedToStateMachine(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _statusMock.SetupProperty(s => s.LeaderId, _statusMock.Object.Id);
            _statusMock.SetupGet(s => s.IsLeader).Returns(true);
            _statusMock.SetupGet(s => s.HasLeader).Returns(true);
            var request = new AppendEntriesExternalRequest
            {
                Commands = new List<string> { "CLEAR X X", "SET X X" }
            };

            // Act
            var result = await _service.AppendEntriesExternalAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            _repoMock.Verify(r => r.AppendLogEntry(It.IsAny<LogEntity>()), Times.Exactly(2));
            _stateMachineMock.Verify(r => r.Apply(It.IsAny<string>()), Times.Exactly(2));
            // checking that request is not forwarded
            _otherServer1.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            _otherServer2.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            _otherServer3.Verify(s => s.AppendEntriesExternalAsync(It.IsAny<AppendEntriesExternalRequest>()), Times.Never());
            // checking that it distribute the logs between followers
            _otherServer1.Verify(s => s.AppendEntriesAsync(It.IsAny<AppendEntriesRequest>()), Times.Once());
            _otherServer2.Verify(s => s.AppendEntriesAsync(It.IsAny<AppendEntriesRequest>()), Times.Once());
            _otherServer3.Verify(s => s.AppendEntriesAsync(It.IsAny<AppendEntriesRequest>()), Times.Once());
        }

        [Test]
        public void AppendEntries_WhenTermFromRequest_LessThenCurrentTerm_IgnoreRequest()
        {
            // Arrange         
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new AppendEntriesRequest
            {
                LeaderId = _otherServer1.Object.Id,
                Term = 0,
                CommitIndex = -1,
                PrevLogIndex = -1,
                PrevLogTerm = -1,
                Entries = new List<LogEntry>
                {
                    new (){ Command = "CLEAR X X", Index = 0, Term = 0 },
                    new (){ Command = "SET X X", Index = 1, Term = 0 },
                }
            };

            // Act
            var result = _service.AppendEntries(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Term.Should().Be(1);
        }

        [TestCase(ServerStatus.Leader)]
        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public void AppendEntries_WhenTermFromRequest_BiggerThenCurrentTerm_UpdateCurrentTerm_StepDown(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(0);
            _timeoutMock.Setup(r => r.GetRandomTimeout()).Returns(Timeout.Infinite);
            var request = new AppendEntriesRequest
            {
                LeaderId = _otherServer1.Object.Id,
                Term = 1,
                CommitIndex = -1,
                PrevLogIndex = -1,
                PrevLogTerm = -1,
                Entries = new List<LogEntry>
                {
                    new (){ Command = "CLEAR X X", Index = 1, Term = 1 },
                    new (){ Command = "SET X X", Index = 2, Term = 1 },
                }
            };

            // Act
            var response = _service.AppendEntries(request);

            // Assert
            response.Term.Should().Be(1);
            _repoMock.Verify(r => r.SetCurrentTerm(It.IsAny<int>()), Times.Once);
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower);
        }

        [TestCase(ServerStatus.Leader)]
        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public void AppendEntries_WhenTermFromRequest_EqualCurrentTerm_DoNotUpdateCurrentTerm_StepDown(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            _timeoutMock.Setup(r => r.GetRandomTimeout()).Returns(Timeout.Infinite);
            var request = new AppendEntriesRequest
            {
                LeaderId = _otherServer1.Object.Id,
                Term = 1,
                CommitIndex = -1,
                PrevLogIndex = -1,
                PrevLogTerm = -1,
                Entries = new List<LogEntry>
                {
                    new (){ Command = "CLEAR X X", Index = 1, Term = 1 },
                    new (){ Command = "SET X X", Index = 2, Term = 1 },
                }
            };

            // Act
            var result = _service.AppendEntries(request);

            // Assert
            result.Term.Should().Be(1);
            _repoMock.Verify(r => r.SetCurrentTerm(It.IsAny<int>()), Times.Never);
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower);
        }

        [TestCase(ServerStatus.Leader)]
        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public void AppendEntries_WhenPrevLogTermFromRequest_NotEqualLocalPrevLogTerm_IgnoreRequest(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(2);
            _timeoutMock.Setup(r => r.GetRandomTimeout()).Returns(Timeout.Infinite);
            var request = new AppendEntriesRequest
            {
                LeaderId = _otherServer1.Object.Id,
                Term = 2,
                CommitIndex = 2,
                PrevLogIndex = 2,
                PrevLogTerm = 2,
                Entries = new List<LogEntry>
                {
                    new (){ Command = "CLEAR X X", Index = 1, Term = 2 },
                    new (){ Command = "SET X X", Index = 2, Term = 2 },
                }
            };

            // Act
            var result = _service.AppendEntries(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Term.Should().Be(2);
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower);
        }

        [TestCase(ServerStatus.Leader)]
        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public void AppendEntries_HappyPath_LogsAppendedAndAppliedToStateMachine(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            _timeoutMock.Setup(r => r.GetRandomTimeout()).Returns(Timeout.Infinite);
            var request = new AppendEntriesRequest
            {
                LeaderId = _otherServer1.Object.Id,
                Term = 1,
                CommitIndex = -1,
                PrevLogIndex = -1,
                PrevLogTerm = -1,
                Entries = new List<LogEntry>
                {
                    new (){ Command = "CLEAR X X", Index = 1, Term = 1 },
                    new (){ Command = "SET X X", Index = 2, Term = 1 },
                }
            };

            // Act
            var result = _service.AppendEntries(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Term.Should().Be(1);
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower);
            _repoMock.Verify(r => r.AppendLogEntry(It.IsAny<LogEntity>()), Times.Exactly(2));
            _stateMachineMock.Verify(r => r.Apply(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void RequestVote_WhenTermFromRequest_LessThenCurrentTerm_IgnoreRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(2);
            var request = new VoteRequest
            {
                CandidateId = _otherServer1.Object.Id,
                Term = 1,
                LastLogIndex = -1,
                LastLogTerm = -1
            };

            // Act
            var response = _service.RequestVote(request);

            // Assert
            response.VoteGranted.Should().BeFalse();
            response.Term.Should().Be(2);
        }

        [Test]
        public void RequestVote_WhenTermFromRequest_BiggerThenCurrentTerm_UpdateCurrentTerm_StepDown()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new VoteRequest
            {
                CandidateId = _otherServer1.Object.Id,
                Term = 2,
                LastLogIndex = -1,
                LastLogTerm = -1
            };

            // Act
            var response = _service.RequestVote(request);

            // Assert
            response.Term.Should().Be(2);
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower);
            _repoMock.Verify(s => s.SetCurrentTerm(It.IsAny<int>()), Times.Once);
            _repoMock.Verify(s => s.GetCandidateIdVotedFor(It.IsAny<int>()), Times.Once);
        }

        [Test]
        public void RequestVote_WhenServerAlreadyVoted_IgnoreVoteRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            _repoMock.Setup(r => r.GetCandidateIdVotedFor(It.IsAny<int>())).Returns(_otherServer1.Object.Id);

            var request = new VoteRequest
            {
                CandidateId = _otherServer1.Object.Id,
                Term = 1,
                LastLogIndex = -1,
                LastLogTerm = -1
            };

            // Act
            var response = _service.RequestVote(request);

            // Assert
            response.VoteGranted.Should().BeFalse();
            response.Term.Should().Be(1);
        }

        [Test]
        public void RequestVote_WhenLastLogIndexFromRequest_LessThenLocalLastLogIndex_IgnoreVoteRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            _timeoutMock.Setup(r => r.GetRandomTimeout()).Returns(Timeout.Infinite);
            var appendLogsRequest = new AppendEntriesRequest
            {
                LeaderId = _otherServer1.Object.Id,
                Term = 1,
                CommitIndex = -1,
                PrevLogIndex = -1,
                PrevLogTerm = -1,
                Entries = new List<LogEntry>
                {
                    new (){ Command = "CLEAR X X", Index = 1, Term = 1 },
                    new (){ Command = "SET X X", Index = 2, Term = 1 },
                }
            };
            var voteRequest = new VoteRequest
            {
                CandidateId = _otherServer1.Object.Id,
                Term = 1,
                LastLogIndex = -1,
                LastLogTerm = -1
            };

            // Act
            var appendResponse = _service.AppendEntries(appendLogsRequest);
            var voteResponse = _service.RequestVote(voteRequest);

            // Assert
            appendResponse.Success.Should().BeTrue();
            voteResponse.VoteGranted.Should().BeFalse();
            voteResponse.Term.Should().Be(1);
        }

        [Test]
        public void RequestVote_WhenLastLogTermFromRequest_LessThenLocalLastLogTerm_IgnoreVoteRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(2);
            _timeoutMock.Setup(r => r.GetRandomTimeout()).Returns(Timeout.Infinite);
            var appendLogsRequest = new AppendEntriesRequest
            {
                LeaderId = _otherServer1.Object.Id,
                Term = 2,
                CommitIndex = -1,
                PrevLogIndex = -1,
                PrevLogTerm = -1,
                Entries = new List<LogEntry>
                {
                    new (){ Command = "CLEAR X X", Index = 1, Term = 2 },
                    new (){ Command = "SET X X", Index = 2, Term = 2 },
                }
            };
            var voteRequest = new VoteRequest
            {
                CandidateId = _otherServer1.Object.Id,
                Term = 2,
                LastLogIndex = 2,
                LastLogTerm = 1
            };

            // Act
            var appendResponse = _service.AppendEntries(appendLogsRequest);
            var voteResponse = _service.RequestVote(voteRequest);

            // Assert
            appendResponse.Success.Should().BeTrue();
            voteResponse.VoteGranted.Should().BeFalse();
            voteResponse.Term.Should().Be(2);
        }

        [Test]
        public void RequestVote_HappyPath_VoteGranted()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new VoteRequest
            {
                CandidateId = _otherServer1.Object.Id,
                Term = 1,
                LastLogIndex = -1,
                LastLogTerm = -1
            };

            // Act
            var response = _service.RequestVote(request);

            // Assert
            response.VoteGranted.Should().BeTrue();
            response.Term.Should().Be(1);
        }

        [Test]
        public void Heartbeat_WhenTermFromRequest_LessThenCurrentTerm_IgnoreRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(2);
            var request = new HeartbeatRequest
            {
                LeaderId = _otherServer1.Object.Id,
                Term = 1
            };

            // Act
            var response = _service.Heartbeat(request);

            // Assert
            response.Success.Should().BeFalse();
            response.Term.Should().Be(2);
        }

        [Test]
        public void Heartbeat_HappyPath_WhenTermFromRequest_BiggerThenCurrentTerm_UpdateCurrentTerm()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new HeartbeatRequest
            {
                LeaderId = _otherServer1.Object.Id,
                Term = 2
            };

            // Act
            var response = _service.Heartbeat(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Term.Should().Be(2);
            _repoMock.Verify(s => s.SetCurrentTerm(It.IsAny<int>()), Times.Once);
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower);
            _statusMock.VerifySet(s => s.LeaderId = request.LeaderId);
        }

        [Test]
        public void Heartbeat_HappyPath_HeartbeatReceived()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new HeartbeatRequest
            {
                LeaderId = _otherServer1.Object.Id,
                Term = 1
            };

            // Act
            var response = _service.Heartbeat(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Term.Should().Be(1);
            _repoMock.Verify(s => s.SetCurrentTerm(It.IsAny<int>()), Times.Never);
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower);
            _statusMock.VerifySet(s => s.LeaderId = request.LeaderId);
        }

        [Test]
        public void StartTest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetLogEntries()).Returns(new List<LogEntity>());
            _timeoutMock.Setup(r => r.GetRandomTimeout()).Returns(Timeout.Infinite);

            // Act
            var result = _service.StartAsync(_cts.Token);
            _cts.Cancel();

            // Assert
            result.Should().Be(Task.CompletedTask);
        }

        [Test]
        public void StopTest()
        {
            // Act
            var result = _service.StopAsync(CancellationToken.None);

            // Assert
            result.Should().Be(Task.CompletedTask);
        }
    }
}
