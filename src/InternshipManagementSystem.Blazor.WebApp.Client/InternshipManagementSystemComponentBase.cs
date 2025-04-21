using InternshipManagementSystem.Localization;
using Volo.Abp.AspNetCore.Components;

namespace InternshipManagementSystem.Blazor.WebApp.Client;

public abstract class InternshipManagementSystemComponentBase : AbpComponentBase
{
    protected InternshipManagementSystemComponentBase()
    {
        LocalizationResource = typeof(InternshipManagementSystemResource);
    }
}
