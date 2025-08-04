using PersonalityAssessment.Api.Data;
using PersonalityAssessment.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace PersonalityAssessment.Api.Services
{
    public interface ICompatibilityService
    {
        Task<List<CompatibilityMatch>> FindCompatibleMatches(int userId, int limit = 10);
        double CalculateCompatibilityScore(double[] p1Traits, double[] p2Traits);
        Task<CompatibilityAnalysis> GetDetailedCompatibilityAnalysis(int userId1, int userId2);
    }

    public class CompatibilityService : ICompatibilityService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompatibilityService> _logger;

        public CompatibilityService(ApplicationDbContext context, ILogger<CompatibilityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CompatibilityMatch>> FindCompatibleMatches(int userId, int limit = 10)
        {
            try
            {
                // Get the user's personality profile
                var userProfile = await _context.PersonalityProfiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (userProfile == null || string.IsNullOrEmpty(userProfile.TraitScoresJson))
                {
                    return new List<CompatibilityMatch>();
                }

                var userTraits = JsonSerializer.Deserialize<double[]>(userProfile.TraitScoresJson);
                if (userTraits == null || userTraits.Length == 0)
                {
                    return new List<CompatibilityMatch>();
                }
                
                // Get all other users with personality profiles
                var otherProfiles = await _context.PersonalityProfiles
                    .Include(p => p.User)
                    .Where(p => p.UserId != userId && !string.IsNullOrEmpty(p.TraitScoresJson))
                    .ToListAsync();

                var matches = new List<CompatibilityMatch>();

                foreach (var profile in otherProfiles)
                {
                    try
                    {
                        var otherTraits = JsonSerializer.Deserialize<double[]>(profile.TraitScoresJson);
                        if (otherTraits == null || otherTraits.Length != userTraits.Length) continue;
                        
                        var score = CalculateCompatibilityScore(userTraits, otherTraits);

                        matches.Add(new CompatibilityMatch
                        {
                            UserId = profile.UserId,
                            UserName = profile.User.Name,
                            UserEmail = profile.User.Email,
                            CompatibilityScore = score,
                            MbtiType = profile.MbtiType,
                            MatchedDate = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error calculating compatibility for user {UserId}", profile.UserId);
                    }
                }

                // Sort by compatibility score and return top matches
                return matches
                    .OrderByDescending(m => m.CompatibilityScore)
                    .Take(limit)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding compatible matches for user {UserId}", userId);
                return new List<CompatibilityMatch>();
            }
        }

        public double CalculateCompatibilityScore(double[] p1Traits, double[] p2Traits)
        {
            try
            {
                if (p1Traits.Length != p2Traits.Length)
                    return 0;

                double totalScore = 0;
                int traitCount = p1Traits.Length;

                for (int i = 0; i < traitCount; i++)
                {
                    double trait1 = p1Traits[i];
                    double trait2 = p2Traits[i];
                    
                    // For most traits, we want similarity (people with similar values)
                    double similarity = 1 - Math.Abs(trait1 - trait2);
                    
                    // Apply different weights for different traits
                    double weight = 1.0; // Default weight
                    
                    // Some traits might benefit from complementarity rather than similarity
                    // This is a simplified approach - in reality, compatibility is much more complex
                    double traitScore = similarity * weight;
                    
                    totalScore += traitScore;
                }

                // Convert to percentage (0-100)
                return (totalScore / traitCount) * 100;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating compatibility score");
                return 0;
            }
        }

        public async Task<CompatibilityAnalysis> GetDetailedCompatibilityAnalysis(int userId1, int userId2)
        {
            try
            {
                var profile1 = await _context.PersonalityProfiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId1);

                var profile2 = await _context.PersonalityProfiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId2);

                if (profile1 == null || profile2 == null ||
                    string.IsNullOrEmpty(profile1.TraitScoresJson) ||
                    string.IsNullOrEmpty(profile2.TraitScoresJson))
                {
                    return null!;
                }

                var traits1 = JsonSerializer.Deserialize<double[]>(profile1.TraitScoresJson);
                var traits2 = JsonSerializer.Deserialize<double[]>(profile2.TraitScoresJson);

                if (traits1 == null || traits2 == null || traits1.Length != traits2.Length)
                {
                    return null!;
                }

                var analysis = new CompatibilityAnalysis
                {
                    User1Name = profile1.User.Name,
                    User2Name = profile2.User.Name,
                    OverallScore = CalculateCompatibilityScore(traits1, traits2),
                    TraitComparisons = new List<TraitCompatibility>(),
                    Strengths = new List<string>(),
                    PotentialChallenges = new List<string>(),
                    Recommendations = new List<string>()
                };

                // Calculate trait-by-trait compatibility using trait names
                var traitNames = new[] { "HonestyHumility", "Emotionality", "Extraversion", "Agreeableness", 
                                       "Conscientiousness", "Openness", "Dominance", "Vigilance", 
                                       "SelfTranscendence", "AbstractOrientation", "ValueOrientation", "Flexibility" };

                for (int i = 0; i < Math.Min(traits1.Length, traitNames.Length); i++)
                {
                    var similarity = 1 - Math.Abs(traits1[i] - traits2[i]);
                    var difference = Math.Abs(traits1[i] - traits2[i]);

                    analysis.TraitComparisons.Add(new TraitCompatibility
                    {
                        TraitName = traitNames[i],
                        User1Score = traits1[i],
                        User2Score = traits2[i],
                        Similarity = similarity,
                        Difference = difference,
                        IsComplementary = traitNames[i] == "Dominance" || traitNames[i] == "Vigilance"
                    });
                }

                // Generate insights
                GenerateCompatibilityInsights(analysis, traits1, traits2);

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating compatibility analysis for users {UserId1} and {UserId2}", userId1, userId2);
                return null!;
            }
        }        private void GenerateCompatibilityInsights(CompatibilityAnalysis analysis, double[] traits1, double[] traits2)
        {
            // Add some basic insights based on trait similarities
            var similarities = analysis.TraitComparisons.OrderByDescending(tc => tc.Similarity).ToList();
            
            // Strengths
            var topSimilarities = similarities.Take(3).ToList();
            foreach (var trait in topSimilarities)
            {
                analysis.Strengths.Add($"High compatibility in {trait.TraitName} ({(trait.Similarity * 100):F0}% similar)");
            }
            
            // Challenges
            var topDifferences = similarities.OrderBy(tc => tc.Similarity).Take(2).ToList();
            foreach (var trait in topDifferences)
            {
                analysis.PotentialChallenges.Add($"Different approaches to {trait.TraitName} may require understanding");
            }
            
            // Recommendations
            analysis.Recommendations.Add("Focus on your shared values and similar traits");
            analysis.Recommendations.Add("Communicate openly about your differences");
            analysis.Recommendations.Add("Appreciate each other's unique perspectives");
        }
    }

    // Model classes for compatibility
    public class CompatibilityMatch
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public double CompatibilityScore { get; set; }
        public string MbtiType { get; set; } = string.Empty;
        public DateTime MatchedDate { get; set; }
    }

    public class CompatibilityAnalysis
    {
        public string User1Name { get; set; } = string.Empty;
        public string User2Name { get; set; } = string.Empty;
        public double OverallScore { get; set; }
        public List<TraitCompatibility> TraitComparisons { get; set; } = new List<TraitCompatibility>();
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> PotentialChallenges { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    public class TraitCompatibility
    {
        public string TraitName { get; set; } = string.Empty;
        public double User1Score { get; set; }
        public double User2Score { get; set; }
        public double Similarity { get; set; }
        public double Difference { get; set; }
        public bool IsComplementary { get; set; }
    }
}
