using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalityAssessment.Api.Data;
using PersonalityAssessment.Api.Models;

namespace PersonalityAssessment.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all users in the database
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<List<User>>> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.PersonalityProfile)
                    .Include(u => u.Assessments)
                    .ToListAsync();
                
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create a user with a specific name and email
        /// </summary>
        [HttpPost("users")]
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update a user with new information
        /// </summary>
        [HttpPut("users/{userId}")]
        public async Task<ActionResult<User>> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound($"User with ID {userId} not found");
                }

                // Update user properties
                user.Name = request.Name;
                user.Email = request.Email;
                
                // Update password if provided
                if (!string.IsNullOrEmpty(request.Password))
                {
                    // Note: In a real application, you would hash the password
                    user.PasswordHash = request.Password; // For simplicity, storing as plain text
                }

                // Update status if provided
                if (!string.IsNullOrEmpty(request.Status))
                {
                    user.IsActive = request.Status == "active";
                }

                // Update notes if property exists (you may need to add this to User model)
                // user.Notes = request.Notes;

                await _context.SaveChangesAsync();

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all assessments with their responses
        /// </summary>
        [HttpGet("assessments")]
        public async Task<ActionResult<List<Assessment>>> GetAllAssessments()
        {
            try
            {
                var assessments = await _context.Assessments
                    .Include(a => a.User)
                    .Include(a => a.UserResponses)
                    .ToListAsync();
                
                return Ok(assessments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assessments");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all personality profiles
        /// </summary>
        [HttpGet("profiles")]
        public async Task<ActionResult<List<PersonalityProfile>>> GetAllProfiles()
        {
            try
            {
                var profiles = await _context.PersonalityProfiles
                    .Include(p => p.User)
                    .ToListAsync();
                
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profiles");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get database statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<DatabaseStats>> GetDatabaseStats()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalAssessments = await _context.Assessments.CountAsync();
                
                var stats = new DatabaseStats
                {
                    TotalUsers = totalUsers,
                    TotalAssessments = totalAssessments,
                    CompletedAssessments = await _context.Assessments.CountAsync(a => a.CompletedDate != null),
                    TotalResponses = await _context.UserResponses.CountAsync(),
                    TotalProfiles = await _context.PersonalityProfiles.CountAsync(),
                    AvgAssessmentsPerUser = totalUsers > 0 ? (double)totalAssessments / totalUsers : 0.0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete a user and all related data
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<ActionResult> DeleteUser(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound($"User with ID {userId} not found");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok($"User {userId} deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Clear all data (use with caution!)
        /// </summary>
        [HttpDelete("clear-all")]
        public async Task<ActionResult> ClearAllData()
        {
            try
            {
                // Remove all data in correct order due to foreign key constraints
                _context.UserResponses.RemoveRange(_context.UserResponses);
                _context.PersonalityProfiles.RemoveRange(_context.PersonalityProfiles);
                _context.Assessments.RemoveRange(_context.Assessments);
                _context.Users.RemoveRange(_context.Users);

                await _context.SaveChangesAsync();

                return Ok("All data cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all questions with their choices
        /// </summary>
        [HttpGet("questions")]
        public async Task<ActionResult> GetAllQuestions()
        {
            try
            {
                var questions = await _context.Questions
                    .Include(q => q.Choices.OrderBy(c => c.SortOrder))
                    .OrderBy(q => q.SortOrder)
                    .ToListAsync();
                
                return Ok(questions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Clear all questions and choices
        /// </summary>
        [HttpDelete("questions")]
        public async Task<ActionResult> ClearAllQuestions()
        {
            try
            {
                // Clear all questions (choices will be cascade deleted)
                await _context.Questions.ExecuteDeleteAsync();
                return Ok(new { message = "All questions cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing questions");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get user's personality profile status
        /// </summary>
        [HttpGet("users/{userId}/profile-status")]
        public async Task<ActionResult> GetUserProfileStatus(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.PersonalityProfile)
                    .Include(u => u.Assessments)
                    .FirstOrDefaultAsync(u => u.UserId == userId);
                
                if (user == null)
                {
                    return NotFound("User not found");
                }
                
                var status = new
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    HasPersonalityProfile = user.PersonalityProfile != null,
                    HasTraitScores = !string.IsNullOrEmpty(user.PersonalityProfile?.TraitScoresJson),
                    AssessmentCount = user.Assessments.Count,
                    MbtiType = user.PersonalityProfile?.MbtiType,
                    ProfileCreated = user.PersonalityProfile?.CreatedDate
                };
                
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile status for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Add a new question with choices
        /// </summary>
        [HttpPost("questions")]
        public async Task<ActionResult> AddQuestion([FromBody] AddQuestionRequest request)
        {
            try
            {
                var question = new QuestionEntity
                {
                    QuestionText = request.QuestionText,
                    PersonalityTrait = request.PersonalityTrait,
                    IsReversed = request.IsReversed,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    SortOrder = request.SortOrder
                };

                // Add choices
                for (int i = 0; i < request.Choices.Count; i++)
                {
                    var choice = request.Choices[i];
                    question.Choices.Add(new ChoiceEntity
                    {
                        ChoiceText = choice.ChoiceText,
                        ChoiceValue = choice.ChoiceValue,
                        SortOrder = choice.SortOrder
                    });
                }

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                var result = await _context.Questions
                    .Include(q => q.Choices.OrderBy(c => c.SortOrder))
                    .FirstOrDefaultAsync(q => q.QuestionId == question.QuestionId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding question");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update an existing question
        /// </summary>
        [HttpPut("questions/{questionId}")]
        public async Task<ActionResult> UpdateQuestion(int questionId, [FromBody] UpdateQuestionRequest request)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.Choices)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                {
                    return NotFound($"Question with ID {questionId} not found");
                }

                // Update question properties
                question.QuestionText = request.QuestionText;
                question.PersonalityTrait = request.PersonalityTrait;
                question.IsReversed = request.IsReversed;
                question.IsActive = request.IsActive;
                question.SortOrder = request.SortOrder;

                // Update choices
                _context.Choices.RemoveRange(question.Choices);
                
                for (int i = 0; i < request.Choices.Count; i++)
                {
                    var choice = request.Choices[i];
                    question.Choices.Add(new ChoiceEntity
                    {
                        QuestionId = questionId,
                        ChoiceText = choice.ChoiceText,
                        ChoiceValue = choice.ChoiceValue,
                        SortOrder = choice.SortOrder
                    });
                }

                await _context.SaveChangesAsync();

                var result = await _context.Questions
                    .Include(q => q.Choices.OrderBy(c => c.SortOrder))
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question {QuestionId}", questionId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete a question and its choices
        /// </summary>
        [HttpDelete("questions/{questionId}")]
        public async Task<ActionResult> DeleteQuestion(int questionId)
        {
            try
            {
                var question = await _context.Questions.FindAsync(questionId);
                if (question == null)
                {
                    return NotFound($"Question with ID {questionId} not found");
                }

                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();

                return Ok($"Question {questionId} deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get a specific question with its choices
        /// </summary>
        [HttpGet("questions/{questionId}")]
        public async Task<ActionResult> GetQuestion(int questionId)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.Choices.OrderBy(c => c.SortOrder))
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                {
                    return NotFound($"Question with ID {questionId} not found");
                }

                return Ok(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question {QuestionId}", questionId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Reorder questions
        /// </summary>
        [HttpPut("questions/reorder")]
        public async Task<ActionResult> ReorderQuestions([FromBody] ReorderQuestionsRequest request)
        {
            try
            {
                foreach (var item in request.Questions)
                {
                    var question = await _context.Questions.FindAsync(item.QuestionId);
                    if (question != null)
                    {
                        question.SortOrder = item.SortOrder;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok("Questions reordered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering questions");
                return StatusCode(500, "Internal server error");
            }
        }

    }

    // Request/Response models for admin endpoints
    public class CreateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }



    public class DatabaseStats
    {
        public int TotalUsers { get; set; }
        public int TotalAssessments { get; set; }
        public int CompletedAssessments { get; set; }
        public int TotalResponses { get; set; }
        public int TotalProfiles { get; set; }
        public double AvgAssessmentsPerUser { get; set; }
    }

    public class AddQuestionRequest
    {
        public string QuestionText { get; set; } = string.Empty;
        public string PersonalityTrait { get; set; } = string.Empty;
        public bool IsReversed { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public List<AddChoiceRequest> Choices { get; set; } = new List<AddChoiceRequest>();
    }

    public class AddChoiceRequest
    {
        public string ChoiceText { get; set; } = string.Empty;
        public int ChoiceValue { get; set; }
        public int SortOrder { get; set; }
    }

    public class UpdateQuestionRequest
    {
        public string QuestionText { get; set; } = string.Empty;
        public string PersonalityTrait { get; set; } = string.Empty;
        public bool IsReversed { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public List<AddChoiceRequest> Choices { get; set; } = new List<AddChoiceRequest>();
    }

    public class ReorderQuestionsRequest
    {
        public List<QuestionOrderItem> Questions { get; set; } = new List<QuestionOrderItem>();
    }

    public class QuestionOrderItem
    {
        public int QuestionId { get; set; }
        public int SortOrder { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        // public string Notes { get; set; } = string.Empty; // Uncomment if Notes field is added to User model
    }
}
