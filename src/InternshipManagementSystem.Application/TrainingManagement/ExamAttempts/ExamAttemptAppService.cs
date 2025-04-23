using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement;
using InternshipManagementSystem.TrainingManagement.DTOs.ExamAttempts;
using InternshipManagementSystem.TrainingManagement.ExamAttempts;
using InternshipManagementSystem.TrainingManagement.Grading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

[Authorize(InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.Default)]
public class ExamAttemptAppService : CrudAppService<ExamAttempt, ExamAttemptDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateExamAttemptDto>, IExamAttemptAppService
{
    private readonly IExamGradingService _gradingService;

    public ExamAttemptAppService(
       IRepository<ExamAttempt, Guid> repository,
       IExamGradingService gradingService)
       : base(repository)
    {
        _gradingService = gradingService;

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

    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.Edit)]
    public async Task SubmitAttemptAsync(Guid attemptId)
    {
        var attempt = await Repository.GetAsync(attemptId);

        if (attempt.IsSubmitted)
        {
            throw new BusinessException("تم إرسال هذه المحاولة مسبقًا.");
        }

        attempt.IsSubmitted = true;
        attempt.EndTime = DateTime.Now;

        await Repository.UpdateAsync(attempt);
        await _gradingService.EvaluateExamAttemptAsync(attemptId);
    }
}
