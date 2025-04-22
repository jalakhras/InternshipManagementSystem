using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement;
using InternshipManagementSystem.TrainingManagement.DTOs.ExamAnswers;
using InternshipManagementSystem.TrainingManagement.ExamAnswers;
using Microsoft.AspNetCore.Authorization;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

[Authorize(InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Default)]
public class ExamAnswerAppService : CrudAppService<ExamAnswer, ExamAnswerDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateExamAnswerDto>, IExamAnswerAppService
{
    public ExamAnswerAppService(IRepository<ExamAnswer, Guid> repository) : base(repository)
    {
        GetPolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.View;
        GetListPolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.View;
        CreatePolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Create;
        UpdatePolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Edit;
        DeletePolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Delete;
    }
}
