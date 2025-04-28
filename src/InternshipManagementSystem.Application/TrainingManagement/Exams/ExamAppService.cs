using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement.DTOs.Exams;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp;
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

        public override async Task<ExamDto> CreateAsync(CreateUpdateExamDto input)
        {
            ValidateExamScheduling(input);
            return await base.CreateAsync(input);
        }

        public override async Task<ExamDto> UpdateAsync(Guid id, CreateUpdateExamDto input)
        {
            ValidateExamScheduling(input);
            return await base.UpdateAsync(id, input);
        }

        private void ValidateExamScheduling(CreateUpdateExamDto input)
        {
            if (input.IsScheduled)
            {
                if (!input.ScheduledStartTime.HasValue || !input.ScheduledEndTime.HasValue)
                {
                    throw new BusinessException("Scheduled times must be provided when IsScheduled is true.");
                }
                if (input.ScheduledStartTime >= input.ScheduledEndTime)
                {
                    throw new BusinessException("Scheduled start time must be before scheduled end time.");
                }
            }
        }

    }
}