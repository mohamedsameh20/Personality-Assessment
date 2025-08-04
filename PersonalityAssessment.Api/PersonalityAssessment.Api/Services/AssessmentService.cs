using PersonalityAssessment.Api.Models;
using PersonalityAssessment.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace PersonalityAssessment.Api.Services
{
    public interface IAssessmentService
    {
        List<Question> GetQuestions();
        Task<AssessmentResult> CalculateResultsAsync(AssessmentRequest request, int? userId = null);
        Task<int> StartAssessmentAsync(int userId);
        Task<AssessmentResult?> GetStoredResultsAsync(int userId);
    }

    public class AssessmentService : IAssessmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IPersonalityScorer _personalityScorer;

        // Simple hardcoded questions for testing
        private readonly List<Question> _questions = new()
        {
            // Extraversion questions
            new Question { Id = 1, Text = "I am the life of the party", Trait = PersonalityTrait.Extraversion, IsReversed = false },
            new Question { Id = 2, Text = "I don't talk a lot", Trait = PersonalityTrait.Extraversion, IsReversed = true },
            
            // Agreeableness questions
            new Question { Id = 3, Text = "I feel others' emotions", Trait = PersonalityTrait.Agreeableness, IsReversed = false },
            new Question { Id = 4, Text = "I am not interested in other people's problems", Trait = PersonalityTrait.Agreeableness, IsReversed = true },
            
            // Conscientiousness questions
            new Question { Id = 5, Text = "I am always prepared", Trait = PersonalityTrait.Conscientiousness, IsReversed = false },
            new Question { Id = 6, Text = "I leave my belongings around", Trait = PersonalityTrait.Conscientiousness, IsReversed = true },
            
            // Emotional Stability questions (now Emotionality - low emotionality = high emotional stability)
            new Question { Id = 7, Text = "I get stressed out easily", Trait = PersonalityTrait.Emotionality, IsReversed = false },
            new Question { Id = 8, Text = "I am relaxed most of the time", Trait = PersonalityTrait.Emotionality, IsReversed = true },
            
            // Openness questions
            new Question { Id = 9, Text = "I have a rich vocabulary", Trait = PersonalityTrait.Openness, IsReversed = false },
            new Question { Id = 10, Text = "I have difficulty understanding abstract ideas", Trait = PersonalityTrait.Openness, IsReversed = true }
        };

        public AssessmentService(ApplicationDbContext context, IUserService userService, IPersonalityScorer personalityScorer)
        {
            _context = context;
            _userService = userService;
            _personalityScorer = personalityScorer;
        }

        public List<Question> GetQuestions()
        {
            return _questions;
        }

        public async Task<int> StartAssessmentAsync(int userId)
        {
            var assessment = new Assessment
            {
                UserId = userId,
                StartedDate = DateTime.UtcNow,
                Status = "InProgress"
            };

            _context.Assessments.Add(assessment);
            await _context.SaveChangesAsync();

            return assessment.AssessmentId;
        }

        public async Task<AssessmentResult> CalculateResultsAsync(AssessmentRequest request, int? userId = null)
        {
            try
            {
                // Create anonymous user if none provided
                if (userId == null)
                {
                    var anonymousUser = await _userService.CreateAnonymousUserAsync();
                    userId = anonymousUser.UserId;
                }
                else
                {
                    // Verify the user exists
                    var existingUser = await _context.Users.FindAsync(userId.Value);
                    if (existingUser == null)
                    {
                        throw new ArgumentException($"User with ID {userId.Value} not found");
                    }
                }

                // Start assessment
                var assessmentId = await StartAssessmentAsync(userId.Value);

                // Store user responses
                foreach (var answer in request.Answers)
                {
                    var response = new UserResponse
                    {
                        AssessmentId = assessmentId,
                        QuestionId = answer.QuestionId,
                        AnswerValue = answer.Value,
                        ResponseTime = DateTime.UtcNow
                    };
                    _context.UserResponses.Add(response);
                }

                // Calculate personality results using the new scoring system
                var scoringResult = await _personalityScorer.CalculateScoresAsync(request.Answers);
                
                // Convert to AssessmentResult format for compatibility
                var result = new AssessmentResult
                {
                    TraitScores = scoringResult.TraitScores,
                    Summary = scoringResult.Summary,
                    CompletedAt = scoringResult.CompletedAt,
                    MbtiType = scoringResult.MbtiType,
                    Confidence = scoringResult.Confidence
                };

                // Mark assessment as completed
                var assessment = await _context.Assessments.FindAsync(assessmentId);
                if (assessment != null)
                {
                    assessment.CompletedDate = DateTime.UtcNow;
                    assessment.Status = "Completed";
                }

                // Store personality profile
                await StorePersonalityProfileAsync(userId.Value, result);

                await _context.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                // Log the error and re-throw it so the controller can handle it
                throw new InvalidOperationException($"Error calculating assessment results for user {userId}: {ex.Message}", ex);
            }
        }

        public async Task<AssessmentResult?> GetStoredResultsAsync(int userId)
        {
            var user = await _userService.GetUserAsync(userId);
            if (user?.PersonalityProfile == null)
                return null;

            var profileData = JsonSerializer.Deserialize<AssessmentResult>(user.PersonalityProfile.ProfileData);
            return profileData;
        }

        private AssessmentResult CalculatePersonalityScores(AssessmentRequest request)
        {
            var result = new AssessmentResult
            {
                CompletedAt = DateTime.UtcNow
            };

            // Calculate scores for each trait
            foreach (PersonalityTrait trait in Enum.GetValues<PersonalityTrait>())
            {
                var traitQuestions = _questions.Where(q => q.Trait == trait).ToList();
                var traitAnswers = request.Answers.Where(a => traitQuestions.Any(q => q.Id == a.QuestionId)).ToList();

                if (traitAnswers.Count > 0)
                {
                    double totalScore = 0;
                    int questionCount = 0;

                    foreach (var answer in traitAnswers)
                    {
                        var question = traitQuestions.First(q => q.Id == answer.QuestionId);
                        var score = question.IsReversed ? (6 - answer.Value) : answer.Value;
                        totalScore += score;
                        questionCount++;
                    }

                    // Convert to 0-100 scale
                    var averageScore = totalScore / questionCount;
                    var percentageScore = ((averageScore - 1) / 4) * 100; // Convert from 1-5 scale to 0-100

                    result.TraitScores.Add(new TraitScore
                    {
                        Trait = trait,
                        TraitName = trait.ToString(),
                        Score = Math.Round(percentageScore, 1),
                        Description = GetTraitDescription(trait, percentageScore)
                    });
                }
            }

            result.Summary = GenerateSummary(result.TraitScores);
            return result;
        }

        private async Task StorePersonalityProfileAsync(int userId, AssessmentResult result)
        {
            var existingProfile = await _context.PersonalityProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            var profileJson = JsonSerializer.Serialize(result);
            
            // Extract normalized trait scores (0-1 scale) for matching algorithm
            var traitScores = result.TraitScores.Select(ts => ts.Score / 100.0).ToArray(); // Convert from 0-100 to 0-1
            var traitScoresJson = JsonSerializer.Serialize(traitScores);

            if (existingProfile != null)
            {
                // Update existing profile
                existingProfile.ProfileData = profileJson;
                existingProfile.MbtiType = result.MbtiType;
                existingProfile.TraitScoresJson = traitScoresJson;
                existingProfile.Confidence = result.Confidence;
                existingProfile.UpdatedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new profile - use the Data namespace PersonalityProfile
                var newProfile = new Data.PersonalityProfile
                {
                    UserId = userId,
                    ProfileData = profileJson,
                    MbtiType = result.MbtiType,
                    TraitScoresJson = traitScoresJson,
                    Confidence = result.Confidence,
                    CreatedDate = DateTime.UtcNow
                };
                _context.PersonalityProfiles.Add(newProfile);
            }
        }

        private string GetTraitDescription(PersonalityTrait trait, double score)
        {
            var level = score switch
            {
                < 30 => "Low",
                < 70 => "Moderate",
                _ => "High"
            };

            return trait switch
            {
                PersonalityTrait.Extraversion => $"{level} extraversion - {(score > 50 ? "Outgoing and energetic" : "Reserved and reflective")}",
                PersonalityTrait.Agreeableness => $"{level} agreeableness - {(score > 50 ? "Cooperative and trusting" : "Competitive and skeptical")}",
                PersonalityTrait.Conscientiousness => $"{level} conscientiousness - {(score > 50 ? "Organized and disciplined" : "Flexible and spontaneous")}",
                PersonalityTrait.Emotionality => $"{level} emotionality - {(score > 50 ? "Sensitive and reactive" : "Calm and resilient")}",
                PersonalityTrait.Openness => $"{level} openness - {(score > 50 ? "Creative and curious" : "Practical and conventional")}",
                PersonalityTrait.HonestyHumility => $"{level} honesty-humility - {(score > 50 ? "Modest and sincere" : "Bold and entitled")}",
                PersonalityTrait.Dominance => $"{level} dominance - {(score > 50 ? "Assertive and commanding" : "Cooperative and submissive")}",
                PersonalityTrait.Vigilance => $"{level} vigilance - {(score > 50 ? "Cautious and alert" : "Trusting and accepting")}",
                PersonalityTrait.SelfTranscendence => $"{level} self-transcendence - {(score > 50 ? "Spiritual and idealistic" : "Practical and materialistic")}",
                PersonalityTrait.AbstractOrientation => $"{level} abstract orientation - {(score > 50 ? "Theoretical and conceptual" : "Concrete and practical")}",
                PersonalityTrait.ValueOrientation => $"{level} value orientation - {(score > 50 ? "Values-driven and principled" : "Pragmatic and adaptable")}",
                PersonalityTrait.Flexibility => $"{level} flexibility - {(score > 50 ? "Adaptable and spontaneous" : "Structured and routine-oriented")}",
                _ => "Unknown trait"
            };
        }

        private string GenerateSummary(List<TraitScore> scores)
        {
            var highestTrait = scores.OrderByDescending(s => s.Score).First();
            var lowestTrait = scores.OrderBy(s => s.Score).First();

            return $"Your personality profile shows highest scores in {highestTrait.TraitName} ({highestTrait.Score}%) " +
                   $"and lowest in {lowestTrait.TraitName} ({lowestTrait.Score}%). " +
                   $"This suggests a balanced personality with particular strengths in {highestTrait.TraitName.ToLower()}.";
        }
    }
}
