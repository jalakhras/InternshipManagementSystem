using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.People;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace InternshipManagementSystem.EntityFrameworkCore;

public partial class InternshipManagementSystemDbContext
{
    /// <summary>
    /// Every entity here implements <c>IMultiTenant</c>, so ABP's tenant filter
    /// applies to all of them. Without that the tenant boundary would exist for
    /// users but not for their data, and one tenant would read another's exams,
    /// question banks and results.
    /// </summary>
    private void ConfigureAssessment(ModelBuilder builder)
    {
        var prefix = InternshipManagementSystemConsts.DbTablePrefix;
        var schema = InternshipManagementSystemConsts.DbSchema;

        // ---------- Catalog: the tenant's own vocabulary ----------

        builder.Entity<CategorySet>(b =>
        {
            b.ToTable(prefix + "CategorySets", schema);
            b.ConfigureByConvention();
            b.Property(x => x.SingularName).IsRequired().HasMaxLength(64);
            b.Property(x => x.PluralName).IsRequired().HasMaxLength(64);
            b.Property(x => x.SubjectSingularName).IsRequired().HasMaxLength(64);
            b.Property(x => x.SubjectPluralName).IsRequired().HasMaxLength(64);
            b.Property(x => x.GroupSingularName).IsRequired().HasMaxLength(64);
            b.Property(x => x.GroupPluralName).IsRequired().HasMaxLength(64);
            b.HasIndex(x => x.TenantId).IsUnique();
        });

        builder.Entity<Category>(b =>
        {
            b.ToTable(prefix + "Categories", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Code).IsRequired().HasMaxLength(64);
            b.Property(x => x.Description).HasMaxLength(512);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        builder.Entity<Level>(b =>
        {
            b.ToTable(prefix + "Levels", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Code).IsRequired().HasMaxLength(64);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        builder.Entity<Topic>(b =>
        {
            b.ToTable(prefix + "Topics", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Code).IsRequired().HasMaxLength(64);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => x.ParentId);
        });

        // ---------- Exams and their question bank ----------

        builder.Entity<Exam>(b =>
        {
            b.ToTable(prefix + "Exams", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2048);
            b.Property(x => x.PassingPercentage).HasPrecision(5, 2);

            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.CategoryId, x.LevelId });

            b.HasMany(x => x.Questions).WithOne().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.QuestionGroups).WithOne().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Blueprint).WithOne().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuestionGroup>(b =>
        {
            b.ToTable(prefix + "QuestionGroups", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Instructions).HasMaxLength(2048);
            b.Property(x => x.StimulusBlobName).HasMaxLength(256);
            b.Property(x => x.StimulusMediaType).HasMaxLength(32);
            b.HasIndex(x => x.ExamId);

            b.HasMany(x => x.Questions).WithOne().HasForeignKey(x => x.QuestionGroupId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Question>(b =>
        {
            b.ToTable(prefix + "Questions", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Text).IsRequired();
            b.Property(x => x.Type).IsRequired().HasMaxLength(64);

            // Everything type-specific lives here. A new question type needs a new
            // grader and a new payload shape — never a new column.
            b.Property(x => x.Payload).IsRequired();

            b.Property(x => x.Score).HasPrecision(9, 2);
            b.Property(x => x.MediaBlobName).HasMaxLength(256);
            b.Property(x => x.MediaType).HasMaxLength(32);
            b.Property(x => x.DifficultyIndex).HasPrecision(5, 4);
            b.Property(x => x.DiscriminationIndex).HasPrecision(5, 4);

            b.HasIndex(x => x.ExamId);
            b.HasIndex(x => x.QuestionGroupId);
            // The blueprint draws by topic and difficulty, so it indexes on both.
            b.HasIndex(x => new { x.ExamId, x.TopicId, x.Difficulty });
        });

        builder.Entity<ExamBlueprintRule>(b =>
        {
            b.ToTable(prefix + "ExamBlueprintRules", schema);
            b.ConfigureByConvention();
            b.Property(x => x.QuestionType).HasMaxLength(64);
            b.HasIndex(x => x.ExamId);
        });

        // ---------- People and cohorts ----------

        builder.Entity<Candidate>(b =>
        {
            b.ToTable(prefix + "Candidates", schema);
            b.ConfigureByConvention();
            b.Property(x => x.FullName).IsRequired().HasMaxLength(256);
            b.Property(x => x.Email).IsRequired().HasMaxLength(256);
            b.Property(x => x.PhoneNumber).HasMaxLength(32);
            b.Property(x => x.Reference).HasMaxLength(256);

            // One person per email within a tenant; the same email may exist in another tenant.
            b.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.CategoryId });
        });

        builder.Entity<CandidateGroup>(b =>
        {
            b.ToTable(prefix + "CandidateGroups", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(1024);
            b.HasIndex(x => new { x.TenantId, x.CategoryId });

            b.HasMany(x => x.Members).WithOne(x => x.CandidateGroup)
                .HasForeignKey(x => x.CandidateGroupId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CandidateGroupMember>(b =>
        {
            b.ToTable(prefix + "CandidateGroupMembers", schema);
            b.ConfigureByConvention();
            b.HasIndex(x => new { x.CandidateGroupId, x.CandidateId }).IsUnique();

            b.HasOne(x => x.Candidate).WithMany(x => x.GroupMemberships)
                .HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Delivery ----------

        builder.Entity<Assignment>(b =>
        {
            b.ToTable(prefix + "Assignments", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Note).HasMaxLength(1024);
            b.HasIndex(x => new { x.TenantId, x.ExamId });
        });

        builder.Entity<ExamLink>(b =>
        {
            b.ToTable(prefix + "ExamLinks", schema);
            b.ConfigureByConvention();

            // The token is stored hashed: a leaked backup must not hand over working links.
            b.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
            b.Property(x => x.TokenPrefix).IsRequired().HasMaxLength(12);

            // Every access hashes the incoming token and looks it up here.
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.CandidateId });
            b.HasIndex(x => x.AssignmentId);
        });

        builder.Entity<Attempt>(b =>
        {
            b.ToTable(prefix + "Attempts", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Score).HasPrecision(9, 2);
            b.Property(x => x.MaxScore).HasPrecision(9, 2);
            b.Property(x => x.ScorePercentage).HasPrecision(5, 2);

            b.HasIndex(x => new { x.TenantId, x.ExamId });
            b.HasIndex(x => new { x.TenantId, x.CandidateId });

            // The timeout worker scans for unsubmitted attempts past their deadline.
            b.HasIndex(x => new { x.IsSubmitted, x.DeadlineAt });

            // The manual-review queue reads exactly this predicate.
            b.HasIndex(x => new { x.TenantId, x.NeedsManualReview, x.IsSubmitted });

            b.HasMany(x => x.Questions).WithOne().HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Answers).WithOne().HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AttemptQuestion>(b =>
        {
            b.ToTable(prefix + "AttemptQuestions", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Score).HasPrecision(9, 2);

            // The frozen form: one row per question on this taker's paper.
            b.HasIndex(x => new { x.AttemptId, x.Position });
            b.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
        });

        builder.Entity<Answer>(b =>
        {
            b.ToTable(prefix + "Answers", schema);
            b.ConfigureByConvention();
            b.Property(x => x.AwardedScore).HasPrecision(9, 2);
            b.Property(x => x.AnswerBlobName).HasMaxLength(256);
            b.Property(x => x.AnswerFileName).HasMaxLength(256);
            b.Property(x => x.ReviewComment).HasMaxLength(4000);

            // Autosave upserts on this key, so it must be unique.
            b.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.NeedsManualReview });
        });

        builder.Entity<IntegritySignal>(b =>
        {
            b.ToTable(prefix + "IntegritySignals", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Detail).HasMaxLength(2048);
            b.HasIndex(x => new { x.AttemptId, x.Type });
        });
    }
}
