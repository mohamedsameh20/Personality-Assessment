using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalityAssessment.Api.Data;
using PersonalityAssessment.Api.Models;
using PersonalityAssessment.Api.Services;

namespace PersonalityAssessment.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext context, IUserService userService, ILogger<UsersController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get user's personality profile including trait scores
        /// </summary>
        [HttpGet("{userId}/profile")]
        public async Task<ActionResult<UserProfileResponse>> GetUserProfile(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.PersonalityProfile)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                if (user.PersonalityProfile == null)
                {
                    return NotFound("User has not completed personality assessment");
                }

                // Parse the profile data to get trait scores
                var profileData = System.Text.Json.JsonSerializer.Deserialize<AssessmentResult>(user.PersonalityProfile.ProfileData);
                
                var profile = new UserProfileResponse
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    MbtiType = user.PersonalityProfile.MbtiType,
                    Confidence = user.PersonalityProfile.Confidence,
                    CreatedDate = user.PersonalityProfile.CreatedDate,
                    TraitScores = profileData?.TraitScores ?? new List<TraitScore>()
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get user statistics (assessment count, match count, etc.)
        /// </summary>
        [HttpGet("{userId}/stats")]
        public async Task<ActionResult<UserStatsResponse>> GetUserStats(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Assessments)
                    .Include(u => u.PersonalityProfile)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Calculate stats
                var assessmentCount = user.Assessments.Where(a => a.Status == "Completed").Count();
                var daysActive = (DateTime.UtcNow - user.CreatedDate).Days;
                
                // Calculate potential matches (users with profiles, excluding self)
                var matchCount = 0;
                if (user.PersonalityProfile != null)
                {
                    matchCount = await _context.PersonalityProfiles
                        .Where(p => p.UserId != userId)
                        .CountAsync();
                }

                var stats = new UserStatsResponse
                {
                    AssessmentCount = assessmentCount,
                    MatchCount = matchCount,
                    DaysActive = Math.Max(1, daysActive)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get user's assessment history
        /// </summary>
        [HttpGet("{userId}/assessments")]
        public async Task<ActionResult<List<AssessmentHistoryResponse>>> GetUserAssessments(int userId)
        {
            try
            {
                var assessments = await _context.Assessments
                    .Where(a => a.UserId == userId && a.Status == "Completed")
                    .OrderByDescending(a => a.CompletedDate)
                    .ToListAsync();

                var assessmentHistory = new List<AssessmentHistoryResponse>();

                foreach (var assessment in assessments)
                {
                    // Get the personality profile for this assessment
                    var profile = await _context.PersonalityProfiles
                        .FirstOrDefaultAsync(p => p.UserId == userId);

                    if (profile != null)
                    {
                        var history = new AssessmentHistoryResponse
                        {
                            AssessmentId = assessment.AssessmentId,
                            CompletedAt = assessment.CompletedDate ?? assessment.StartedDate,
                            MbtiType = profile.MbtiType,
                            Confidence = profile.Confidence
                        };
                        assessmentHistory.Add(history);
                    }
                }

                return Ok(assessmentHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assessments for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update user profile information
        /// </summary>
        [HttpPut("{userId}")]
        public async Task<ActionResult> UpdateUser(int userId, [FromBody] UpdateUserProfileRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Update fields
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    user.Name = request.Name;
                }

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    // Check if email is already taken by another user
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email && u.UserId != userId);
                    
                    if (existingUser != null)
                    {
                        return BadRequest("Email is already taken");
                    }

                    user.Email = request.Email;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }
    }

    // Response models
    public class UserProfileResponse
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MbtiType { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<TraitScore> TraitScores { get; set; } = new();
    }

    public class UserStatsResponse
    {
        public int AssessmentCount { get; set; }
        public int MatchCount { get; set; }
        public int DaysActive { get; set; }
    }

    public class AssessmentHistoryResponse
    {
        public int AssessmentId { get; set; }
        public DateTime CompletedAt { get; set; }
        public string MbtiType { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }

    public class UpdateUserProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool ShareProfile { get; set; }
    }
}
