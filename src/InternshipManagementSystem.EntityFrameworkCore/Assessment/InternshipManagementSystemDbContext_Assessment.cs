using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.Tenancy;
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
            b.HasIndex(x => new { x.TenantId, x.CategoryId });
        });

        builder.Entity<Topic>(b =>
        {
            b.ToTable(prefix + "Topics", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Code).IsRequired().HasMaxLength(64);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => x.ParentId);
            b.HasIndex(x => new { x.TenantId, x.CategoryId });
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
            // Wider than the value it holds, which is always between zero and one.
            //
            // The running mean is updated in the database — a whole cohort answers
            // the same questions, and read-modify-write on those rows is a race
            // most of them lose — and EF casts the answer count to this column's
            // own precision to compute it. At decimal(5,4) that cast overflowed
            // the moment a question had been answered ten times, and every
            // submission in the cohort failed with an arithmetic overflow.
            b.Property(x => x.DifficultyIndex).HasPrecision(18, 4);
            b.Property(x => x.DiscriminationIndex).HasPrecision(5, 4);

            b.HasIndex(x => x.ExamId);
            b.HasIndex(x => x.QuestionGroupId);
            b.HasIndex(x => x.ExamSectionId);
            // The blueprint draws by topic and difficulty, so it indexes on both.
            b.HasIndex(x => new { x.ExamId, x.TopicId, x.Difficulty });

            // Bank questions have no ExamId, so the index above never serves them.
            // Drawing a form for a level is the hottest read in authoring and it
            // filters on exactly this shape.
            b.HasIndex(x => new { x.TenantId, x.CategoryId, x.LevelId, x.TopicId, x.Difficulty })
                .HasFilter("[ExamId] IS NULL");
        });

        builder.Entity<ExamSection>(b =>
        {
            b.ToTable(prefix + "ExamSections", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Instructions).HasMaxLength(2048);
            b.Property(x => x.MinimumPercentage).HasPrecision(5, 2);
            b.HasIndex(x => new { x.ExamId, x.DisplayOrder });
        });

        builder.Entity<ExamForm>(b =>
        {
            b.ToTable(prefix + "ExamForms", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Code).IsRequired().HasMaxLength(32);
            b.Property(x => x.MaxScore).HasPrecision(9, 2);

            // A code identifies a form on a result sheet, so two forms of one exam
            // sharing one is a result nobody can trace back to a paper.
            b.HasIndex(x => new { x.ExamId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.ExamId, x.Status });

            b.HasMany(x => x.Questions).WithOne().HasForeignKey(x => x.ExamFormId);
        });

        builder.Entity<ExamFormQuestion>(b =>
        {
            b.ToTable(prefix + "ExamFormQuestions", schema);
            b.ConfigureByConvention();
            b.Property(x => x.Score).HasPrecision(9, 2);

            // The same question twice on one form is caught at publish, but the
            // database is where a guarantee belongs.
            b.HasIndex(x => new { x.ExamFormId, x.QuestionId }).IsUnique();
            b.HasIndex(x => new { x.ExamFormId, x.DisplayOrder });
        });

        builder.Entity<ExamBlueprintRule>(b =>
        {
            b.ToTable(prefix + "ExamBlueprintRules", schema);
            b.ConfigureByConvention();
            b.Property(x => x.QuestionType).HasMaxLength(64);
            b.HasIndex(x => x.ExamId);
        });

        builder.Entity<TenantBranding>(b =>
        {
            b.ToTable(prefix + "TenantBranding", schema);
            b.ConfigureByConvention();
            b.Property(x => x.DisplayName).IsRequired().HasMaxLength(128);
            b.Property(x => x.DisplayNameAlternate).HasMaxLength(128);
            b.Property(x => x.LogoBlobName).HasMaxLength(256);
            b.Property(x => x.IconBlobName).HasMaxLength(256);

            // #rrggbb and nothing else. See TenantBranding.IsUsableColor.
            b.Property(x => x.PrimaryColor).HasMaxLength(7);
            b.Property(x => x.CertificateFooter).HasMaxLength(512);
            b.Property(x => x.SupportEmail).HasMaxLength(256);

            // One identity per tenant. Without this a second row is silently created
            // and which of the two the shell reads becomes a matter of ordering.
            b.HasIndex(x => x.TenantId).IsUnique();
        });

        // ---------- People and cohorts ----------

        builder.Entity<Candidate>(b =>
        {
            b.ToTable(prefix + "Candidates", schema);
            b.ConfigureByConvention();
            b.Property(x => x.FullName).IsRequired().HasMaxLength(256);

            // Same length as the name it is folded from: the folding only ever
            // removes characters, never adds them.
            b.Property(x => x.NormalisedName).IsRequired().HasMaxLength(256);

            b.Property(x => x.Email).IsRequired().HasMaxLength(256);
            b.Property(x => x.PhoneNumber).HasMaxLength(32);
            b.Property(x => x.Reference).HasMaxLength(256);

            // One person per email within a tenant; the same email may exist in another tenant.
            b.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.CategoryId });

            // Searched on every keystroke of the roll's person picker, in a
            // centre that may hold thousands.
            b.HasIndex(x => new { x.TenantId, x.NormalisedName });
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

            // At most one attempt in progress per link. The service resumes a running
            // attempt rather than creating a second, but a double-click or a retried
            // request can race past that check — two concurrent attempts would split a
            // taker's answers across papers and lose half their work. Enforced here so
            // the database refuses it regardless of what any caller does.
            b.HasIndex(x => x.ExamLinkId)
                .IsUnique()
                .HasFilter("[IsSubmitted] = 0 AND [ExamLinkId] IS NOT NULL");

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

            // Delivery and the result both slice one paper by section: which part
            // the candidate is in, and what each part scored. Both read exactly
            // this shape, and both run while somebody is sitting or reading their
            // own result.
            b.HasIndex(x => new { x.AttemptId, x.ExamSectionId, x.Position });
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
