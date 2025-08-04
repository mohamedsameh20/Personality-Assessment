namespace PersonalityAssessment.Api.Models
{
    // 12-trait personality model enum
    public enum PersonalityTrait
    {
        HonestyHumility = 1,
        Emotionality = 2,
        Extraversion = 3,
        Agreeableness = 4,
        Conscientiousness = 5,
        Openness = 6,
        Dominance = 7,
        Vigilance = 8,
        SelfTranscendence = 9,
        AbstractOrientation = 10,
        ValueOrientation = 11,
        Flexibility = 12
    }

    // Simple question model
    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public PersonalityTrait Trait { get; set; }
        public bool IsReversed { get; set; } // For reverse-scored questions
    }

    // User's answer to a question
    public class Answer
    {
        public int QuestionId { get; set; }
        public int Value { get; set; } // 1-5 scale (Strongly Disagree to Strongly Agree)
    }

    // Assessment request model
    public class AssessmentRequest
    {
        public List<Answer> Answers { get; set; } = new List<Answer>();
    }

    // Trait score result
    public class TraitScore
    {
        public PersonalityTrait Trait { get; set; }
        public string TraitName { get; set; } = string.Empty;
        public double Score { get; set; } // 0-100 scale
        public string Description { get; set; } = string.Empty;
    }

    // Assessment result
    public class AssessmentResult
    {
        public List<TraitScore> TraitScores { get; set; } = new List<TraitScore>();
        public DateTime CompletedAt { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string MbtiType { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }
}
