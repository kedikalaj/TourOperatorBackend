using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using TourOperator.Application.Interfaces;

namespace TourOperator.Controllers
{
    [ApiController]
    [Route("api/touroperators/{tourOperatorId:guid}")]
    public class TourOperatorController : ControllerBase
    {
        private readonly ICsvProcessingService _csvProcessingService;

        public TourOperatorController(ICsvProcessingService csvProcessingService)
        {
            _csvProcessingService = csvProcessingService;
        }

        // POST /api/touroperators/{tourOperatorId}/pricing-upload
        [HttpPost("pricing-upload")]
        [Authorize(Roles = "TourOperator")]
        public async Task<IActionResult> Upload([FromRoute] Guid tourOperatorId, [FromForm] IFormFile file, [FromForm] string connectionId, CancellationToken ct)
        {
            // Authorization: ensure user's tourOperatorId matches route
            var userTourOpClaim = User.Claims.FirstOrDefault(c => c.Type == "tourOperatorId")?.Value;
            if (string.IsNullOrEmpty(userTourOpClaim) || Guid.Parse(userTourOpClaim) != tourOperatorId)
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest("File is missing");

            Log.Information("Upload started for tourOperatorId={TourOperatorId} by user={User}", tourOperatorId, User.Identity?.Name);

            using var stream = file.OpenReadStream();
            await _csvProcessingService.ProcessCsvAsync(stream, tourOperatorId, connectionId, ct);

            Log.Information("Upload finished for tourOperatorId={TourOperatorId}", tourOperatorId);
            return Accepted(new { message = "File processing started/completed" });
        }
    }
}
