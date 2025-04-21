using System;
using System.Collections.Generic;
using System.Text;
using InternshipManagementSystem.Localization;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem;

/* Inherit your application services from this class.
 */
public abstract class InternshipManagementSystemAppService : ApplicationService
{
    protected InternshipManagementSystemAppService()
    {
        LocalizationResource = typeof(InternshipManagementSystemResource);
    }
}
