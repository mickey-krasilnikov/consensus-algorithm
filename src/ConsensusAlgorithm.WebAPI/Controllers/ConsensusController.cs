using ConsensusAlgorithm.Core.Services;
using ConsensusAlgorithm.DTO.AppendEntries;
using ConsensusAlgorithm.DTO.RequestVote;
using Microsoft.AspNetCore.Mvc;

namespace ConsensusAlgorithm.WebAPI.Controllers
{
	[ApiController]
	[Consumes("application/json")]
	[Produces("application/json")]
	[Route("api/consensus")]
	public class ConsensusController : ControllerBase
	{
		private readonly ILogger<ConsensusController> _logger;
		private readonly IConsensusService _consensusService;

		public ConsensusController(ILogger<ConsensusController> logger, ConsensusService consensusService)
		{
			_logger = logger;
			_consensusService = consensusService;
		}

		/// <summary>
		/// Invoked by candidates to gather votes
		/// </summary>
		/// <param name="request">Request Vote Request</param>
		/// <returns>Request Vote Response</returns>
		[HttpPost("requestVote")]
		public ActionResult<RequestVoteResponse> RequestVoteInternal(RequestVoteRequest request)
		{
			var response = _consensusService.RequestVoteInternal(request);
			return Ok(response);
		}

		/// <summary>
		/// Invoked by leader to replicate log entries and discover inconsistencies
		/// Also used as heartbeat
		/// </summary>
		/// <param name="request">AppendEntriesRequest</param>
		/// <returns>AppendEntriesResponse</returns>
		[HttpPost("appendEntries")]
		public ActionResult<AppendEntriesResponse> AppendEntriesInternal(AppendEntriesRequest request)
		{
			var response = _consensusService.AppendEntriesInternal(request);
			return response.Success ? Ok(response) : BadRequest(response);
		}

		[HttpPost("appendEntriesExternal")]
		public async Task<ActionResult<AppendEntriesExternalResponse>> AppendEntriesExternalAsync(AppendEntriesExternalRequest request)
		{
			var response = await _consensusService.AppendEntriesExternalAsync(request);
			return response.Success ? Ok(response) : BadRequest(response);
		}

		/// <summary>
		/// Heartbeat endpoint
		/// </summary>
		[HttpPost("heartbeat")]
		public ActionResult<HeartbeatResponse> SendHeartbeatInternal(HeartbeatRequest request)
		{
			var response = _consensusService.Heartbeat(request);
			return response.Success ? Ok(response) : BadRequest(response);
		}
	}
}