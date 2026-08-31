using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace InternshipManagementSystem.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public partial class InternshipManagementSystemDbContext :
    AbpDbContext<InternshipManagementSystemDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    #region Assessment

    // Declared explicitly rather than relying on the model alone: ABP registers
    // default repositories from the DbSet properties it finds, so an entity that is
    // only configured in OnModelCreating gets tables but no IRepository<T> — which
    // then fails at resolve time, not at build time.

    public DbSet<Assessment.Catalog.CategorySet> CategorySets { get; set; }
    public DbSet<Assessment.Catalog.Category> Categories { get; set; }
    public DbSet<Assessment.Catalog.Level> Levels { get; set; }
    public DbSet<Assessment.Catalog.Topic> Topics { get; set; }

    public DbSet<Assessment.Exams.Exam> Exams { get; set; }
    public DbSet<Assessment.Exams.QuestionGroup> QuestionGroups { get; set; }
    public DbSet<Assessment.Exams.Question> Questions { get; set; }
    public DbSet<Assessment.Exams.ExamSection> ExamSections { get; set; }
    public DbSet<Assessment.Exams.ExamForm> ExamForms { get; set; }
    public DbSet<Assessment.Exams.ExamFormQuestion> ExamFormQuestions { get; set; }
    public DbSet<Assessment.Exams.ExamBlueprintRule> ExamBlueprintRules { get; set; }

    public DbSet<Assessment.People.Candidate> Candidates { get; set; }
    public DbSet<Assessment.People.CandidateGroup> CandidateGroups { get; set; }
    public DbSet<Assessment.People.CandidateGroupMember> CandidateGroupMembers { get; set; }

    public DbSet<Assessment.Delivery.Assignment> Assignments { get; set; }
    public DbSet<Assessment.Delivery.ExamLink> ExamLinks { get; set; }
    public DbSet<Assessment.Delivery.Attempt> Attempts { get; set; }
    public DbSet<Assessment.Delivery.AttemptQuestion> AttemptQuestions { get; set; }
    public DbSet<Assessment.Delivery.Answer> Answers { get; set; }
    public DbSet<Assessment.Delivery.IntegritySignal> IntegritySignals { get; set; }

    #endregion Assessment

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }

    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }

    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion Entities from the modules

    public InternshipManagementSystemDbContext(DbContextOptions<InternshipManagementSystemDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();
        ConfigureAssessment(builder);

        /* Configure your own tables/entities inside here */

        //builder.Entity<YourEntity>(b =>
        //{
        //    b.ToTable(InternshipManagementSystemConsts.DbTablePrefix + "YourEntities", InternshipManagementSystemConsts.DbSchema);
        //    b.ConfigureByConvention(); //auto configure for the base class props
        //    //...
        //});
    }
}