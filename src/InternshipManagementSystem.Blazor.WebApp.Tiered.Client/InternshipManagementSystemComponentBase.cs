using InternshipManagementSystem.Localization;
using Volo.Abp.AspNetCore.Components;

namespace InternshipManagementSystem.Blazor.WebApp.Tiered.Client;

public abstract class InternshipManagementSystemComponentBase : AbpComponentBase
{
    protected InternshipManagementSystemComponentBase()
    {
        LocalizationResource = typeof(InternshipManagementSystemResource);
    }
}
