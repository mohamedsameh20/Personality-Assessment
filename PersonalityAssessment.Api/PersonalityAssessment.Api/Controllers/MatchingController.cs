using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalityAssessment.Api.Data;
using System.Text.Json;

namespace PersonalityAssessment.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MatchingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("users/{userId}/matches")]
        public async Task<ActionResult<List<UserMatchResult>>> GetMatches(int userId)
        {
            // Get the target user's personality profile
            var targetProfile = await _context.PersonalityProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (targetProfile == null || string.IsNullOrEmpty(targetProfile.TraitScoresJson))
            {
                return BadRequest("User has not completed personality assessment");
            }

            // Parse target user's trait scores
            var targetScores = JsonSerializer.Deserialize<double[]>(targetProfile.TraitScoresJson);
            if (targetScores == null || targetScores.Length == 0)
            {
                return BadRequest("Invalid trait scores data");
            }

            // Get all other users with completed assessments
            var allProfiles = await _context.PersonalityProfiles
                .Include(p => p.User)
                .Where(p => p.UserId != userId && !string.IsNullOrEmpty(p.TraitScoresJson))
                .ToListAsync();

            var matches = new List<UserMatchResult>();

            foreach (var profile in allProfiles)
            {
                try
                {
                    var otherScores = JsonSerializer.Deserialize<double[]>(profile.TraitScoresJson);
                    if (otherScores == null || otherScores.Length != targetScores.Length)
                        continue;

                    // Calculate Euclidean distance
                    var distance = CalculateEuclideanDistance(targetScores, otherScores);
                    
                    // Convert to similarity percentage
                    var maxDistance = Math.Sqrt(targetScores.Length); // Max possible distance
                    var normalizedDistance = distance / maxDistance;
                    var similarityPercentage = (1 - normalizedDistance) * 100;

                    matches.Add(new UserMatchResult
                    {
                        UserId = profile.UserId,
                        UserName = profile.User.Name,
                        MbtiType = profile.MbtiType,
                        SimilarityPercentage = Math.Round(similarityPercentage, 1),
                        Distance = Math.Round(distance, 4),
                        TraitScores = otherScores
                    });
                }
                catch (JsonException)
                {
                    // Skip profiles with invalid JSON data
                    continue;
                }
            }

            // Sort by similarity (highest first) and take top 20
            var topMatches = matches
                .OrderByDescending(m => m.SimilarityPercentage)
                .Take(20)
                .ToList();

            return Ok(topMatches);
        }

        [HttpGet("users/{userId}/compare/{otherUserId}")]
        public async Task<ActionResult<UserComparisonResult>> CompareUsers(int userId, int otherUserId)
        {
            // Get both users' profiles
            var userProfile = await _context.PersonalityProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
            
            var otherProfile = await _context.PersonalityProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == otherUserId);

            if (userProfile == null || otherProfile == null)
            {
                return BadRequest("One or both users have not completed personality assessment");
            }

            var userScores = JsonSerializer.Deserialize<double[]>(userProfile.TraitScoresJson);
            var otherScores = JsonSerializer.Deserialize<double[]>(otherProfile.TraitScoresJson);

            if (userScores == null || otherScores == null || userScores.Length != otherScores.Length)
            {
                return BadRequest("Invalid trait scores data");
            }

            // Calculate trait differences
            var traitNames = new[] { 
                "Honesty-Humility", "Emotionality", "Extraversion", "Agreeableness", 
                "Conscientiousness", "Openness", "Dominance", "Vigilance", 
                "Self-Transcendence", "Abstract Orientation", "Value Orientation", "Flexibility" 
            };

            var traitComparisons = new List<TraitComparison>();
            for (int i = 0; i < userScores.Length && i < traitNames.Length; i++)
            {
                var difference = Math.Abs(userScores[i] - otherScores[i]);
                traitComparisons.Add(new TraitComparison
                {
                    TraitName = traitNames[i],
                    UserScore = Math.Round(userScores[i] * 100, 1), // Convert to 0-100 scale
                    OtherUserScore = Math.Round(otherScores[i] * 100, 1),
                    Difference = Math.Round(difference * 100, 1)
                });
            }

            // Find most similar and different traits
            var mostSimilar = traitComparisons.OrderBy(t => t.Difference).Take(3).ToList();
            var mostDifferent = traitComparisons.OrderByDescending(t => t.Difference).Take(3).ToList();

            return Ok(new UserComparisonResult
            {
                User1 = new UserInfo { UserId = userId, Name = userProfile.User.Name, MbtiType = userProfile.MbtiType },
                User2 = new UserInfo { UserId = otherUserId, Name = otherProfile.User.Name, MbtiType = otherProfile.MbtiType },
                AllTraitComparisons = traitComparisons,
                MostSimilarTraits = mostSimilar,
                MostDifferentTraits = mostDifferent,
                OverallSimilarity = Math.Round((1 - CalculateEuclideanDistance(userScores, otherScores) / Math.Sqrt(userScores.Length)) * 100, 1)
            });
        }

        private static double CalculateEuclideanDistance(double[] scores1, double[] scores2)
        {
            if (scores1.Length != scores2.Length)
                throw new ArgumentException("Score arrays must have the same length");

            double sumSquaredDifferences = 0;
            for (int i = 0; i < scores1.Length; i++)
            {
                double difference = scores1[i] - scores2[i];
                sumSquaredDifferences += difference * difference;
            }

            return Math.Sqrt(sumSquaredDifferences);
        }
    }

    // Response models
    public class UserMatchResult
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string MbtiType { get; set; } = string.Empty;
        public double SimilarityPercentage { get; set; }
        public double Distance { get; set; }
        public double[] TraitScores { get; set; } = Array.Empty<double>();
    }

    public class UserComparisonResult
    {
        public UserInfo User1 { get; set; } = new();
        public UserInfo User2 { get; set; } = new();
        public List<TraitComparison> AllTraitComparisons { get; set; } = new();
        public List<TraitComparison> MostSimilarTraits { get; set; } = new();
        public List<TraitComparison> MostDifferentTraits { get; set; } = new();
        public double OverallSimilarity { get; set; }
    }

    public class UserInfo
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MbtiType { get; set; } = string.Empty;
    }

    public class TraitComparison
    {
        public string TraitName { get; set; } = string.Empty;
        public double UserScore { get; set; }
        public double OtherUserScore { get; set; }
        public double Difference { get; set; }
    }
}
