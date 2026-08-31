using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Grading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Emailing;
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
        ConfigureEmailTransport(context);
    }

    /// <summary>
    /// Which road an invitation travels.
    /// <para>
    /// SMTP unless an HTTP key is configured, and then HTTP. The choice is made
    /// by which credential exists rather than by a switch, because a deployment
    /// that has put an API key in its secrets has already said what it wants and
    /// should not have to say it twice.
    /// </para>
    /// <para>
    /// This exists because SMTP is not always reachable. On the network this was
    /// first configured for, a connection to smtp.gmail.com on 587 and on 465 is
    /// accepted and then never answered, and 25 is refused; 443 works. Blocking
    /// outbound SMTP is ordinary practice for consumer and corporate networks in
    /// the region this product is for — and a customer who cannot send
    /// invitations cannot use it at all, because the link inside that message is
    /// the candidate's entire credential.
    /// </para>
    /// <para>
    /// Registered here rather than in the domain module, which has no business
    /// making HTTP calls, and after it, so this replaces what it chose.
    /// </para>
    /// </summary>
    private void ConfigureEmailTransport(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<ResendOptions>(configuration.GetSection("Mailing:Resend"));

        var apiKey = configuration["Mailing:Resend:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        context.Services.AddHttpClient(nameof(ResendEmailSender));

        context.Services.Replace(
            ServiceDescriptor.Transient<IEmailSender, ResendEmailSender>());
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
