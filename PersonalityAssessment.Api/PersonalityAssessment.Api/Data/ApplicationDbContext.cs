using Microsoft.EntityFrameworkCore;

namespace PersonalityAssessment.Api.Data
{
    // Entity classes for the database
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string UserType { get; set; } = "Regular"; // Regular, Admin
        
        // Navigation properties
        public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
        public PersonalityProfile? PersonalityProfile { get; set; }
    }

    public class Assessment
    {
        public int AssessmentId { get; set; }
        public int UserId { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Status { get; set; } = "InProgress"; // InProgress, Completed
        
        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<UserResponse> UserResponses { get; set; } = new List<UserResponse>();
    }

    public class UserResponse
    {
        public int ResponseId { get; set; }
        public int AssessmentId { get; set; }
        public int QuestionId { get; set; }
        public int AnswerValue { get; set; }
        public DateTime ResponseTime { get; set; }
        
        // Navigation properties
        public Assessment Assessment { get; set; } = null!;
    }

    public class PersonalityProfile
    {
        public int ProfileId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string ProfileData { get; set; } = string.Empty; // JSON data for trait scores
        public string MbtiType { get; set; } = string.Empty; // MBTI type result
        public string TraitScoresJson { get; set; } = string.Empty; // JSON array of normalized trait scores
        public double Confidence { get; set; } = 0.0; // Assessment confidence score
        
        // Navigation properties
        public User User { get; set; } = null!;
    }

    // Question entity - represents a personality assessment question (situation)
    public class QuestionEntity
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty; // The situation/question text
        public string PersonalityTrait { get; set; } = string.Empty; // Which trait this measures
        public bool IsReversed { get; set; } = false; // For reverse scoring
        public bool IsActive { get; set; } = true; // Can be disabled
        public DateTime CreatedDate { get; set; }
        public int SortOrder { get; set; } = 0; // For ordering questions
        
        // Navigation properties
        public ICollection<ChoiceEntity> Choices { get; set; } = new List<ChoiceEntity>();
        public ICollection<UserResponse> UserResponses { get; set; } = new List<UserResponse>();
    }

    // Choice entity - represents the 5 answer choices for each question
    public class ChoiceEntity
    {
        public int ChoiceId { get; set; }
        public int QuestionId { get; set; }
        public string ChoiceText { get; set; } = string.Empty; // The answer choice text
        public int ChoiceValue { get; set; } // 1-5 scale value
        public int SortOrder { get; set; } = 0; // Order of the choice (1-5)
        
        // Navigation properties
        public QuestionEntity Question { get; set; } = null!;
    }

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<UserResponse> UserResponses { get; set; }
        public DbSet<PersonalityProfile> PersonalityProfiles { get; set; }
        public DbSet<QuestionEntity> Questions { get; set; }
        public DbSet<ChoiceEntity> Choices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
            });

            // Assessment configuration
            modelBuilder.Entity<Assessment>(entity =>
            {
                entity.HasKey(e => e.AssessmentId);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.StartedDate).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Assessments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserResponse configuration
            modelBuilder.Entity<UserResponse>(entity =>
            {
                entity.HasKey(e => e.ResponseId);
                entity.Property(e => e.ResponseTime).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(e => e.Assessment)
                    .WithMany(a => a.UserResponses)
                    .HasForeignKey(e => e.AssessmentId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne<QuestionEntity>()
                    .WithMany(q => q.UserResponses)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // PersonalityProfile configuration
            modelBuilder.Entity<PersonalityProfile>(entity =>
            {
                entity.HasKey(e => e.ProfileId);
                entity.Property(e => e.ProfileData).HasColumnType("NVARCHAR(MAX)");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(e => e.User)
                    .WithOne(u => u.PersonalityProfile)
                    .HasForeignKey<PersonalityProfile>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Question configuration
            modelBuilder.Entity<QuestionEntity>(entity =>
            {
                entity.HasKey(e => e.QuestionId);
                entity.Property(e => e.QuestionText).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.PersonalityTrait).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.SortOrder);
            });

            // Choice configuration
            modelBuilder.Entity<ChoiceEntity>(entity =>
            {
                entity.HasKey(e => e.ChoiceId);
                entity.Property(e => e.ChoiceText).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ChoiceValue).IsRequired();
                entity.Property(e => e.SortOrder).IsRequired();
                
                entity.HasOne(e => e.Question)
                    .WithMany(q => q.Choices)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Ensure each question has exactly 5 choices
                entity.HasIndex(e => new { e.QuestionId, e.SortOrder }).IsUnique();
            });

            // Update UserResponse to reference QuestionEntity
            modelBuilder.Entity<UserResponse>(entity =>
            {
                entity.HasOne<QuestionEntity>()
                    .WithMany(q => q.UserResponses)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
