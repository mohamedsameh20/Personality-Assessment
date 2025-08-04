using PersonalityAssessment.Api.Data;
using PersonalityAssessment.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace PersonalityAssessment.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> SignupAsync(SignupRequest request);
        Task<AuthResponse> SigninAsync(SigninRequest request);
        Task<bool> EmailExistsAsync(string email);
        Task<UserInfo?> GetUserInfoAsync(int userId);
        Task<bool> UpdateLastLoginAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AuthResponse> SignupAsync(SignupRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email) || 
                    string.IsNullOrWhiteSpace(request.Password) || 
                    string.IsNullOrWhiteSpace(request.Name))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Name, email, and password are required."
                    };
                }

                // Check if email already exists
                if (await EmailExistsAsync(request.Email))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "An account with this email already exists."
                    };
                }

                // Validate email format
                if (!IsValidEmail(request.Email))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Please enter a valid email address."
                    };
                }

                // Validate password strength
                if (request.Password.Length < 6)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Password must be at least 6 characters long."
                    };
                }

                // Hash password
                string passwordHash = HashPassword(request.Password);

                // Create user
                var user = new User
                {
                    Name = request.Name.Trim(),
                    Email = request.Email.Trim().ToLowerInvariant(),
                    PasswordHash = passwordHash,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    UserType = "Regular"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Return success response
                return new AuthResponse
                {
                    Success = true,
                    Message = "Account created successfully!",
                    User = new UserInfo
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        Email = user.Email,
                        CreatedDate = user.CreatedDate,
                        UserType = user.UserType,
                        IsActive = user.IsActive
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user signup");
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred while creating your account. Please try again."
                };
            }
        }

        public async Task<AuthResponse> SigninAsync(SigninRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Email and password are required."
                    };
                }

                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant());

                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    };
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Your account has been deactivated. Please contact support."
                    };
                }

                // Verify password
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    };
                }

                // Update last login
                await UpdateLastLoginAsync(user.UserId);

                // Return success response
                return new AuthResponse
                {
                    Success = true,
                    Message = "Signed in successfully!",
                    User = new UserInfo
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        Email = user.Email,
                        CreatedDate = user.CreatedDate,
                        LastLoginDate = DateTime.UtcNow,
                        UserType = user.UserType,
                        IsActive = user.IsActive
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user signin");
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred while signing in. Please try again."
                };
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email.Trim().ToLowerInvariant());
        }

        public async Task<UserInfo?> GetUserInfoAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            return new UserInfo
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                UserType = user.UserType,
                IsActive = user.IsActive
            };
        }

        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user {UserId}", userId);
                return false;
            }
        }

        // Helper methods
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var salt = "PersonalityAssessment2025"; // In production, use a random salt per user
            var saltedPassword = salt + password;
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string hashedPassword)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hashedPassword;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
