using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement.Candidates.DTOs;
using Microsoft.AspNetCore.Authorization;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.TrainingManagement.Candidates;

[Authorize(InternshipManagementSystemPermissions.TrainingManagement.Candidates.Default)]
public class CandidateAppService : CrudAppService<
  Candidate,
    CandidateDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateCandidateDto,
    CreateUpdateCandidateDto>, ICandidateAppService
{
    public CandidateAppService(IRepository<Candidate, Guid> repository) : base(repository)
    {
    }
}
