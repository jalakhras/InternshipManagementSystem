using Microsoft.Extensions.Localization;
using InternshipManagementSystem.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace InternshipManagementSystem.Blazor.WebApp.Client;

[Dependency(ReplaceServices = true)]
public class InternshipManagementSystemBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<InternshipManagementSystemResource> _localizer;

    public InternshipManagementSystemBrandingProvider(IStringLocalizer<InternshipManagementSystemResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
