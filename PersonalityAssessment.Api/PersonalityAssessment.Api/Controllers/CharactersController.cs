using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PersonalityAssessment.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CharactersController : ControllerBase
    {
        private readonly ILogger<CharactersController> _logger;
        private readonly IWebHostEnvironment _environment;

        public CharactersController(ILogger<CharactersController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Get all fictional characters available for analysis
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<FictionalCharacter>>> GetAllCharacters()
        {
            try
            {
                var charactersPath = Path.Combine(_environment.WebRootPath, "data", "characters.json");
                
                if (!System.IO.File.Exists(charactersPath))
                {
                    _logger.LogError("Characters data file not found at: {Path}", charactersPath);
                    return NotFound("Character data not available");
                }

                var jsonContent = await System.IO.File.ReadAllTextAsync(charactersPath);
                var characters = JsonSerializer.Deserialize<List<FictionalCharacter>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Ok(characters ?? new List<FictionalCharacter>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading character data");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get a specific character by ID
        /// </summary>
        [HttpGet("{characterId}")]
        public async Task<ActionResult<FictionalCharacter>> GetCharacter(string characterId)
        {
            try
            {
                var charactersPath = Path.Combine(_environment.WebRootPath, "data", "characters.json");
                
                if (!System.IO.File.Exists(charactersPath))
                {
                    return NotFound("Character data not available");
                }

                var jsonContent = await System.IO.File.ReadAllTextAsync(charactersPath);
                var characters = JsonSerializer.Deserialize<List<FictionalCharacter>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var character = characters?.FirstOrDefault(c => c.Id.Equals(characterId, StringComparison.OrdinalIgnoreCase));
                
                if (character == null)
                {
                    return NotFound($"Character with ID '{characterId}' not found");
                }

                return Ok(character);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading character data for ID: {CharacterId}", characterId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Compare user's personality with a character's personality
        /// </summary>
        [HttpPost("{characterId}/compare")]
        public async Task<ActionResult<CharacterComparisonResult>> CompareWithCharacter(
            string characterId, 
            [FromBody] UserTraitScores userScores)
        {
            try
            {
                // Get character data
                var charactersPath = Path.Combine(_environment.WebRootPath, "data", "characters.json");
                
                if (!System.IO.File.Exists(charactersPath))
                {
                    return NotFound("Character data not available");
                }

                var jsonContent = await System.IO.File.ReadAllTextAsync(charactersPath);
                var characters = JsonSerializer.Deserialize<List<FictionalCharacter>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var character = characters?.FirstOrDefault(c => c.Id.Equals(characterId, StringComparison.OrdinalIgnoreCase));
                
                if (character == null)
                {
                    return NotFound($"Character with ID '{characterId}' not found");
                }

                // Calculate similarity using Euclidean distance
                var similarity = CalculateSimilarity(userScores, character.NormalizedScores);

                // Find key differences
                var differences = CalculateTraitDifferences(userScores, character.NormalizedScores);

                var result = new CharacterComparisonResult
                {
                    CharacterId = characterId,
                    CharacterName = character.Name,
                    SimilarityPercentage = similarity,
                    TraitDifferences = differences,
                    ComparisonSummary = GenerateComparisonSummary(character.Name, similarity, differences)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing user with character: {CharacterId}", characterId);
                return StatusCode(500, "Internal server error");
            }
        }

        private double CalculateSimilarity(UserTraitScores userScores, Dictionary<string, double> characterScores)
        {
            var userDict = ConvertUserScoresToDictionary(userScores);
            
            double sumSquaredDifferences = 0;
            int validTraits = 0;

            foreach (var trait in characterScores.Keys)
            {
                if (userDict.ContainsKey(trait))
                {
                    var difference = userDict[trait] - characterScores[trait];
                    sumSquaredDifferences += difference * difference;
                    validTraits++;
                }
            }

            if (validTraits == 0) return 0;

            var euclideanDistance = Math.Sqrt(sumSquaredDifferences / validTraits);
            var maxDistance = Math.Sqrt(2); // Maximum possible distance in normalized space
            var similarity = (1.0 - (euclideanDistance / maxDistance)) * 100.0;

            return Math.Max(0, Math.Min(100, similarity));
        }

        private Dictionary<string, double> ConvertUserScoresToDictionary(UserTraitScores userScores)
        {
            return new Dictionary<string, double>
            {
                ["Honesty-Humility"] = userScores.HonestyHumility,
                ["Emotionality"] = userScores.Emotionality,
                ["Extraversion"] = userScores.Extraversion,
                ["Agreeableness"] = userScores.Agreeableness,
                ["Conscientiousness"] = userScores.Conscientiousness,
                ["Openness"] = userScores.Openness,
                ["Dominance"] = userScores.Dominance,
                ["Vigilance"] = userScores.Vigilance,
                ["Self-Transcendence"] = userScores.SelfTranscendence,
                ["Abstract Orientation"] = userScores.AbstractOrientation,
                ["Value Orientation"] = userScores.ValueOrientation,
                ["Flexibility"] = userScores.Flexibility
            };
        }

        private List<TraitDifference> CalculateTraitDifferences(UserTraitScores userScores, Dictionary<string, double> characterScores)
        {
            var userDict = ConvertUserScoresToDictionary(userScores);
            var differences = new List<TraitDifference>();

            foreach (var trait in characterScores.Keys)
            {
                if (userDict.ContainsKey(trait))
                {
                    var diff = userDict[trait] - characterScores[trait];
                    differences.Add(new TraitDifference
                    {
                        TraitName = trait,
                        UserScore = userDict[trait],
                        CharacterScore = characterScores[trait],
                        Difference = diff,
                        AbsoluteDifference = Math.Abs(diff)
                    });
                }
            }

            return differences.OrderByDescending(d => d.AbsoluteDifference).ToList();
        }

        private string GenerateComparisonSummary(string characterName, double similarity, List<TraitDifference> differences)
        {
            var similarityLevel = similarity switch
            {
                >= 80 => "remarkably similar",
                >= 60 => "quite similar",
                >= 40 => "moderately similar",
                >= 20 => "somewhat different",
                _ => "very different"
            };

            var topDifference = differences.FirstOrDefault();
            var summary = $"You are {similarityLevel} to {characterName} (#{similarity:F1}% match).";

            if (topDifference != null)
            {
                var differenceType = topDifference.Difference > 0 ? "higher" : "lower";
                summary += $" Your biggest difference is in {topDifference.TraitName}, where you score {differenceType}.";
            }

            return summary;
        }
    }

    // Data models for character analysis
    public class FictionalCharacter
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Show { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Analysis { get; set; } = string.Empty;
        public Dictionary<string, double> NormalizedScores { get; set; } = new();
    }

    public class UserTraitScores
    {
        public double HonestyHumility { get; set; }
        public double Emotionality { get; set; }
        public double Extraversion { get; set; }
        public double Agreeableness { get; set; }
        public double Conscientiousness { get; set; }
        public double Openness { get; set; }
        public double Dominance { get; set; }
        public double Vigilance { get; set; }
        public double SelfTranscendence { get; set; }
        public double AbstractOrientation { get; set; }
        public double ValueOrientation { get; set; }
        public double Flexibility { get; set; }
    }

    public class CharacterComparisonResult
    {
        public string CharacterId { get; set; } = string.Empty;
        public string CharacterName { get; set; } = string.Empty;
        public double SimilarityPercentage { get; set; }
        public List<TraitDifference> TraitDifferences { get; set; } = new();
        public string ComparisonSummary { get; set; } = string.Empty;
    }

    public class TraitDifference
    {
        public string TraitName { get; set; } = string.Empty;
        public double UserScore { get; set; }
        public double CharacterScore { get; set; }
        public double Difference { get; set; }
        public double AbsoluteDifference { get; set; }
    }
}
