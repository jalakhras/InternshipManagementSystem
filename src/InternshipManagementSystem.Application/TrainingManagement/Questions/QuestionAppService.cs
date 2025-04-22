using InternshipManagementSystem.TrainingManagement.DTOs.Questions;
using InternshipManagementSystem.TrainingManagement;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using InternshipManagementSystem.TrainingManagement.Questions;

public class QuestionAppService : CrudAppService<
    Question,
    QuestionDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateQuestionDto>,
    IQuestionAppService
{
    public QuestionAppService(IRepository<Question, Guid> repository)
        : base(repository)
    {
    }
}