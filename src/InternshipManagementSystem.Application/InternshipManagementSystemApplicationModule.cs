using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Grading;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace InternshipManagementSystem;

[DependsOn(
    typeof(InternshipManagementSystemDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(InternshipManagementSystemApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpBackgroundWorkersModule)
    )]
public class InternshipManagementSystemApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        RegisterQuestionGraders(context.Services);
    }

    /// <summary>
    /// Registers every <see cref="IQuestionGrader"/> in this assembly against the
    /// interface, so <c>GraderResolver</c> can enumerate them.
    /// <para>
    /// ABP's conventional registration does find these classes — they inherit
    /// <c>ITransientDependency</c> through the interface — but it exposes a class
    /// only under interfaces whose name matches it, looking for
    /// <c>ISingleChoiceGrader</c> rather than <c>IQuestionGrader</c>. So each grader
    /// was registered only as itself, resolving <c>IEnumerable&lt;IQuestionGrader&gt;</c>
    /// returned nothing, and every question of every type was quietly routed to
    /// manual review. An integration test asking the type catalogue whether
    /// single-choice is auto-graded is what surfaced it.
    /// </para>
    /// <para>
    /// Scanned rather than listed so the promise still holds: adding a question
    /// type is one class, with nothing to remember here.
    /// </para>
    /// </summary>
    private static void RegisterQuestionGraders(IServiceCollection services)
    {
        var graders = typeof(InternshipManagementSystemApplicationModule).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => typeof(IQuestionGrader).IsAssignableFrom(type));

        foreach (var grader in graders)
        {
            services.AddTransient(typeof(IQuestionGrader), grader);
        }
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        // Closes out attempts whose deadline passed while nobody was watching: a
        // closed laptop must not leave an attempt ungraded with its link consumed.
        await context.AddBackgroundWorkerAsync<AttemptTimeoutWorker>();
    }
}
