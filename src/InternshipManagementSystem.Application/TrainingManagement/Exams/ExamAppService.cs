using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement.DTOs.Exams;
using Microsoft.AspNetCore.Authorization;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.TrainingManagement.Exams
{
    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.Exams.Default)]
    public class ExamAppService : CrudAppService<Exam, ExamDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateExamDto>, IExamAppService
    {
        public ExamAppService(IRepository<Exam, Guid> repository) : base(repository)
        {
            GetPolicyName = InternshipManagementSystemPermissions.TrainingManagement.Exams.View;
            GetListPolicyName = InternshipManagementSystemPermissions.TrainingManagement.Exams.View;
            CreatePolicyName = InternshipManagementSystemPermissions.TrainingManagement.Exams.Create;
            UpdatePolicyName = InternshipManagementSystemPermissions.TrainingManagement.Exams.Edit;
            DeletePolicyName = InternshipManagementSystemPermissions.TrainingManagement.Exams.Delete;
        }
    }

}