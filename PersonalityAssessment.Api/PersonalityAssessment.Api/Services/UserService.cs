using PersonalityAssessment.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace PersonalityAssessment.Api.Services
{
    public interface IUserService
    {
        Task<User> CreateAnonymousUserAsync();
        Task<User?> GetUserAsync(int userId);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> CreateAnonymousUserAsync()
        {
            var user = new User
            {
                Email = $"anonymous_{Guid.NewGuid():N}@personality.local",
                CreatedDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            return user;
        }

        public async Task<User?> GetUserAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.PersonalityProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }
    }
}
