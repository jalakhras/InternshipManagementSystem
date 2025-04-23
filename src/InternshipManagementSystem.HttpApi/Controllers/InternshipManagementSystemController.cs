using InternshipManagementSystem.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers;

/* Inherit your controllers from this class.
 */

public abstract class InternshipManagementSystemController : AbpControllerBase
{
    protected InternshipManagementSystemController()
    {
        LocalizationResource = typeof(InternshipManagementSystemResource);
    }
}