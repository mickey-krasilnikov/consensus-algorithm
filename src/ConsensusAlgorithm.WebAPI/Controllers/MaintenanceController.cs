using ConsensusAlgorithm.Core.Services.ServerStatusService;
using Microsoft.AspNetCore.Mvc;

namespace ConsensusAlgorithm.WebAPI.Controllers
{
    [ApiController]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class MaintenanceController : ControllerBase
    {
        private readonly ILogger<ConsensusController> _logger;
        private readonly IServerStatusService _statusService;

        public MaintenanceController(ILogger<ConsensusController> logger, IServerStatusService statusService)
        {
            _logger = logger;
            _statusService = statusService;
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("healthz")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult HealthCheck()
        {
            return Ok();
        }

        /// <summary>
        /// Get server id endpoint
        /// </summary>
        [HttpGet("serverId")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        public IActionResult GetServerId()
        {
            return Ok(_statusService.Id);
        }

        /// <summary>
        /// Get state endpoint
        /// </summary>
        [HttpGet("state")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        public IActionResult GetState()
        {
            return Ok(_statusService.State.ToString());
        }

        /// <summary>
        /// Get leader id endpoint
        /// </summary>
        [HttpGet("leaderId")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        public IActionResult GetLeaderId()
        {
            return Ok(_statusService.LeaderId);
        }
    }
}
