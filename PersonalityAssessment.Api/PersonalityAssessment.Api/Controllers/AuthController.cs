using Microsoft.AspNetCore.Mvc;
using PersonalityAssessment.Api.Models;
using PersonalityAssessment.Api.Services;

namespace PersonalityAssessment.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new user account
        /// </summary>
        [HttpPost("signup")]
        public async Task<ActionResult<AuthResponse>> Signup([FromBody] SignupRequest request)
        {
            var response = await _authService.SignupAsync(request);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return BadRequest(response);
        }

        /// <summary>
        /// Sign in with email and password
        /// </summary>
        [HttpPost("signin")]
        public async Task<ActionResult<AuthResponse>> Signin([FromBody] SigninRequest request)
        {
            var response = await _authService.SigninAsync(request);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return Unauthorized(response);
        }

        /// <summary>
        /// Check if an email is already registered
        /// </summary>
        [HttpGet("check-email")]
        public async Task<ActionResult<bool>> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required");
            }

            var exists = await _authService.EmailExistsAsync(email);
            return Ok(new { exists = exists });
        }

        /// <summary>
        /// Get user information by ID
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<UserInfo>> GetUser(int userId)
        {
            var user = await _authService.GetUserInfoAsync(userId);
            
            if (user == null)
            {
                return NotFound("User not found");
            }
            
            return Ok(user);
        }

        /// <summary>
        /// Update user's last login time
        /// </summary>
        [HttpPost("update-login/{userId}")]
        public async Task<ActionResult> UpdateLastLogin(int userId)
        {
            var success = await _authService.UpdateLastLoginAsync(userId);
            
            if (success)
            {
                return Ok("Last login updated");
            }
            
            return NotFound("User not found");
        }
    }
}
