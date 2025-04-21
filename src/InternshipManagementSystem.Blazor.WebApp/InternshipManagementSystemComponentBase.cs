using InternshipManagementSystem.Localization;
using Volo.Abp.AspNetCore.Components;

namespace InternshipManagementSystem.Blazor.WebApp;

public abstract class InternshipManagementSystemComponentBase : AbpComponentBase
{
    protected InternshipManagementSystemComponentBase()
    {
        LocalizationResource = typeof(InternshipManagementSystemResource);
    }
}
