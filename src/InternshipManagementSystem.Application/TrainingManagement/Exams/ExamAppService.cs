using AutoMapper.Internal.Mappers;
using InternshipManagementSystem.TrainingManagement.DTOs.Exams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.TrainingManagement.Exams
{
    public class ExamAppService : CrudAppService<
        Exam,
        ExamDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateExamDto>,
        IExamAppService
    {
        public ExamAppService(IRepository<Exam, Guid> repository)
            : base(repository)
        {
        }
    }
}