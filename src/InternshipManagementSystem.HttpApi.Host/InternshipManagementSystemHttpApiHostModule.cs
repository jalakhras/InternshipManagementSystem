using InternshipManagementSystem.EntityFrameworkCore;
using InternshipManagementSystem.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;

namespace InternshipManagementSystem;

[DependsOn(
    typeof(InternshipManagementSystemHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(InternshipManagementSystemApplicationModule),
    typeof(InternshipManagementSystemEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule)
)]
public class InternshipManagementSystemHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("InternshipManagementSystem");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        ConfigureOpenIddictCertificates(configuration);
    }

    /// <summary>
    /// Swaps ABP's development signing and encryption certificates for real ones when
    /// a deployment supplies them.
    /// <para>
    /// Development certificates are generated into the user's certificate store on
    /// first run. In a container that store lives in the writable layer and is gone
    /// the moment the container is replaced — so every access token, refresh token
    /// and authorization code issued before a restart stops validating after it, and
    /// every signed-in user is logged out by a routine redeploy. Worse, two replicas
    /// behind a load balancer each generate their own, so a token minted by one is
    /// rejected by the other.
    /// </para>
    /// <para>
    /// The path stays empty by default, which leaves the development certificates in
    /// place — that is what local development and a from-clean-clone <c>compose up</c>
    /// want, and neither needs tokens to survive a restart. Anything longer-lived sets
    /// <c>OpenIddict:Certificate:Path</c>.
    /// </para>
    /// </summary>
    private void ConfigureOpenIddictCertificates(IConfiguration configuration)
    {
        var certificatePath = configuration["OpenIddict:Certificate:Path"];

        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            return;
        }

        PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
        {
            options.AddDevelopmentEncryptionAndSigningCertificate = false;
        });

        PreConfigure<OpenIddictServerBuilder>(builder =>
        {
            builder.AddProductionEncryptionAndSigningCertificate(
                certificatePath,
                configuration["OpenIddict:Certificate:PassPhrase"] ?? string.Empty);

            // Behind a proxy the host only ever sees the internal address, so
            // discovery would advertise http://api:8080 and every client would then
            // reject the issuer in the tokens it was just handed.
            var authority = configuration["AuthServer:Authority"];
            if (!string.IsNullOrWhiteSpace(authority))
            {
                builder.SetIssuer(new Uri(authority.EnsureEndsWith('/')));
            }
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        ConfigureAuthentication(context);
        ConfigureBundles();
        ConfigureUrls(configuration);
        ConfigureConventionalControllers();
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);
        ConfigureDataProtection(context, configuration);
        ConfigureForwardedHeaders(context, configuration);

        // Something an orchestrator can poll that does not require a token and does
        // not touch the database. Compose gates the SPA on it; a scheduler uses it to
        // decide whether a replica is taking traffic.
        context.Services.AddHealthChecks();

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(InternshipManagementSystemApplicationModule).Assembly);
        });
    }

    /// <summary>
    /// Pins the data-protection key ring to a durable location when one is configured.
    /// <para>
    /// The framework default is a directory under the user profile. A container has
    /// no persistent profile, so the keys are regenerated on every start and anything
    /// they protect — most visibly the anti-forgery tokens on the login page — breaks
    /// across a restart. Two replicas each get their own ring, and a request that
    /// lands on the wrong one fails.
    /// </para>
    /// </summary>
    private void ConfigureDataProtection(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var builder = context.Services
            .AddDataProtection()
            .SetApplicationName(
                configuration["DataProtection:ApplicationName"] ?? "InternshipManagementSystem");

        var keysPath = configuration["DataProtection:KeysPath"];

        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            Directory.CreateDirectory(keysPath);
            builder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
    }

    /// <summary>
    /// Teaches the host to believe the proxy in front of it about scheme and client
    /// address, when a deployment says there is one.
    /// <para>
    /// Off by default, because trusting these headers from an arbitrary caller lets
    /// that caller forge the client IP recorded in the audit log and claim a plain
    /// HTTP request arrived over TLS.
    /// </para>
    /// </summary>
    private void ConfigureForwardedHeaders(ServiceConfigurationContext context, IConfiguration configuration)
    {
        if (!configuration.GetValue("ForwardedHeaders:Enabled", false))
        {
            return;
        }

        context.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // The defaults only trust a loopback proxy. In a container network the
            // proxy is a peer on a bridge, so the deployment has to name it — or,
            // where the network itself is the trust boundary, clear both lists.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var network in SplitList(configuration["ForwardedHeaders:KnownNetworks"]))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }

            foreach (var proxy in SplitList(configuration["ForwardedHeaders:KnownProxies"]))
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });
    }

    private static string[] SplitList(string? value) =>
        value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? Array.Empty<string>();

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(SplitList(configuration["App:RedirectAllowedUrls"]));

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<InternshipManagementSystemDomainSharedModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}InternshipManagementSystem.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<InternshipManagementSystemDomainModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}InternshipManagementSystem.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<InternshipManagementSystemApplicationContractsModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}InternshipManagementSystem.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<InternshipManagementSystemApplicationModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}InternshipManagementSystem.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(InternshipManagementSystemApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOAuth(
            configuration["AuthServer:Authority"]!,
            new Dictionary<string, string>
            {
                    {"InternshipManagementSystem", "InternshipManagementSystem API"}
            },
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "InternshipManagementSystem API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    // Trimmed as well as split: this list is written by hand into an
                    // environment variable at deployment time, and " http://app" is
                    // not an origin any browser will ever send.
                    .WithOrigins(SplitList(configuration["App:CorsOrigins"])
                        .Select(o => o.RemovePostFix("/"))
                        .ToArray())
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();
        var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();

        // First in the pipeline, before anything reads the scheme or the client
        // address — the correlation id and the audit log both do.
        if (configuration.GetValue("ForwardedHeaders:Enabled", false))
        {
            app.UseForwardedHeaders();
        }

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();
        app.MapAbpStaticAssets();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }
        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "InternshipManagementSystem API");

            c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            c.OAuthScopes("InternshipManagementSystem");
        });

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints(endpoints =>
        {
            // Anonymous and cheap on purpose. A readiness probe that needs a token is
            // a probe nobody wires up, and one that queries the database turns a slow
            // query into a restart loop.
            endpoints.MapHealthChecks("/health").AllowAnonymous();
        });
    }
}