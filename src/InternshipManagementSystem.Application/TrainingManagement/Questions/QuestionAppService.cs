using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement;
using InternshipManagementSystem.TrainingManagement.DTOs.Questions;
using InternshipManagementSystem.TrainingManagement.Questions;
using Microsoft.AspNetCore.Authorization;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

[Authorize(InternshipManagementSystemPermissions.TrainingManagement.Questions.Default)]
public class QuestionAppService : CrudAppService<Question, QuestionDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateQuestionDto>, IQuestionAppService
{
    public QuestionAppService(IRepository<Question, Guid> repository) : base(repository)
    {
        GetPolicyName = InternshipManagementSystemPermissions.TrainingManagement.Questions.View;
        GetListPolicyName = InternshipManagementSystemPermissions.TrainingManagement.Questions.View;
        CreatePolicyName = InternshipManagementSystemPermissions.TrainingManagement.Questions.Create;
        UpdatePolicyName = InternshipManagementSystemPermissions.TrainingManagement.Questions.Edit;
        DeletePolicyName = InternshipManagementSystemPermissions.TrainingManagement.Questions.Delete;
    }
}
