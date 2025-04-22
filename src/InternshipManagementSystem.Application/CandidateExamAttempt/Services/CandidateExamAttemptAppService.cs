using InternshipManagementSystem.Candidate;
using InternshipManagementSystem.CandidateExamAttempts.DTOs;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement.CandidateExamAttempts.DTOs;
using Microsoft.AspNetCore.Authorization;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.TrainingManagement.CandidateExamAttempts;

[Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Default)]
public class CandidateExamAttemptAppService : CrudAppService<
    CandidateExamAttempt,
    CandidateExamAttemptDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateCandidateExamAttemptDto,
    CreateUpdateCandidateExamAttemptDto>, ICandidateExamAttemptAppService
{
    public CandidateExamAttemptAppService(IRepository<CandidateExamAttempt, Guid> repository) : base(repository)
    {
    }
}
