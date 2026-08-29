using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Settings;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// What an organisation changes about the platform for itself.
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/settings")]
public class TenantSettingsController : AbpControllerBase
{
    private readonly ITenantSettingsAppService _settings;

    public TenantSettingsController(ITenantSettingsAppService settings)
    {
        _settings = settings;
    }

    [HttpGet]
    public Task<TenantSettingsDto> GetAsync() => _settings.GetAsync();

    [HttpPut]
    public Task<TenantSettingsDto> UpdateAsync([FromBody] TenantSettingsDto input) =>
        _settings.UpdateAsync(input);
}
