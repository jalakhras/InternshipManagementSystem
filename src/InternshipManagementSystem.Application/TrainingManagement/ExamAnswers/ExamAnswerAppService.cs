using InternshipManagementSystem.FileManagement;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement;
using InternshipManagementSystem.TrainingManagement.DTOs.ExamAnswers;
using InternshipManagementSystem.TrainingManagement.ExamAnswers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

[Authorize(InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Default)]
public class ExamAnswerAppService : CrudAppService<ExamAnswer, ExamAnswerDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateExamAnswerDto>, IExamAnswerAppService
{
    private readonly IFileUploadAppService _fileUploadAppService;

    public ExamAnswerAppService(
        IRepository<ExamAnswer, Guid> repository,
        IFileUploadAppService fileUploadAppService)
        : base(repository)
    {
        _fileUploadAppService = fileUploadAppService;

        GetPolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.View;
        GetListPolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.View;
        CreatePolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Create;
        UpdatePolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Edit;
        DeletePolicyName = InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Delete;
    }

    public override async Task<ExamAnswerDto> CreateAsync(CreateUpdateExamAnswerDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateExamAnswerDto, ExamAnswer>(input);

        if (input.AnswerFile != null)
        {
            var uploadResult = await _fileUploadAppService.UploadAsync(input.AnswerFile, "answers");
            entity.AnswerFileUrl = uploadResult.FilePath;
            entity.AnswerFileName = uploadResult.FileName;
        }

        await Repository.InsertAsync(entity);
        return ObjectMapper.Map<ExamAnswer, ExamAnswerDto>(entity);
    }

    public override async Task<ExamAnswerDto> UpdateAsync(Guid id, CreateUpdateExamAnswerDto input)
    {
        var entity = await Repository.GetAsync(id);

        if (!string.IsNullOrEmpty(entity.AnswerFileUrl) && input.AnswerFile != null)
        {
            await _fileUploadAppService.DeleteFileAsync(entity.AnswerFileUrl);
        }

        ObjectMapper.Map(input, entity);

        if (input.AnswerFile != null)
        {
            var uploadResult = await _fileUploadAppService.UploadAsync(input.AnswerFile, "answers");
            entity.AnswerFileUrl = uploadResult.FilePath;
            entity.AnswerFileName = uploadResult.FileName;
        }

        await Repository.UpdateAsync(entity);
        return ObjectMapper.Map<ExamAnswer, ExamAnswerDto>(entity);
    }
}
