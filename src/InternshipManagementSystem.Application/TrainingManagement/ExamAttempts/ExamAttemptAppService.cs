using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement;
using InternshipManagementSystem.TrainingManagement.DTOs.ExamAttempts;
using InternshipManagementSystem.TrainingManagement.ExamAttempts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

[Authorize(InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.Default)]
public class ExamAttemptAppService : CrudAppService<ExamAttempt, ExamAttemptDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateExamAttemptDto>, IExamAttemptAppService
{
    public ExamAttemptAppService(IRepository<ExamAttempt, Guid> repository) : base(repository)
    {
        GetPolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.View;
        GetListPolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.View;
        CreatePolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.Create;
        UpdatePolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.Edit;
        DeletePolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.Delete;
    }

    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.View)]
    public async Task<List<ExamAttemptDto>> GetAttemptsByTraineeIdAsync(Guid traineeId)
    {
        var queryable = await Repository.GetQueryableAsync();
        var attempts = await queryable
            .Where(x => x.TraineeId == traineeId)
            .ToListAsync(); 

        return ObjectMapper.Map<List<ExamAttempt>, List<ExamAttemptDto>>(attempts);
    }

    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.View)]
    public async Task<List<ExamAttemptDto>> GetAttemptsByExamIdAsync(Guid examId)
    {
        var queryable = await Repository.GetQueryableAsync();
        var attempts = await queryable
            .Where(x => x.ExamId == examId)
            .ToListAsync();

        return ObjectMapper.Map<List<ExamAttempt>, List<ExamAttemptDto>>(attempts);
    }
}
