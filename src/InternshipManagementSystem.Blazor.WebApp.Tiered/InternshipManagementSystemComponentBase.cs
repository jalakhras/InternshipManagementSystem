using InternshipManagementSystem.Localization;
using Volo.Abp.AspNetCore.Components;

namespace InternshipManagementSystem.Blazor.WebApp.Tiered;

public abstract class InternshipManagementSystemComponentBase : AbpComponentBase
{
    protected InternshipManagementSystemComponentBase()
    {
        LocalizationResource = typeof(InternshipManagementSystemResource);
    }
}
