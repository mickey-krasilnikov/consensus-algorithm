using ConsensusAlgorithm.Core.Services.ConsensusService;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.AppendEntriesExternal;
using ConsensusAlgorithm.DTO.Heartbeat;
using ConsensusAlgorithm.DTO.RequestVote;
using Microsoft.AspNetCore.Mvc;

namespace ConsensusAlgorithm.WebAPI.Controllers
{
    [ApiController]
	[Consumes("application/json")]
	[Produces("application/json")]
    [Route("api/[controller]")]
    public class ConsensusController : ControllerBase
	{
		private readonly ILogger<ConsensusController> _logger;
		private readonly IConsensusService _consensusService;

		public ConsensusController(ILogger<ConsensusController> logger, IConsensusService consensusService)
		{
			_logger = logger;
			_consensusService = consensusService;
		}

        /// <summary>
        /// Endpoint to send commands to the leader from outside of the consensus cluster
        /// </summary>
        /// <param name="request">Request contains the list of commands</param>
        /// <returns></returns>
		[HttpPost("appendEntriesExternal")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AppendEntriesExternalResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(AppendEntriesExternalResponse))]
        public async Task<ActionResult<AppendEntriesExternalResponse>> AppendEntriesExternalAsync(AppendEntriesExternalRequest request)
		{
			var response = await _consensusService.AppendEntriesExternalAsync(request);
			return response.Success ? Ok(response) : BadRequest(response);
		}

		/// <summary>
		/// Invoked by leader to replicate log entries and discover inconsistencies
		/// Also used as heartbeat
		/// </summary>
		/// <param name="request">AppendEntriesRequest</param>
		/// <returns>AppendEntriesResponse</returns>
		[HttpPost("appendEntries")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AppendEntriesExternalResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(AppendEntriesExternalResponse))]
        public ActionResult<AppendEntriesResponse> AppendEntries(AppendEntriesRequest request)
		{
			var response = _consensusService.AppendEntries(request);
			return response.Success ? Ok(response) : BadRequest(response);
		}

		/// <summary>
		/// Invoked by candidates to gather votes
		/// </summary>
		/// <param name="request">Request Vote Request</param>
		/// <returns>Request Vote Response</returns>
		[HttpPost("requestVote")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AppendEntriesExternalResponse))]
        [ProducesDefaultResponseType]
        public ActionResult<VoteResponse> RequestVote(VoteRequest request)
		{
			var response = _consensusService.RequestVote(request);
			return Ok(response);
		}

		/// <summary>
		/// Heartbeat endpoint
		/// </summary>
		[HttpPost("heartbeat")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AppendEntriesExternalResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(AppendEntriesExternalResponse))]
        public ActionResult<HeartbeatResponse> SendHeartbeat(HeartbeatRequest request)
		{
			var response = _consensusService.Heartbeat(request);
			return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}