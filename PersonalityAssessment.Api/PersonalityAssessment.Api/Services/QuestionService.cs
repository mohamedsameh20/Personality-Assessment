using Microsoft.EntityFrameworkCore;
using PersonalityAssessment.Api.Data;

namespace PersonalityAssessment.Api.Services
{
    public interface IQuestionService
    {
        Task<List<QuestionWithChoicesDto>> GetAllQuestionsAsync();
        Task SeedSampleQuestionsAsync();
    }
    
    public class QuestionService : IQuestionService
    {
        private readonly ApplicationDbContext _context;
        
        public QuestionService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<QuestionWithChoicesDto>> GetAllQuestionsAsync()
        {
            var questions = await _context.Questions
                .Include(q => q.Choices.OrderBy(c => c.SortOrder))
                .Where(q => q.IsActive)
                .OrderBy(q => q.SortOrder)
                .ToListAsync();
                
            return questions.Select(q => new QuestionWithChoicesDto
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                PersonalityTrait = q.PersonalityTrait,
                IsReversed = q.IsReversed,
                SortOrder = q.SortOrder,
                Choices = q.Choices.Select(c => new ChoiceDto
                {
                    ChoiceId = c.ChoiceId,
                    ChoiceText = c.ChoiceText,
                    ChoiceValue = c.ChoiceValue,
                    SortOrder = c.SortOrder
                }).ToList()
            }).ToList();
        }
        
        public async Task SeedSampleQuestionsAsync()
        {
            // Check if questions already exist
            if (await _context.Questions.AnyAsync())
            {
                return; // Already seeded
            }
            
            var questions = new List<QuestionEntity>
            {
                // Extraversion Questions
                new QuestionEntity
                {
                    QuestionText = "You're at a party where you don't know many people. What do you do?",
                    PersonalityTrait = "Extraversion",
                    IsReversed = false,
                    SortOrder = 1,
                    Choices = new List<ChoiceEntity>
                    {
                        new ChoiceEntity { ChoiceText = "Leave early or stay quiet in a corner", ChoiceValue = 1, SortOrder = 1 },
                        new ChoiceEntity { ChoiceText = "Find one person to talk to quietly", ChoiceValue = 2, SortOrder = 2 },
                        new ChoiceEntity { ChoiceText = "Mingle moderately with a few people", ChoiceValue = 3, SortOrder = 3 },
                        new ChoiceEntity { ChoiceText = "Actively meet several new people", ChoiceValue = 4, SortOrder = 4 },
                        new ChoiceEntity { ChoiceText = "Become the life of the party", ChoiceValue = 5, SortOrder = 5 }
                    }
                },
                new QuestionEntity
                {
                    QuestionText = "Your ideal weekend activity would be:",
                    PersonalityTrait = "Extraversion",
                    IsReversed = false,
                    SortOrder = 2,
                    Choices = new List<ChoiceEntity>
                    {
                        new ChoiceEntity { ChoiceText = "Reading a book alone at home", ChoiceValue = 1, SortOrder = 1 },
                        new ChoiceEntity { ChoiceText = "Having a quiet dinner with close friends", ChoiceValue = 2, SortOrder = 2 },
                        new ChoiceEntity { ChoiceText = "Attending a small gathering", ChoiceValue = 3, SortOrder = 3 },
                        new ChoiceEntity { ChoiceText = "Going to a concert or event", ChoiceValue = 4, SortOrder = 4 },
                        new ChoiceEntity { ChoiceText = "Hosting a big party", ChoiceValue = 5, SortOrder = 5 }
                    }
                },
                
                // Agreeableness Questions
                new QuestionEntity
                {
                    QuestionText = "When someone disagrees with you strongly, your typical response is:",
                    PersonalityTrait = "Agreeableness",
                    IsReversed = false,
                    SortOrder = 3,
                    Choices = new List<ChoiceEntity>
                    {
                        new ChoiceEntity { ChoiceText = "Argue forcefully to prove your point", ChoiceValue = 1, SortOrder = 1 },
                        new ChoiceEntity { ChoiceText = "Stand firm but avoid conflict", ChoiceValue = 2, SortOrder = 2 },
                        new ChoiceEntity { ChoiceText = "Listen and consider their perspective", ChoiceValue = 3, SortOrder = 3 },
                        new ChoiceEntity { ChoiceText = "Try to find common ground", ChoiceValue = 4, SortOrder = 4 },
                        new ChoiceEntity { ChoiceText = "Immediately look for ways to compromise", ChoiceValue = 5, SortOrder = 5 }
                    }
                },
                new QuestionEntity
                {
                    QuestionText = "A colleague takes credit for your work in a meeting. You:",
                    PersonalityTrait = "Agreeableness",
                    IsReversed = false,
                    SortOrder = 4,
                    Choices = new List<ChoiceEntity>
                    {
                        new ChoiceEntity { ChoiceText = "Confront them aggressively in front of everyone", ChoiceValue = 1, SortOrder = 1 },
                        new ChoiceEntity { ChoiceText = "Feel angry but say nothing", ChoiceValue = 2, SortOrder = 2 },
                        new ChoiceEntity { ChoiceText = "Politely clarify your contribution", ChoiceValue = 3, SortOrder = 3 },
                        new ChoiceEntity { ChoiceText = "Speak with them privately after the meeting", ChoiceValue = 4, SortOrder = 4 },
                        new ChoiceEntity { ChoiceText = "Let it go to avoid any conflict", ChoiceValue = 5, SortOrder = 5 }
                    }
                },
                
                // Conscientiousness Questions
                new QuestionEntity
                {
                    QuestionText = "You have a major project due next week. Your approach is:",
                    PersonalityTrait = "Conscientiousness",
                    IsReversed = false,
                    SortOrder = 5,
                    Choices = new List<ChoiceEntity>
                    {
                        new ChoiceEntity { ChoiceText = "Wait until the last day to start", ChoiceValue = 1, SortOrder = 1 },
                        new ChoiceEntity { ChoiceText = "Start a few days before the deadline", ChoiceValue = 2, SortOrder = 2 },
                        new ChoiceEntity { ChoiceText = "Begin working on it steadily", ChoiceValue = 3, SortOrder = 3 },
                        new ChoiceEntity { ChoiceText = "Create a detailed plan and timeline", ChoiceValue = 4, SortOrder = 4 },
                        new ChoiceEntity { ChoiceText = "Start immediately with a comprehensive strategy", ChoiceValue = 5, SortOrder = 5 }
                    }
                },
                new QuestionEntity
                {
                    QuestionText = "Your workspace/room typically looks:",
                    PersonalityTrait = "Conscientiousness",
                    IsReversed = false,
                    SortOrder = 6,
                    Choices = new List<ChoiceEntity>
                    {
                        new ChoiceEntity { ChoiceText = "Very messy and disorganized", ChoiceValue = 1, SortOrder = 1 },
                        new ChoiceEntity { ChoiceText = "Somewhat cluttered but functional", ChoiceValue = 2, SortOrder = 2 },
                        new ChoiceEntity { ChoiceText = "Generally tidy with some items out of place", ChoiceValue = 3, SortOrder = 3 },
                        new ChoiceEntity { ChoiceText = "Well-organized with designated places", ChoiceValue = 4, SortOrder = 4 },
                        new ChoiceEntity { ChoiceText = "Perfectly organized and immaculate", ChoiceValue = 5, SortOrder = 5 }
                    }
                },
                
                // Emotional Stability Questions
                new QuestionEntity
                {
                    QuestionText = "When facing a stressful situation, you typically:",
                    PersonalityTrait = "EmotionalStability",
                    IsReversed = false,
                    SortOrder = 7,
                    Choices = new List<ChoiceEntity>
                    {
                        new ChoiceEntity { ChoiceText = "Panic and feel overwhelmed", ChoiceValue = 1, SortOrder = 1 },
                        new ChoiceEntity { ChoiceText = "Feel anxious but try to cope", ChoiceValue = 2, SortOrder = 2 },
                        new ChoiceEntity { ChoiceText = "Stay relatively calm under pressure", ChoiceValue = 3, SortOrder = 3 },
                        new ChoiceEntity { ChoiceText = "Maintain composure and think clearly", ChoiceValue = 4, SortOrder = 4 },
                        new ChoiceEntity { ChoiceText = "Remain completely calm and focused", ChoiceValue = 5, SortOrder = 5 }
                    }
                },
                new QuestionEntity
                {
                    QuestionText = "When you receive criticism, your immediate reaction is:",
                    PersonalityTrait = "EmotionalStability",
                    IsReversed = false,
                    SortOrder = 8,
                    Choices = new List<ChoiceEntity>
                    {
                        new ChoiceEntity { ChoiceText = "Feel hurt and defensive", ChoiceValue = 1, SortOrder = 1 },
                        new ChoiceEntity { ChoiceText = "Get upset but try to hide it", ChoiceValue = 2, SortOrder = 2 },
                        new ChoiceEntity { ChoiceText = "Feel uncomfortable but listen", ChoiceValue = 3, SortOrder = 3 },
                        new ChoiceEntity { ChoiceText = "Consider it objectively", ChoiceValue = 4, SortOrder = 4 },
                        new ChoiceEntity { ChoiceText = "Welcome it as an opportunity to improve", ChoiceValue = 5, SortOrder = 5 }
                    }
                },
                
                // Openness Questions
                new QuestionEntity
                {
                    QuestionText = "When planning a vacation, you prefer:",
                    PersonalityTrait = "Openness",
                    IsReversed = false,
                    SortOrder = 9,
                    Choices = new List<ChoiceEntity>
                    {
                        new ChoiceEntity { ChoiceText = "Visiting the same familiar place", ChoiceValue = 1, SortOrder = 1 },
                        new ChoiceEntity { ChoiceText = "Going somewhere similar to past trips", ChoiceValue = 2, SortOrder = 2 },
                        new ChoiceEntity { ChoiceText = "Mixing familiar and new experiences", ChoiceValue = 3, SortOrder = 3 },
                        new ChoiceEntity { ChoiceText = "Exploring new places with some planning", ChoiceValue = 4, SortOrder = 4 },
                        new ChoiceEntity { ChoiceText = "Adventure to completely unknown destinations", ChoiceValue = 5, SortOrder = 5 }
                    }
                },
                new QuestionEntity
                {
                    QuestionText = "Your approach to new ideas and concepts is:",
                    PersonalityTrait = "Openness",
                    IsReversed = false,
                    SortOrder = 10,
                    Choices = new List<ChoiceEntity>
                    {
                        new ChoiceEntity { ChoiceText = "Skeptical and resistant to change", ChoiceValue = 1, SortOrder = 1 },
                        new ChoiceEntity { ChoiceText = "Cautious and need convincing", ChoiceValue = 2, SortOrder = 2 },
                        new ChoiceEntity { ChoiceText = "Open but need time to consider", ChoiceValue = 3, SortOrder = 3 },
                        new ChoiceEntity { ChoiceText = "Interested and willing to explore", ChoiceValue = 4, SortOrder = 4 },
                        new ChoiceEntity { ChoiceText = "Enthusiastic and eager to try", ChoiceValue = 5, SortOrder = 5 }
                    }
                }
            };
            
            _context.Questions.AddRange(questions);
            await _context.SaveChangesAsync();
        }
    }
    
    // DTOs for API responses
    public class QuestionWithChoicesDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string PersonalityTrait { get; set; } = string.Empty;
        public bool IsReversed { get; set; }
        public int SortOrder { get; set; }
        public List<ChoiceDto> Choices { get; set; } = new List<ChoiceDto>();
    }
    
    public class ChoiceDto
    {
        public int ChoiceId { get; set; }
        public string ChoiceText { get; set; } = string.Empty;
        public int ChoiceValue { get; set; }
        public int SortOrder { get; set; }
    }
}
