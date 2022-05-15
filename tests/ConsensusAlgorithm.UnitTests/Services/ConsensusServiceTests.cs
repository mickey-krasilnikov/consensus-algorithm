using NUnit.Framework;
using ConsensusAlgorithm.DataAccess;
using Moq;
using ConsensusAlgorithm.Core.StateMachine;
using Microsoft.Extensions.Logging;
using ConsensusAlgorithm.Core.ApiClient;
using System.Collections.Generic;
using System.Threading;
using ConsensusAlgorithm.DataAccess.Entities;
using ConsensusAlgorithm.Core.Services.ConsensusService;
using ConsensusAlgorithm.Core.Services.ServerStatusService;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using System.Threading.Tasks;
using FluentAssertions;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.RequestVote;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.Core.Services.TimerService;
using ConsensusAlgorithm.Core.Configuration;

namespace ConsensusAlgorithm.UnitTests.Services
{
    [TestFixture]
    public class ConsensusServiceTests
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly string _localServerId = "local_server";
        private readonly string _remoteServer1Id = "remote_server_1";
        private readonly string _remoteServer2Id = "remote_server_2";
        private readonly string _remoteServer3Id = "remote_server_3";

        private Mock<IConsensusRepository> _repoMock = null!;
        private Mock<IStateMachine> _stateMachineMock = null!;
        private Mock<ILogger<ConsensusService>> _loggerMock = null!;
        private Mock<IConsensusApiClient> _apiClient = null!;
        private Mock<ITimerService> _timeoutMock = null!;
        private Mock<IServerStatusService> _statusMock = null!;
        private ConsensusClusterConfig _config = null!;
        private IConsensusService _service = null!;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IConsensusRepository>();
            _stateMachineMock = new Mock<IStateMachine>();
            _loggerMock = new Mock<ILogger<ConsensusService>>();

            var anyHeartbeatReq = It.IsAny<HeartbeatRequest>();
            var successHeartbeatResponse = new HeartbeatResponse { Success = true };

            _apiClient = new Mock<IConsensusApiClient>();
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer1Id, anyHeartbeatReq, null)).ReturnsAsync(successHeartbeatResponse);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer2Id, anyHeartbeatReq, null)).ReturnsAsync(successHeartbeatResponse);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer3Id, anyHeartbeatReq, null)).ReturnsAsync(successHeartbeatResponse);

            _config = new ConsensusClusterConfig
            {
                CurrentServerId = _localServerId,
                ServerList = new Dictionary<string, string>
                {
                    { _localServerId, "localServerBaseUrl" },
                    { _remoteServer1Id, "remoteServer1BaseUrl" },
                    { _remoteServer2Id, "remoteServer2BaseUrl" },
                    { _remoteServer3Id, "remoteServer3BaseUrl" },
                }
            };

            _timeoutMock = new Mock<ITimerService>();
            _statusMock = new Mock<IServerStatusService>();
            _statusMock.SetupGet(s => s.Id).Returns(_localServerId);

            _service = new ConsensusService
            (
                _repoMock.Object,
                _stateMachineMock.Object,
                _loggerMock.Object,
                _apiClient.Object,
                _timeoutMock.Object,
                _statusMock.Object,
                _config
            );
        }

        #region AppendEntriesExternal

        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public async Task AppendEntriesExternal_WhenStatusIsNotLeader_ActiveLeaderUnknown_ReturnNotSuccessfullResponse(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _statusMock.SetupProperty(s => s.LeaderId, null);
            _statusMock.SetupGet(s => s.IsLeader).Returns(false);
            _statusMock.SetupGet(s => s.LeaderId).Returns((string?)null!);
            var request = new AppendEntriesExternalRequest
            {
                Commands = new List<string> { "CLEAR X X", "SET X X" }
            };

            // Act
            var result = await _service.AppendEntriesExternalAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            // checking that request is not forwarded
            _apiClient.Verify(s => s.AppendEntriesExternalAsync(_remoteServer1Id, It.IsAny<AppendEntriesExternalRequest>(), null), Times.Never());
            _apiClient.Verify(s => s.AppendEntriesExternalAsync(_remoteServer2Id, It.IsAny<AppendEntriesExternalRequest>(), null), Times.Never());
            _apiClient.Verify(s => s.AppendEntriesExternalAsync(_remoteServer3Id, It.IsAny<AppendEntriesExternalRequest>(), null), Times.Never());
            //checking that it's not acting as a leader
            _apiClient.Verify(s => s.AppendEntriesAsync(_remoteServer1Id, It.IsAny<AppendEntriesRequest>(), null), Times.Never());
            _apiClient.Verify(s => s.AppendEntriesAsync(_remoteServer2Id, It.IsAny<AppendEntriesRequest>(), null), Times.Never());
            _apiClient.Verify(s => s.AppendEntriesAsync(_remoteServer3Id, It.IsAny<AppendEntriesRequest>(), null), Times.Never());
        }

        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public async Task AppendEntriesExternal_HappyPath_CandidateOrFollower_RequestForwardedToLeader(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _statusMock.SetupProperty(s => s.LeaderId, _remoteServer1Id);
            _statusMock.SetupGet(s => s.IsLeader).Returns(false);
            _apiClient
                .Setup(s => s.AppendEntriesExternalAsync(_remoteServer1Id, It.IsAny<AppendEntriesExternalRequest>(), null))
                .ReturnsAsync(new AppendEntriesExternalResponse { Success = true });
            var request = new AppendEntriesExternalRequest
            {
                Commands = new List<string> { "CLEAR X X", "SET X X" }
            };

            // Act
            var result = await _service.AppendEntriesExternalAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            // checking that request is not forwarded
            _apiClient.Verify(s => s.AppendEntriesExternalAsync(_remoteServer1Id, It.IsAny<AppendEntriesExternalRequest>(), null), Times.Once());
            _apiClient.Verify(s => s.AppendEntriesExternalAsync(_remoteServer2Id, It.IsAny<AppendEntriesExternalRequest>(), null), Times.Never());
            _apiClient.Verify(s => s.AppendEntriesExternalAsync(_remoteServer3Id, It.IsAny<AppendEntriesExternalRequest>(), null), Times.Never());
            //checking that it's not acting as a leader
            _apiClient.Verify(s => s.AppendEntriesAsync(_remoteServer1Id, It.IsAny<AppendEntriesRequest>(), null), Times.Never());
            _apiClient.Verify(s => s.AppendEntriesAsync(_remoteServer2Id, It.IsAny<AppendEntriesRequest>(), null), Times.Never());
            _apiClient.Verify(s => s.AppendEntriesAsync(_remoteServer3Id, It.IsAny<AppendEntriesRequest>(), null), Times.Never());
        }

        [TestCase(ServerStatus.Leader)]
        public async Task AppendEntriesExternal_HappyPath_Leader_LogsAppendedAndAppliedToStateMachine(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _statusMock.SetupProperty(s => s.LeaderId, _statusMock.Object.Id);
            _statusMock.SetupGet(s => s.IsLeader).Returns(true);
            _statusMock.SetupGet(s => s.LeaderId).Returns(default(string?));
            var successIntResponse = new AppendEntriesResponse { Success = true, Term = 0 };
            _apiClient.Setup(s => s.AppendEntriesAsync(_remoteServer1Id, It.IsAny<AppendEntriesRequest>(), null)).ReturnsAsync(successIntResponse);
            _apiClient.Setup(s => s.AppendEntriesAsync(_remoteServer2Id, It.IsAny<AppendEntriesRequest>(), null)).ReturnsAsync(successIntResponse);
            _apiClient.Setup(s => s.AppendEntriesAsync(_remoteServer3Id, It.IsAny<AppendEntriesRequest>(), null)).ReturnsAsync(successIntResponse);
            var request = new AppendEntriesExternalRequest { Commands = new List<string> { "CLEAR X X", "SET X X" } };

            // Act
            var result = await _service.AppendEntriesExternalAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            _repoMock.Verify(r => r.AppendLogEntry(It.IsAny<LogEntity>()), Times.Exactly(2));
            _stateMachineMock.Verify(r => r.Apply(It.IsAny<string>()), Times.Exactly(2));
            // checking that request is not forwarded
            _apiClient.Verify(s => s.AppendEntriesExternalAsync(_remoteServer1Id, It.IsAny<AppendEntriesExternalRequest>(), null), Times.Never());
            _apiClient.Verify(s => s.AppendEntriesExternalAsync(_remoteServer2Id, It.IsAny<AppendEntriesExternalRequest>(), null), Times.Never());
            _apiClient.Verify(s => s.AppendEntriesExternalAsync(_remoteServer3Id, It.IsAny<AppendEntriesExternalRequest>(), null), Times.Never());
            //checking that it's not acting as a leader
            _apiClient.Verify(s => s.AppendEntriesAsync(_remoteServer1Id, It.IsAny<AppendEntriesRequest>(), null), Times.Once());
            _apiClient.Verify(s => s.AppendEntriesAsync(_remoteServer2Id, It.IsAny<AppendEntriesRequest>(), null), Times.Once());
            _apiClient.Verify(s => s.AppendEntriesAsync(_remoteServer3Id, It.IsAny<AppendEntriesRequest>(), null), Times.Once());
        }

        #endregion AppendEntriesExternal

        #region AppendEntries

        [Test]
        public void AppendEntries_WhenTermFromRequest_LessThenCurrentTerm_IgnoreRequest()
        {
            // Arrange         
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new AppendEntriesRequest
            {
                LeaderId = _remoteServer1Id,
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
        public void AppendEntries_WhenTermFromRequest_BiggerThenCurrentTerm_UpdateCurrentTerm_ResetTimeout_StepDown(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(0);
            var request = new AppendEntriesRequest
            {
                LeaderId = _remoteServer1Id,
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
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        [TestCase(ServerStatus.Leader)]
        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public void AppendEntries_WhenTermFromRequest_EqualCurrentTerm_DoNotUpdateCurrentTerm_ResetTimeout_StepDown(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new AppendEntriesRequest
            {
                LeaderId = _remoteServer1Id,
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
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        [TestCase(ServerStatus.Leader)]
        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public void AppendEntries_WhenPrevLogTermFromRequest_NotEqualLocalPrevLogTerm_ResetTimeout_IgnoreRequest(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(2);
            var request = new AppendEntriesRequest
            {
                LeaderId = _remoteServer1Id,
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
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        [TestCase(ServerStatus.Leader)]
        [TestCase(ServerStatus.Candidate)]
        [TestCase(ServerStatus.Follower)]
        public void AppendEntries_HappyPath_LogsAppendedAndAppliedToStateMachine(ServerStatus state)
        {
            // Arrange
            _statusMock.SetupProperty(s => s.State, state);
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new AppendEntriesRequest
            {
                LeaderId = _remoteServer1Id,
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
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        #endregion AppendEntries

        #region RequestVote

        [Test]
        public void RequestVote_WhenTermFromRequest_LessThenCurrentTerm_IgnoreRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(2);
            var request = new VoteRequest
            {
                CandidateId = _remoteServer1Id,
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
                CandidateId = _remoteServer1Id,
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
        public void RequestVote_WhenAlreadyVoted_ForDifferentServer_IgnoreVoteRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new VoteRequest
            {
                CandidateId = _remoteServer1Id,
                Term = 1,
                LastLogIndex = -1,
                LastLogTerm = -1
            };
            _repoMock.Setup(r => r.GetCandidateIdVotedFor(It.IsAny<int>())).Returns(_remoteServer2Id);

            // Act
            var response = _service.RequestVote(request);

            // Assert
            response.VoteGranted.Should().BeFalse();
            response.Term.Should().Be(1);
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Never);
        }

        [Test]
        public void RequestVote_HappyPath_WhenTermsAreEqual_AlreadyVotedForSameServer_VoteAgain()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var voteRequesterId = _remoteServer1Id;
            var request = new VoteRequest
            {
                CandidateId = voteRequesterId,
                Term = 1,
                LastLogIndex = -1,
                LastLogTerm = -1
            };
            _repoMock.Setup(r => r.GetCandidateIdVotedFor(It.IsAny<int>())).Returns(voteRequesterId);

            // Act
            var response = _service.RequestVote(request);

            // Assert
            response.VoteGranted.Should().BeTrue();
            response.Term.Should().Be(1);
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(request.CandidateId, request.Term), Times.Once);
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        [Test]
        public void RequestVote_WhenLastLogIndexFromRequest_LessThenLocalLastLogIndex_IgnoreVoteRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var appendLogsRequest = new AppendEntriesRequest
            {
                LeaderId = _remoteServer1Id,
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
                CandidateId = _remoteServer1Id,
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
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void RequestVote_WhenLastLogTermFromRequest_LessThenLocalLastLogTerm_IgnoreVoteRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(2);
            var appendLogsRequest = new AppendEntriesRequest
            {
                LeaderId = _remoteServer1Id,
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
                CandidateId = _remoteServer1Id,
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
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void RequestVote_HappyPath_VoteGranted()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new VoteRequest
            {
                CandidateId = _remoteServer1Id,
                Term = 1,
                LastLogIndex = -1,
                LastLogTerm = -1
            };

            // Act
            var response = _service.RequestVote(request);

            // Assert
            response.VoteGranted.Should().BeTrue();
            response.Term.Should().Be(1);
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(request.CandidateId, request.Term), Times.Once);
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        #endregion RequestVote

        #region Heartbeat

        [Test]
        public void Heartbeat_WhenTermFromRequest_LessThenCurrentTerm_IgnoreRequest()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(2);
            var request = new HeartbeatRequest
            {
                LeaderId = _remoteServer1Id,
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
                LeaderId = _remoteServer1Id,
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
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        [Test]
        public void Heartbeat_HappyPath_HeartbeatReceived()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(1);
            var request = new HeartbeatRequest
            {
                LeaderId = _remoteServer1Id,
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
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        #endregion Heartbeat

        #region IHostedService

        [Test]
        public void Start_HappyPath()
        {
            // Arrange
            _repoMock.Setup(r => r.GetLogEntries()).Returns(new List<LogEntity>());

            // Act
            var result = _service.StartAsync(_cts.Token);
            _cts.Cancel();

            // Assert
            result.Should().Be(Task.CompletedTask);
            _timeoutMock.Verify(t => t.Initialize(It.IsAny<TimerCallback>(), It.IsAny<TimerCallback>()), Times.Once);
        }

        [Test]
        public void Stop_HappyPath()
        {
            // Act
            var result = _service.StopAsync(CancellationToken.None);

            // Assert
            result.Should().Be(Task.CompletedTask);
            _timeoutMock.Verify(t => t.StopAll(), Times.Once);
        }

        [Test]
        public void Dispose_HappyPath()
        {
            // Act
            _service.Dispose();

            // Assert
            _timeoutMock.Verify(t => t.Dispose(), Times.Once);
        }

        #endregion IHostedService

        #region RunElection_Internal

        [Test]
        public void RunElection_Verify_StatusSetAsCandidate_IncrementCurrentTerm_VoteForHimself()
        {
            // Arrange
            const int currentTerm = 1;
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(currentTerm);

            // Act
            ((ConsensusService)_service).RunElectionAsync(default);

            // Assert
            _statusMock.VerifySet(s => s.State = ServerStatus.Candidate, Times.Once);
            _repoMock.Verify(r => r.SetCurrentTerm(currentTerm + 1), Times.Once);
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(_localServerId, currentTerm + 1), Times.Once);
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        [Test]
        public void RunElection_HappyPath_WhenMajorityVoteFor_BecameLeader()
        {
            // Arrange
            const int currentTerm = 1;
            var anyRequest = It.IsAny<VoteRequest>();

            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(currentTerm);
            var voteFor = new VoteResponse { VoteGranted = true, Term = currentTerm + 1 };
            var voteAgainst = new VoteResponse { VoteGranted = false, Term = currentTerm + 1 };
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer1Id, anyRequest, null)).ReturnsAsync(voteFor);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer2Id, anyRequest, null)).ReturnsAsync(voteFor);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer3Id, anyRequest, null)).ReturnsAsync(voteAgainst);
            _statusMock.SetupProperty(s => s.State, ServerStatus.Candidate);

            // Act
            ((ConsensusService)_service).RunElectionAsync(default);

            // Assert
            _statusMock.VerifySet(s => s.State = ServerStatus.Leader, Times.Once);
            _repoMock.Verify(r => r.SetCurrentTerm(currentTerm + 1), Times.Once);
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(_localServerId, currentTerm + 1), Times.Once);
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        [Test]
        public void RunElection_WhenMajorityDidNotVoteFor_RemainCandidate()
        {
            // Arrange
            const int currentTerm = 1;
            var anyRequest = It.IsAny<VoteRequest>();

            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(currentTerm);
            var voteAgainst = new VoteResponse { VoteGranted = false, Term = currentTerm + 1 };
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer1Id, anyRequest, null)).ReturnsAsync(voteAgainst);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer2Id, anyRequest, null)).ReturnsAsync(voteAgainst);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer3Id, anyRequest, null)).ReturnsAsync(voteAgainst);

            // Act
            ((ConsensusService)_service).RunElectionAsync(default);

            // Assert
            _statusMock.VerifySet(s => s.State = ServerStatus.Candidate, Times.Once);
            _repoMock.Verify(r => r.SetCurrentTerm(currentTerm + 1), Times.Once);
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(_localServerId, currentTerm + 1), Times.Once);
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
            _statusMock.VerifySet(s => s.State = ServerStatus.Leader, Times.Never);
        }

        [Test]
        public void RunElection_WhenTermFromResponse_BiggerThenCurrentTerm_UpdateCurrentTerm_StepDown()
        {
            // Arrange
            const int currentTerm = 1;
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(currentTerm);
            var voteAgainst = new VoteResponse { VoteGranted = false, Term = currentTerm + 2 };
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer1Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync(voteAgainst);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer2Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync(voteAgainst);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer3Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync(voteAgainst);

            // Act
            ((ConsensusService)_service).RunElectionAsync(default);

            // Assert
            _repoMock.Verify(s => s.SetCurrentTerm(currentTerm + 2), Times.Once);
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower, Times.Once);
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(_localServerId, currentTerm + 1), Times.Once);
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
            _statusMock.VerifySet(s => s.State = ServerStatus.Leader, Times.Never);
        }

        [Test]
        public void RunElection_WhenDifferentTermsFromResponse_BiggerThenCurrentTerm_UpdateCurrentTermToMaxTerm_StepDown()
        {
            // Arrange
            const int currentTerm = 1;
            var voteAgainst1 = new VoteResponse { VoteGranted = false, Term = currentTerm + 2 };
            var voteAgainst2 = new VoteResponse { VoteGranted = false, Term = currentTerm + 5 };
            var voteAgainst3 = new VoteResponse { VoteGranted = false, Term = currentTerm + 1 };
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(currentTerm);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer1Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync(voteAgainst1);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer2Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync(voteAgainst2);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer3Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync(voteAgainst3);

            // Act
            ((ConsensusService)_service).RunElectionAsync(default);

            // Assert
            _repoMock.Verify(s => s.SetCurrentTerm(currentTerm + 5), Times.Once);
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower, Times.Once);
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(_localServerId, currentTerm + 1), Times.Once);
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
            _statusMock.VerifySet(s => s.State = ServerStatus.Leader, Times.Never);
        }

        [Test]
        public void RunElection_HappyPath_WhenSomeVotersAreDead_ButAliveServersVotedFor_BecameLeader()
        {
            // Arrange
            const int currentTerm = 1;
            var voteFor = new VoteResponse { VoteGranted = true, Term = currentTerm + 1 };
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(currentTerm);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer1Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync(voteFor);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer2Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync((VoteResponse)null!);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer3Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync((VoteResponse)null!);
            _statusMock.SetupProperty(s => s.State, ServerStatus.Candidate);

            // Act
            ((ConsensusService)_service).RunElectionAsync(default);

            // Assert
            _statusMock.VerifySet(s => s.State = ServerStatus.Leader, Times.Once);
            _repoMock.Verify(r => r.SetCurrentTerm(currentTerm + 1), Times.Once);
            _repoMock.Verify(r => r.SetCandidateIdVotedFor(_localServerId, currentTerm + 1), Times.Once);
            _timeoutMock.Verify(t => t.ResetElectionTimeout(), Times.Once);
        }

        [Test]
        public void RunElection_HappyPath_WhenAllServersAreDead_BecameLeader()
        {
            // Arrange
            const int currentTerm = 1;
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(currentTerm);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer1Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync((VoteResponse)null!);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer2Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync((VoteResponse)null!);
            _apiClient.Setup(s => s.RequestVoteAsync(_remoteServer3Id, It.IsAny<VoteRequest>(), null)).ReturnsAsync((VoteResponse)null!);
            _statusMock.SetupProperty(s => s.State, ServerStatus.Candidate);

            // Act
            ((ConsensusService)_service).RunElectionAsync(default);

            // Assert
            _statusMock.VerifySet(s => s.State = ServerStatus.Leader, Times.Once);
        }

        #endregion RunElection_Internal

        #region SendHeartbeat_Internal

        [Test]
        public void SendHeartbeat_HappyPath_AllFollowersReceivedHeartbeat_HasSameTerm()
        {
            // Arrange
            const int currentTerm = 5;
            var successResponse = new HeartbeatResponse { Success = true, Term = currentTerm };
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(currentTerm);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer1Id, It.IsAny<HeartbeatRequest>(), null)).ReturnsAsync(successResponse);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer2Id, It.IsAny<HeartbeatRequest>(), null)).ReturnsAsync(successResponse);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer3Id, It.IsAny<HeartbeatRequest>(), null)).ReturnsAsync(successResponse);

            // Act
            ((ConsensusService)_service).SendHeartbeatAsync(null);

            // Assert
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower, Times.Never);
            _repoMock.Verify(s => s.SetCurrentTerm(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void SendHeartbeat_FollowersHasBiggerTerm_PickBiggest_UpdateCurrentTerm_StepDown()
        {
            // Arrange
            const int currentTerm = 5;
            var successResponse1 = new HeartbeatResponse { Success = true, Term = currentTerm + 1 };
            var successResponse2 = new HeartbeatResponse { Success = true, Term = currentTerm + 3 };
            var successResponse3 = new HeartbeatResponse { Success = true, Term = currentTerm + 2 };
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(currentTerm);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer1Id, It.IsAny<HeartbeatRequest>(), null)).ReturnsAsync(successResponse1);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer2Id, It.IsAny<HeartbeatRequest>(), null)).ReturnsAsync(successResponse2);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer3Id, It.IsAny<HeartbeatRequest>(), null)).ReturnsAsync(successResponse3);

            // Act
            ((ConsensusService)_service).SendHeartbeatAsync(null);

            // Assert
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower, Times.Once);
            _repoMock.Verify(s => s.SetCurrentTerm(currentTerm + 3), Times.Once);
            _timeoutMock.Verify(t => t.StopHeartbeatTimer(), Times.Once);
            _timeoutMock.Verify(t => t.StartElectionTimer(), Times.Once);
        }

        [Test]
        public void SendHeartbeat_FollowersOffline_StayLeader_BeAMan()
        {
            // Arrange
            const int currentTerm = 5;
            var unsuccessResponse = new HeartbeatResponse { Success = false };
            _repoMock.Setup(r => r.GetCurrentTerm()).Returns(currentTerm);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer1Id, It.IsAny<HeartbeatRequest>(), null)).ReturnsAsync(unsuccessResponse);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer2Id, It.IsAny<HeartbeatRequest>(), null)).ReturnsAsync(unsuccessResponse);
            _apiClient.Setup(s => s.SendHeartbeatAsync(_remoteServer3Id, It.IsAny<HeartbeatRequest>(), null)).ReturnsAsync(unsuccessResponse);

            // Act
            ((ConsensusService)_service).SendHeartbeatAsync(null);

            // Assert
            _statusMock.VerifySet(s => s.State = ServerStatus.Follower, Times.Never);
            _repoMock.Verify(s => s.SetCurrentTerm(It.IsAny<int>()), Times.Never);
        }

        #endregion SendHeartbeat_Internal
    }
}
