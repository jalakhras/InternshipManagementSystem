using InternshipManagementSystem.TrainingManagement.DTOs.ExamAnswers;
using InternshipManagementSystem.TrainingManagement.ExamAnswers;
using InternshipManagementSystem.TrainingManagement;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

public class ExamAnswerAppService : CrudAppService<
    ExamAnswer,
    ExamAnswerDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateExamAnswerDto>,
    IExamAnswerAppService
{
    public ExamAnswerAppService(IRepository<ExamAnswer, Guid> repository)
        : base(repository)
    {
    }
}