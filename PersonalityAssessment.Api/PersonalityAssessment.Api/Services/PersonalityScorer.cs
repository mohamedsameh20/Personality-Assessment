using PersonalityAssessment.Api.Models;
using System.Text.Json;

namespace PersonalityAssessment.Api.Services
{
    public interface IPersonalityScorer
    {
        Task<PersonalityScoringResult> CalculateScoresAsync(List<Answer> answers);
        PersonalityScoringResult ProcessAnswersWithCorrelations(List<Answer> answers);
    }

    public class PersonalityScorer : IPersonalityScorer
    {
        // Core weights for answer processing
        private const double W_DIRECT = 39.0;      // Direct weight for primary trait
        private const double W_CORRELATED = 0.085; // Weight for correlated traits
        
        // Correlation adjustment parameters
        private const double CORRELATION_ENFORCEMENT = 0.22;
        private const double EXTREME_RESPONSE_THRESHOLD = 0.8;
        
        // Trait correlation matrix (12-trait expanded model)
        // Order: Honesty-Humility, Emotionality, Extraversion, Agreeableness, Conscientiousness, Openness,
        //        Dominance, Vigilance, Self-Transcendence, Abstract Orientation, Value Orientation, Flexibility
        private readonly double[,] _traitCorrelations = new double[,]
        {
            //    HH    Em    Ex    Ag    Co    Op    Do    Vi    ST    AO    VO    Fl
            { 1.00, 0.08, 0.12, 0.42, 0.31, 0.09, -0.52, -0.34, 0.56, 0.05, 0.48, 0.28 }, // Honesty-Humility
            { 0.08, 1.00, -0.28, 0.02, 0.18, 0.07, -0.41, 0.58, 0.21, 0.03, 0.33, 0.19 }, // Emotionality
            { 0.12, -0.28, 1.00, 0.38, 0.29, 0.34, 0.64, -0.35, 0.26, 0.19, -0.08, -0.12 }, // Extraversion
            { 0.42, 0.02, 0.38, 1.00, 0.12, 0.14, -0.48, -0.67, 0.52, 0.08, 0.59, -0.09 }, // Agreeableness
            { 0.31, 0.18, 0.29, 0.12, 1.00, 0.24, 0.31, 0.08, -0.11, -0.35, -0.29, -0.76 }, // Conscientiousness
            { 0.09, 0.07, 0.34, 0.14, 0.24, 1.00, 0.08, -0.28, 0.73, 0.69, 0.18, 0.45 }, // Openness
            { -0.52, -0.41, 0.64, -0.48, 0.31, 0.08, 1.00, -0.12, -0.35, 0.11, -0.31, 0.15 }, // Dominance
            { -0.34, 0.58, -0.35, -0.67, 0.08, -0.28, -0.12, 1.00, -0.31, -0.38, -0.45, 0.23 }, // Vigilance
            { 0.56, 0.21, 0.26, 0.52, -0.11, 0.73, -0.35, -0.31, 1.00, 0.64, 0.71, 0.41 }, // Self-Transcendence
            { 0.05, 0.03, 0.19, 0.08, -0.35, 0.69, 0.11, -0.38, 0.64, 1.00, 0.22, 0.12 }, // Abstract Orientation
            { 0.48, 0.33, -0.08, 0.59, -0.29, 0.18, -0.31, -0.45, 0.71, 0.22, 1.00, 0.38 }, // Value Orientation
            { 0.28, 0.19, -0.12, -0.09, -0.76, 0.45, 0.15, 0.23, 0.41, 0.12, 0.38, 1.00 } // Flexibility
        };

        // Trait index mapping for 12-trait model
        private readonly Dictionary<string, int> _traitIndex = new()
        {
            { "Honesty-Humility", 0 },
            { "Emotionality", 1 },
            { "Extraversion", 2 },
            { "Agreeableness", 3 },
            { "Conscientiousness", 4 },
            { "Openness", 5 },
            { "Dominance", 6 },
            { "Vigilance", 7 },
            { "Self-Transcendence", 8 },
            { "Abstract Orientation", 9 },
            { "Value Orientation", 10 },
            { "Flexibility", 11 }
        };

        // Critical trait pairs for special handling (updated for 12-trait model)
        private readonly List<(int trait1, int trait2, double expectedCorr, double force)> _criticalPairs = new()
        {
            (0, 6, -0.52, 0.4),  // Honesty-Humility - Dominance (strong negative)
            (1, 7, 0.58, 0.35),  // Emotionality - Vigilance (strong positive)
            (2, 6, 0.64, 0.4),   // Extraversion - Dominance (strong positive)
            (3, 7, -0.67, 0.45), // Agreeableness - Vigilance (very strong negative)
            (4, 11, -0.76, 0.5), // Conscientiousness - Flexibility (very strong negative)
            (5, 8, 0.73, 0.4),   // Openness - Self-Transcendence (strong positive)
            (5, 9, 0.69, 0.4),   // Openness - Abstract Orientation (strong positive)
            (8, 9, 0.64, 0.35),  // Self-Transcendence - Abstract Orientation (strong positive)
            (8, 10, 0.71, 0.4)   // Self-Transcendence - Value Orientation (strong positive)
        };

        // Value mapping for choice scores
        private readonly Dictionary<int, string> _valueDescriptions = new()
        {
            { 1, "Very Low" },
            { 2, "Low" },
            { 3, "Moderate" },
            { 4, "High" },
            { 5, "Very High" }
        };

        // Adjustment mapping for correlations
        private readonly Dictionary<string, double> _adjustmentMap = new()
        {
            { "++", 0.6 },   // Very strong positive
            { "+", 0.5 },    // Strong positive
            { "+m", 0.25 },  // Moderate positive
            { "0", 0.0 },    // Neutral
            { "-m", -0.25 }, // Moderate negative
            { "-", -0.5 },   // Strong negative
            { "--", -0.6 }   // Very strong negative
        };

        // MBTI type score ranges for the 12-trait model
        private readonly Dictionary<string, Dictionary<string, (double min, double max)>> _mbtiTypeRanges = new()
        {
            ["ESTJ"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 0.4),
                ["Extraversion"] = (0.65, 1.0),
                ["Agreeableness"] = (0.0, 0.45),
                ["Conscientiousness"] = (0.7, 1.0),
                ["Openness"] = (0.0, 0.45),
                ["Dominance"] = (0.7, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["ENTJ"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.65, 1.0),
                ["Agreeableness"] = (0.0, 0.45),
                ["Conscientiousness"] = (0.7, 1.0),
                ["Openness"] = (0.55, 1.0),
                ["Dominance"] = (0.8, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["ESFJ"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.6, 1.0),
                ["Extraversion"] = (0.65, 1.0),
                ["Agreeableness"] = (0.65, 1.0),
                ["Conscientiousness"] = (0.65, 1.0),
                ["Openness"] = (0.0, 0.45),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["ENFJ"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.65, 1.0),
                ["Agreeableness"] = (0.65, 1.0),
                ["Conscientiousness"] = (0.6, 1.0),
                ["Openness"] = (0.55, 1.0),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.7, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["ESTP"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.7, 1.0),
                ["Agreeableness"] = (0.0, 0.55),
                ["Conscientiousness"] = (0.0, 0.45),
                ["Openness"] = (0.0, 0.5),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.65, 1.0)
            },
            ["ENTP"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.65, 1.0),
                ["Agreeableness"] = (0.0, 0.55),
                ["Conscientiousness"] = (0.0, 0.45),
                ["Openness"] = (0.7, 1.0),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["ESFP"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.6, 1.0),
                ["Extraversion"] = (0.7, 1.0),
                ["Agreeableness"] = (0.6, 1.0),
                ["Conscientiousness"] = (0.0, 0.4),
                ["Openness"] = (0.0, 0.5),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["ENFP"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.65, 1.0),
                ["Agreeableness"] = (0.6, 1.0),
                ["Conscientiousness"] = (0.0, 0.45),
                ["Openness"] = (0.7, 1.0),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["ISTJ"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.0, 0.45),
                ["Agreeableness"] = (0.0, 0.55),
                ["Conscientiousness"] = (0.75, 1.0),
                ["Openness"] = (0.0, 0.45),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["INTJ"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.0, 0.45),
                ["Agreeableness"] = (0.0, 0.5),
                ["Conscientiousness"] = (0.65, 1.0),
                ["Openness"] = (0.65, 1.0),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["ISFJ"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.0, 0.45),
                ["Agreeableness"] = (0.65, 1.0),
                ["Conscientiousness"] = (0.65, 1.0),
                ["Openness"] = (0.0, 0.45),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["INFJ"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.0, 0.45),
                ["Agreeableness"] = (0.65, 1.0),
                ["Conscientiousness"] = (0.6, 1.0),
                ["Openness"] = (0.65, 1.0),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.7, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["ISTP"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.0, 0.45),
                ["Agreeableness"] = (0.0, 0.55),
                ["Conscientiousness"] = (0.0, 0.45),
                ["Openness"] = (0.0, 0.5),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["INTP"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.0, 0.45),
                ["Agreeableness"] = (0.0, 0.5),
                ["Conscientiousness"] = (0.0, 0.45),
                ["Openness"] = (0.7, 1.0),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["ISFP"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.0, 0.45),
                ["Agreeableness"] = (0.65, 1.0),
                ["Conscientiousness"] = (0.0, 0.4),
                ["Openness"] = (0.0, 0.5),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            },
            ["INFP"] = new()
            {
                ["Honesty-Humility"] = (0.0, 1.0),
                ["Emotionality"] = (0.0, 1.0),
                ["Extraversion"] = (0.0, 0.45),
                ["Agreeableness"] = (0.7, 1.0),
                ["Conscientiousness"] = (0.0, 0.4),
                ["Openness"] = (0.65, 1.0),
                ["Dominance"] = (0.0, 1.0),
                ["Vigilance"] = (0.0, 1.0),
                ["Self-Transcendence"] = (0.0, 1.0),
                ["Abstract Orientation"] = (0.0, 1.0),
                ["Value Orientation"] = (0.0, 1.0),
                ["Flexibility"] = (0.0, 1.0)
            }
        };

        // Scoring accumulators
        private readonly Dictionary<string, double> _sumValueWeight = new();
        private readonly Dictionary<string, double> _sumWeight = new();

        public PersonalityScorer()
        {
            InitializeAccumulators();
        }

        private void InitializeAccumulators()
        {
            foreach (var trait in _traitIndex.Keys)
            {
                _sumValueWeight[trait] = 0.0;
                _sumWeight[trait] = 0.0;
            }
        }

        public Task<PersonalityScoringResult> CalculateScoresAsync(List<Answer> answers)
        {
            return Task.FromResult(ProcessAnswersWithCorrelations(answers));
        }

        public PersonalityScoringResult ProcessAnswersWithCorrelations(List<Answer> answers)
        {
            // Reset accumulators
            InitializeAccumulators();

            // Step 1: Process each answer with correlations
            foreach (var answer in answers)
            {
                ProcessAnswer(answer);
            }

            // Step 2: Calculate base scores
            var baseScores = CalculateBaseScores();

            // Step 3: Apply multi-phase correlation adjustments
            var adjustedScores = ApplyCorrelationAdjustments(baseScores);

            // Step 4: Generate personality profile
            return GeneratePersonalityProfile(adjustedScores, baseScores);
        }

        private void ProcessAnswer(Answer answer)
        {
            // Get the trait this question measures (simplified - would need question metadata)
            var primaryTrait = GetPrimaryTraitForQuestion(answer.QuestionId);
            if (primaryTrait == null) return;

            var score = (double)answer.Value;

            // Direct contribution to primary trait
            _sumValueWeight[primaryTrait] += score * W_DIRECT;
            _sumWeight[primaryTrait] += W_DIRECT;

            // Correlated contributions to other traits
            var correlations = GetCorrelationsForQuestion(answer.QuestionId, answer.Value);
            foreach (var correlation in correlations)
            {
                var adjustment = _adjustmentMap.GetValueOrDefault(correlation.Value, 0.0);
                var deviation = score - 3.0; // Neutral point
                var correlatedScore = 3.0 + (deviation * adjustment * 2.0);

                // Clamp to valid range
                correlatedScore = Math.Max(1.0, Math.Min(5.0, correlatedScore));

                _sumValueWeight[correlation.Key] += correlatedScore * W_CORRELATED;
                _sumWeight[correlation.Key] += W_CORRELATED;
            }
        }

        private Dictionary<string, double> CalculateBaseScores()
        {
            var baseScores = new Dictionary<string, double>();

            foreach (var trait in _traitIndex.Keys)
            {
                if (_sumWeight[trait] > 0)
                {
                    var avgScore = _sumValueWeight[trait] / _sumWeight[trait];
                    // Normalize from 1-5 scale to 0-1 scale
                    var normalizedScore = (avgScore - 1.0) / 4.0;
                    baseScores[trait] = Math.Max(0.0, Math.Min(1.0, normalizedScore));
                }
                else
                {
                    baseScores[trait] = 0.5; // Default neutral
                }
            }

            return baseScores;
        }

        private Dictionary<string, double> ApplyCorrelationAdjustments(Dictionary<string, double> baseScores)
        {
            var adjustedScores = new Dictionary<string, double>(baseScores);

            // Phase 1: General correlation adjustment
            adjustedScores = ApplyPhase1GeneralAdjustment(adjustedScores, baseScores);

            // Phase 2: Pair-specific enforcement
            adjustedScores = ApplyPhase2PairEnforcement(adjustedScores, baseScores);

            // Phase 3: Critical pairs special handling
            adjustedScores = ApplyPhase3CriticalPairs(adjustedScores, baseScores);

            // Phase 4: Final direct score influence
            adjustedScores = ApplyPhase4DirectInfluence(adjustedScores, baseScores);

            return adjustedScores;
        }

        private Dictionary<string, double> ApplyPhase1GeneralAdjustment(
            Dictionary<string, double> currentScores,
            Dictionary<string, double> baseScores)
        {
            var adjustedScores = new Dictionary<string, double>(currentScores);
            var traits = _traitIndex.Keys.ToArray();

            foreach (var trait in traits)
            {
                var traitIdx = _traitIndex[trait];
                var targetScore = 0.0;
                var totalCorrelation = 0.0;

                // Calculate target based on correlations with other traits
                for (int i = 0; i < traits.Length; i++)
                {
                    if (i != traitIdx)
                    {
                        var otherTrait = traits[i];
                        var correlation = _traitCorrelations[traitIdx, i];
                        var otherScore = currentScores[otherTrait];

                        // Calculate expected score based on correlation
                        var expectedContribution = 0.5 + (otherScore - 0.5) * correlation;
                        targetScore += expectedContribution * Math.Abs(correlation);
                        totalCorrelation += Math.Abs(correlation);
                    }
                }

                if (totalCorrelation > 0)
                {
                    targetScore /= totalCorrelation;

                    // Calculate adjustment
                    var currentScore = adjustedScores[trait];
                    var adjustment = (targetScore - currentScore) * CORRELATION_ENFORCEMENT;

                    // Reduce adjustment for extreme responses or strong base scores
                    var extremenessFactor = CalculateExtreamenessFactor(baseScores[trait]);
                    adjustment *= extremenessFactor;

                    adjustedScores[trait] = Math.Max(0.0, Math.Min(1.0, currentScore + adjustment));
                }
            }

            return adjustedScores;
        }

        private double CalculateExtreamenessFactor(double baseScore)
        {
            // Reduce adjustment strength for extreme scores
            if (baseScore > EXTREME_RESPONSE_THRESHOLD || baseScore < (1.0 - EXTREME_RESPONSE_THRESHOLD))
            {
                return 0.3; // Strong direct evidence, reduce correlation adjustment
            }
            return 1.0; // Normal adjustment
        }

        private Dictionary<string, double> ApplyPhase2PairEnforcement(
            Dictionary<string, double> currentScores,
            Dictionary<string, double> baseScores)
        {
            var adjustedScores = new Dictionary<string, double>(currentScores);

            foreach (var (trait1Idx, trait2Idx, expectedCorr, force) in _criticalPairs)
            {
                var trait1 = _traitIndex.Keys.ElementAt(trait1Idx);
                var trait2 = _traitIndex.Keys.ElementAt(trait2Idx);

                var score1 = adjustedScores[trait1];
                var score2 = adjustedScores[trait2];

                if (expectedCorr > 0) // Positive correlation expected
                {
                    // If scores are on opposite sides of neutral
                    if ((score1 > 0.5 && score2 < 0.5) || (score1 < 0.5 && score2 > 0.5))
                    {
                        // Move the score closer to neutral towards the other
                        if (Math.Abs(score1 - 0.5) < Math.Abs(score2 - 0.5))
                        {
                            adjustedScores[trait1] = MoveTowardsSameSign(score1, score2, force);
                        }
                        else
                        {
                            adjustedScores[trait2] = MoveTowardsSameSign(score2, score1, force);
                        }
                    }
                }
                else if (expectedCorr < 0) // Negative correlation expected
                {
                    // If scores are on the same side of neutral
                    if ((score1 > 0.5 && score2 > 0.5) || (score1 < 0.5 && score2 < 0.5))
                    {
                        // Move one to the opposite side
                        if (Math.Abs(score1 - 0.5) < Math.Abs(score2 - 0.5))
                        {
                            adjustedScores[trait1] = MoveToOppositeSign(score1, score2, force);
                        }
                        else
                        {
                            adjustedScores[trait2] = MoveToOppositeSign(score2, score1, force);
                        }
                    }
                }
            }

            return adjustedScores;
        }

        private double MoveTowardsSameSign(double scoreToMove, double targetScore, double force)
        {
            var target = targetScore > 0.5 ? 0.6 : 0.4;
            var adjustment = (target - scoreToMove) * force;
            return Math.Max(0.0, Math.Min(1.0, scoreToMove + adjustment));
        }

        private double MoveToOppositeSign(double scoreToMove, double referenceScore, double force)
        {
            var target = referenceScore > 0.5 ? 0.3 : 0.7;
            var adjustment = (target - scoreToMove) * force;
            return Math.Max(0.0, Math.Min(1.0, scoreToMove + adjustment));
        }

        private Dictionary<string, double> ApplyPhase3CriticalPairs(
            Dictionary<string, double> currentScores,
            Dictionary<string, double> baseScores)
        {
            var adjustedScores = new Dictionary<string, double>(currentScores);

            foreach (var (trait1Idx, trait2Idx, expectedCorr, baseForce) in _criticalPairs)
            {
                var trait1 = _traitIndex.Keys.ElementAt(trait1Idx);
                var trait2 = _traitIndex.Keys.ElementAt(trait2Idx);

                var score1 = adjustedScores[trait1];
                var score2 = adjustedScores[trait2];
                
                // Calculate adaptive force based on base scores strength
                var directStrength1 = CalculateDirectStrength(baseScores[trait1]);
                var directStrength2 = CalculateDirectStrength(baseScores[trait2]);
                
                var adaptiveForce1 = baseForce * (1.0 - directStrength1);
                var adaptiveForce2 = baseForce * (1.0 - directStrength2);

                if (expectedCorr > 0) // Positive correlation expected
                {
                    // If scores are on opposite sides of neutral
                    if ((score1 > 0.5 && score2 < 0.5) || (score1 < 0.5 && score2 > 0.5))
                    {
                        // Move the score with weaker direct evidence
                        if (directStrength1 < directStrength2)
                        {
                            adjustedScores[trait1] = MoveTowardsSameSign(score1, score2, adaptiveForce1);
                        }
                        else
                        {
                            adjustedScores[trait2] = MoveTowardsSameSign(score2, score1, adaptiveForce2);
                        }
                    }
                }
                else if (expectedCorr < 0) // Negative correlation expected
                {
                    // If scores are on the same side of neutral
                    if ((score1 > 0.5 && score2 > 0.5) || (score1 < 0.5 && score2 < 0.5))
                    {
                        // Move the score with weaker direct evidence to opposite side
                        if (directStrength1 < directStrength2)
                        {
                            adjustedScores[trait1] = MoveToOppositeSign(score1, score2, adaptiveForce1);
                        }
                        else
                        {
                            adjustedScores[trait2] = MoveToOppositeSign(score2, score1, adaptiveForce2);
                        }
                    }
                }
            }

            return adjustedScores;
        }

        private double CalculateDirectStrength(double baseScore)
        {
            // Calculate how strong the direct evidence is
            // Strong evidence = far from neutral (0.5)
            var distanceFromNeutral = Math.Abs(baseScore - 0.5);
            return Math.Min(1.0, distanceFromNeutral * 2.0); // Scale to 0-1
        }

        private Dictionary<string, double> ApplyPhase4DirectInfluence(
            Dictionary<string, double> currentScores,
            Dictionary<string, double> baseScores)
        {
            var finalScores = new Dictionary<string, double>(currentScores);

            foreach (var trait in _traitIndex.Keys)
            {
                var baseScore = baseScores[trait];
                var adjustedScore = finalScores[trait];

                // Preserve very high base scores
                if (baseScore > 0.9 && adjustedScore < 0.65)
                {
                    finalScores[trait] = Math.Max(adjustedScore, 0.65);
                }

                // Preserve very low base scores
                if (baseScore < 0.1 && adjustedScore > 0.35)
                {
                    finalScores[trait] = Math.Min(adjustedScore, 0.35);
                }

                // General preservation for strong signals
                if (baseScore > 0.8 && adjustedScore < baseScore - 0.3)
                {
                    finalScores[trait] = baseScore - 0.3;
                }

                if (baseScore < 0.2 && adjustedScore > baseScore + 0.3)
                {
                    finalScores[trait] = baseScore + 0.3;
                }
            }

            return finalScores;
        }

        private PersonalityScoringResult GeneratePersonalityProfile(
            Dictionary<string, double> finalScores,
            Dictionary<string, double> baseScores)
        {
            var traitScores = new List<TraitScore>();

            foreach (var trait in _traitIndex.Keys)
            {
                var score = finalScores[trait] * 100; // Convert to 0-100 scale
                traitScores.Add(new TraitScore
                {
                    Trait = MapTraitNameToEnum(trait),
                    TraitName = trait,
                    Score = score,
                    Description = GenerateTraitDescription(trait, score)
                });
            }

            // Calculate MBTI type using dual method approach
            var (mbtiType, confidence) = CalculateMbtiType(finalScores);

            return new PersonalityScoringResult
            {
                TraitScores = traitScores,
                Summary = GenerateSummary(traitScores),
                CompletedAt = DateTime.UtcNow,
                MbtiType = mbtiType,
                Confidence = confidence
            };
        }

        private PersonalityTrait MapTraitNameToEnum(string traitName)
        {
            return traitName switch
            {
                "Honesty-Humility" => PersonalityTrait.HonestyHumility,
                "Emotionality" => PersonalityTrait.Emotionality,
                "Extraversion" => PersonalityTrait.Extraversion,
                "Agreeableness" => PersonalityTrait.Agreeableness,
                "Conscientiousness" => PersonalityTrait.Conscientiousness,
                "Openness" => PersonalityTrait.Openness,
                "Dominance" => PersonalityTrait.Dominance,
                "Vigilance" => PersonalityTrait.Vigilance,
                "Self-Transcendence" => PersonalityTrait.SelfTranscendence,
                "Abstract Orientation" => PersonalityTrait.AbstractOrientation,
                "Value Orientation" => PersonalityTrait.ValueOrientation,
                "Flexibility" => PersonalityTrait.Flexibility,
                _ => PersonalityTrait.Extraversion // Default fallback
            };
        }

        private (string mbtiType, double confidence) CalculateMbtiType(Dictionary<string, double> scores)
        {
            // Method 1: Direct mapping based on trait combinations
            var (directType, directConfidence) = CalculateMbtiDirect(scores);
            
            // Method 2: Nearest neighbor to ideal MBTI profiles
            var (nearestType, nearestConfidence) = CalculateMbtiNearestNeighbor(scores);
            
            // Return the method with higher confidence
            if (directConfidence >= nearestConfidence)
            {
                return (directType, directConfidence);
            }
            else
            {
                return (nearestType, nearestConfidence);
            }
        }

        private (string type, double confidence) CalculateMbtiDirect(Dictionary<string, double> scores)
        {
            var extraversion = scores["Extraversion"];
            var agreeableness = scores["Agreeableness"];
            var conscientiousness = scores["Conscientiousness"];
            var emotionality = scores["Emotionality"];
            var openness = scores["Openness"];
            var abstractOrientation = scores["Abstract Orientation"];
            var valueOrientation = scores["Value Orientation"];
            var flexibility = scores["Flexibility"];

            // E/I: Based on Extraversion
            var e_i = extraversion > 0.5 ? 'E' : 'I';
            var e_i_strength = Math.Abs(extraversion - 0.5);

            // S/N: Based on combination of Openness and Abstract Orientation
            var intuition_score = (openness + abstractOrientation) / 2.0;
            var s_n = intuition_score > 0.5 ? 'N' : 'S';
            var s_n_strength = Math.Abs(intuition_score - 0.5);

            // T/F: Based on combination of Agreeableness and Value Orientation
            var feeling_score = (agreeableness + valueOrientation) / 2.0;
            var t_f = feeling_score > 0.5 ? 'F' : 'T';
            var t_f_strength = Math.Abs(feeling_score - 0.5);

            // J/P: Based on Conscientiousness vs Flexibility
            var judging_score = conscientiousness - flexibility + 0.5; // Normalize around 0.5
            judging_score = Math.Max(0.0, Math.Min(1.0, judging_score)); // Clamp to 0-1
            var j_p = judging_score > 0.5 ? 'J' : 'P';
            var j_p_strength = Math.Abs(judging_score - 0.5);

            var type = $"{e_i}{s_n}{t_f}{j_p}";
            
            // Confidence is average of all dimension strengths
            var confidence = (e_i_strength + s_n_strength + t_f_strength + j_p_strength) / 4.0;
            
            return (type, confidence);
        }

        private (string type, double confidence) CalculateMbtiNearestNeighbor(Dictionary<string, double> scores)
        {
            var userVector = new[] {
                scores["Honesty-Humility"],
                scores["Emotionality"],
                scores["Extraversion"],
                scores["Agreeableness"], 
                scores["Conscientiousness"],
                scores["Openness"],
                scores["Dominance"],
                scores["Vigilance"],
                scores["Self-Transcendence"],
                scores["Abstract Orientation"],
                scores["Value Orientation"],
                scores["Flexibility"]
            };

            var bestMatch = "";
            var highestScore = 0.0;

            foreach (var (type, ranges) in _mbtiTypeRanges)
            {
                var matchScore = 0.0;
                var validTraits = 0;

                foreach (var trait in _traitIndex.Keys)
                {
                    if (ranges.ContainsKey(trait))
                    {
                        var userScore = scores[trait];
                        var (min, max) = ranges[trait];
                        
                        if (userScore >= min && userScore <= max)
                        {
                            // Score based on how well it fits within the range
                            var rangeSize = max - min;
                            if (rangeSize > 0)
                            {
                                // Closer to center of range = higher score
                                var center = (min + max) / 2.0;
                                var distanceFromCenter = Math.Abs(userScore - center);
                                var normalizedDistance = distanceFromCenter / (rangeSize / 2.0);
                                matchScore += 1.0 - normalizedDistance;
                            }
                            else
                            {
                                matchScore += 1.0; // Perfect match for narrow range
                            }
                        }
                        else
                        {
                            // Penalty for being outside range
                            var distanceOutside = Math.Min(Math.Abs(userScore - min), Math.Abs(userScore - max));
                            matchScore -= distanceOutside * 2.0; // Penalty factor
                        }
                        validTraits++;
                    }
                }

                if (validTraits > 0)
                {
                    matchScore /= validTraits; // Average score
                    if (matchScore > highestScore)
                    {
                        highestScore = matchScore;
                        bestMatch = type;
                    }
                }
            }

            // Convert match score to confidence (0-1 range)
            var confidence = Math.Max(0.0, Math.Min(1.0, highestScore));
            
            return (bestMatch, confidence);
        }

        private double CalculateEuclideanDistance(double[] vector1, double[] vector2)
        {
            if (vector1.Length != vector2.Length)
                throw new ArgumentException("Vectors must have same length");

            double sumSquaredDifferences = 0;
            for (int i = 0; i < vector1.Length; i++)
            {
                var difference = vector1[i] - vector2[i];
                sumSquaredDifferences += difference * difference;
            }

            return Math.Sqrt(sumSquaredDifferences);
        }

        private string GenerateTraitDescription(string trait, double score)
        {
            var level = score switch
            {
                >= 80 => "Very High",
                >= 65 => "High",
                >= 35 => "Moderate",
                >= 20 => "Low",
                _ => "Very Low"
            };

            return $"{level} {trait}";
        }

        private string GenerateSummary(List<TraitScore> traitScores)
        {
            var highestTrait = traitScores.OrderByDescending(t => t.Score).First();
            var lowestTrait = traitScores.OrderBy(t => t.Score).First();
            
            var summary = $"Your personality profile shows strongest tendencies in {highestTrait.TraitName} ({highestTrait.Score:F0}%) " +
                         $"and lowest in {lowestTrait.TraitName} ({lowestTrait.Score:F0}%). ";

            // Add interpretation based on trait combinations
            var extraversionScore = traitScores.First(t => t.TraitName == "Extraversion").Score;
            var conscientiousnessScore = traitScores.First(t => t.TraitName == "Conscientiousness").Score;
            var opennessScore = traitScores.First(t => t.TraitName == "Openness").Score;

            if (extraversionScore > 70 && opennessScore > 70)
            {
                summary += "This combination suggests an outgoing, innovative personality who thrives on new experiences and social interaction. ";
            }
            else if (extraversionScore < 30 && conscientiousnessScore > 70)
            {
                summary += "This profile indicates a thoughtful, disciplined approach with a preference for deep focus and careful planning. ";
            }
            else if (opennessScore > 70 && conscientiousnessScore > 70)
            {
                summary += "You combine creative thinking with strong organizational skills, making you effective at implementing innovative ideas. ";
            }

            return summary;
        }

        // Helper methods to get question metadata
        // TODO: Replace with database-driven metadata loading
        private string? GetPrimaryTraitForQuestion(int questionId)
        {
            // Updated mapping for 12-trait model
            // For now, mapping existing questions to appropriate traits
            return questionId switch
            {
                1 or 2 => "Extraversion",
                3 or 4 => "Agreeableness", 
                5 or 6 => "Conscientiousness",
                7 or 8 => "Emotionality", // Changed from EmotionalStability to Emotionality
                9 or 10 => "Openness",
                _ => null
            };
        }

        private Dictionary<string, string> GetCorrelationsForQuestion(int questionId, int value)
        {
            // Enhanced correlation mapping for 12-trait model
            var correlations = new Dictionary<string, string>();

            switch (questionId)
            {
                case 1: // Extraversion: "You're at a party where you don't know many people"
                    if (value >= 4) // High extraversion
                    {
                        correlations["Dominance"] = "+"; // Extraverts often more dominant
                        correlations["Emotionality"] = "-m"; // May indicate emotional stability
                        correlations["Agreeableness"] = "+m"; // Social engagement
                        correlations["Vigilance"] = "-m"; // Less suspicious in social situations
                    }
                    else if (value <= 2) // Low extraversion (introverted)
                    {
                        correlations["Emotionality"] = "+m"; // May indicate social anxiety
                        correlations["Vigilance"] = "+m"; // More cautious/vigilant
                        correlations["Dominance"] = "-m"; // Less assertive in groups
                    }
                    break;

                case 2: // Extraversion: "Your ideal weekend activity"
                    if (value >= 4) // High extraversion
                    {
                        correlations["Agreeableness"] = "+m"; // Enjoys social connection
                        correlations["Flexibility"] = "+m"; // Open to social opportunities
                        correlations["Self-Transcendence"] = "+m"; // Engagement with others
                    }
                    else if (value <= 2) // Low extraversion
                    {
                        correlations["Conscientiousness"] = "+m"; // Prefers planned, structured activities
                        correlations["Abstract Orientation"] = "+m"; // May prefer intellectual activities
                        correlations["Openness"] = "+m"; // Internal exploration
                    }
                    break;

                case 3: // Agreeableness: "When someone disagrees with you strongly"
                    if (value >= 4) // High agreeableness
                    {
                        correlations["Honesty-Humility"] = "+"; // Humble, seeks understanding
                        correlations["Vigilance"] = "--"; // Very strong negative - not suspicious
                        correlations["Self-Transcendence"] = "+"; // Focus on others
                        correlations["Value Orientation"] = "+"; // Values harmony
                        correlations["Dominance"] = "-"; // Less dominant/aggressive
                    }
                    else if (value <= 2) // Low agreeableness
                    {
                        correlations["Vigilance"] = "+"; // More suspicious of others
                        correlations["Dominance"] = "+"; // More assertive/competitive
                        correlations["Honesty-Humility"] = "-m"; // May be more self-focused
                    }
                    break;

                case 4: // Agreeableness: "A colleague takes credit for your work"
                    if (value >= 4) // High agreeableness (handles diplomatically)
                    {
                        correlations["Honesty-Humility"] = "+m"; // Humble approach
                        correlations["Emotionality"] = "-m"; // Manages emotions well
                        correlations["Value Orientation"] = "+"; // Values relationships
                        correlations["Vigilance"] = "-m"; // Gives benefit of doubt
                    }
                    else if (value <= 2) // Low agreeableness (confrontational)
                    {
                        correlations["Dominance"] = "+"; // Asserts dominance
                        correlations["Vigilance"] = "+m"; // Suspicious of motives
                        correlations["Emotionality"] = "+m"; // May react emotionally
                    }
                    break;

                case 5: // Conscientiousness: "You have a major project due next week"
                    if (value >= 4) // High conscientiousness
                    {
                        correlations["Flexibility"] = "--"; // Very strong negative - not flexible
                        correlations["Emotionality"] = "-"; // Planning reduces anxiety
                        correlations["Honesty-Humility"] = "+m"; // Responsible approach
                        correlations["Dominance"] = "+m"; // Takes control of situation
                    }
                    else if (value <= 2) // Low conscientiousness
                    {
                        correlations["Flexibility"] = "+"; // More flexible/spontaneous
                        correlations["Emotionality"] = "+"; // May cause stress later
                        correlations["Abstract Orientation"] = "+m"; // Distracted by ideas
                    }
                    break;

                case 6: // Conscientiousness: "Your workspace/room typically looks"
                    if (value >= 4) // High conscientiousness
                    {
                        correlations["Flexibility"] = "-"; // Less flexible about disorder
                        correlations["Emotionality"] = "-m"; // Order reduces anxiety
                        correlations["Vigilance"] = "+m"; // Attention to details
                    }
                    else if (value <= 2) // Low conscientiousness
                    {
                        correlations["Flexibility"] = "+"; // More tolerant of disorder
                        correlations["Openness"] = "+m"; // Creative chaos
                        correlations["Abstract Orientation"] = "+m"; // Mind on bigger things
                    }
                    break;

                case 7: // Emotionality: "When facing a stressful situation"
                    if (value >= 4) // Low emotionality (handles stress well)
                    {
                        correlations["Extraversion"] = "+"; // Confident in situations
                        correlations["Dominance"] = "+"; // Takes charge under pressure
                        correlations["Vigilance"] = "-m"; // Not overwhelmed by threats
                        correlations["Agreeableness"] = "+m"; // Calm helps relationships
                    }
                    else if (value <= 2) // High emotionality (stressed easily)
                    {
                        correlations["Extraversion"] = "-"; // May withdraw
                        correlations["Vigilance"] = "+"; // Heightened threat sensitivity
                        correlations["Flexibility"] = "-m"; // Stress reduces adaptability
                    }
                    break;

                case 8: // Emotionality: "When you receive criticism"
                    if (value >= 4) // Low emotionality (handles criticism well)
                    {
                        correlations["Openness"] = "+"; // Open to feedback
                        correlations["Honesty-Humility"] = "+m"; // Accepts feedback humbly
                        correlations["Vigilance"] = "-m"; // Not defensive
                        correlations["Dominance"] = "+m"; // Confident response
                    }
                    else if (value <= 2) // High emotionality (sensitive to criticism)
                    {
                        correlations["Vigilance"] = "+"; // Defensive/suspicious
                        correlations["Agreeableness"] = "+m"; // Seeks approval/harmony
                        correlations["Conscientiousness"] = "+m"; // Perfectionist tendencies
                    }
                    break;

                case 9: // Openness: "When planning a vacation"
                    if (value >= 4) // High openness
                    {
                        correlations["Abstract Orientation"] = "+"; // Enjoys novel experiences
                        correlations["Self-Transcendence"] = "+"; // Seeks transcendent experiences
                        correlations["Flexibility"] = "+"; // Adaptable to new situations
                        correlations["Extraversion"] = "+m"; // Seeks external stimulation
                    }
                    else if (value <= 2) // Low openness
                    {
                        correlations["Conscientiousness"] = "+"; // Prefers planned, familiar
                        correlations["Vigilance"] = "+m"; // Cautious about unknown
                        correlations["Agreeableness"] = "+m"; // Considers others' comfort
                    }
                    break;

                case 10: // Openness: "Your approach to new ideas and concepts"
                    if (value >= 4) // High openness
                    {
                        correlations["Abstract Orientation"] = "+"; // Enjoys complex thinking
                        correlations["Self-Transcendence"] = "+m"; // Open to broader perspectives
                        correlations["Flexibility"] = "+"; // Adapts thinking
                        correlations["Conscientiousness"] = "+m"; // Disciplined exploration
                    }
                    else if (value <= 2) // Low openness
                    {
                        correlations["Conscientiousness"] = "+"; // Prefers concrete, proven
                        correlations["Vigilance"] = "+m"; // Skeptical of new ideas
                        correlations["Value Orientation"] = "+m"; // Sticks to established values
                    }
                    break;
            }

            return correlations;
        }
    }

    // Supporting model for the enhanced personality scoring result
    public class PersonalityScoringResult
    {
        public List<TraitScore> TraitScores { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public string MbtiType { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }
}
