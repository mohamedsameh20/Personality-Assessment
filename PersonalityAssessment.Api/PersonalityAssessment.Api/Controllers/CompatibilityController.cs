using Microsoft.AspNetCore.Mvc;
using PersonalityAssessment.Api.Services;

namespace PersonalityAssessment.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompatibilityController : ControllerBase
    {
        private readonly ICompatibilityService _compatibilityService;
        private readonly ILogger<CompatibilityController> _logger;

        public CompatibilityController(ICompatibilityService compatibilityService, ILogger<CompatibilityController> logger)
        {
            _compatibilityService = compatibilityService;
            _logger = logger;
        }

        /// <summary>
        /// Find compatible matches for a user
        /// </summary>
        [HttpGet("matches/{userId}")]
        public async Task<ActionResult> FindMatches(int userId, [FromQuery] int limit = 10)
        {
            try
            {
                var matches = await _compatibilityService.FindCompatibleMatches(userId, limit);
                return Ok(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding matches for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Calculate compatibility score between two users
        /// </summary>
        [HttpPost("calculate")]
        public async Task<ActionResult> CalculateCompatibility([FromBody] CalculateCompatibilityRequest request)
        {
            try
            {
                var analysis = await _compatibilityService.GetDetailedCompatibilityAnalysis(request.UserId1, request.UserId2);
                
                if (analysis == null)
                {
                    return BadRequest("Unable to calculate compatibility. Please ensure both users have completed personality assessments.");
                }

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating compatibility between users {UserId1} and {UserId2}", request.UserId1, request.UserId2);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get detailed compatibility analysis between two users
        /// </summary>
        [HttpGet("analysis/{userId1}/{userId2}")]
        public async Task<ActionResult> GetCompatibilityAnalysis(int userId1, int userId2)
        {
            try
            {
                var analysis = await _compatibilityService.GetDetailedCompatibilityAnalysis(userId1, userId2);
                
                if (analysis == null)
                {
                    return NotFound("Compatibility analysis not available. Please ensure both users have completed personality assessments.");
                }

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compatibility analysis between users {UserId1} and {UserId2}", userId1, userId2);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get compatibility statistics for a user
        /// </summary>
        [HttpGet("stats/{userId}")]
        public async Task<ActionResult> GetCompatibilityStats(int userId)
        {
            try
            {
                var matches = await _compatibilityService.FindCompatibleMatches(userId, 100); // Get more for stats

                var stats = new CompatibilityStats
                {
                    TotalPotentialMatches = matches.Count,
                    HighCompatibilityMatches = matches.Count(m => m.CompatibilityScore >= 80),
                    GoodCompatibilityMatches = matches.Count(m => m.CompatibilityScore >= 60 && m.CompatibilityScore < 80),
                    ModerateCompatibilityMatches = matches.Count(m => m.CompatibilityScore >= 40 && m.CompatibilityScore < 60),
                    AverageCompatibilityScore = matches.Any() ? matches.Average(m => m.CompatibilityScore) : 0,
                    BestMatchScore = matches.Any() ? matches.Max(m => m.CompatibilityScore) : 0,
                    BestMatchUser = matches.FirstOrDefault()?.UserName ?? "No matches found"
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compatibility stats for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }
    }

    // Request/Response models
    public class CalculateCompatibilityRequest
    {
        public int UserId1 { get; set; }
        public int UserId2 { get; set; }
    }

    public class CompatibilityStats
    {
        public int TotalPotentialMatches { get; set; }
        public int HighCompatibilityMatches { get; set; }
        public int GoodCompatibilityMatches { get; set; }
        public int ModerateCompatibilityMatches { get; set; }
        public double AverageCompatibilityScore { get; set; }
        public double BestMatchScore { get; set; }
        public string BestMatchUser { get; set; } = string.Empty;
    }
}
