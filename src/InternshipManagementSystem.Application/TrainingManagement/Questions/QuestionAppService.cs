using InternshipManagementSystem.FileManagement;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement.DTOs.Questions;
using InternshipManagementSystem.TrainingManagement.Questions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

[Authorize(InternshipManagementSystemPermissions.TrainingManagement.Questions.Default)]
public class QuestionAppService : CrudAppService<
    Question,
    QuestionDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateQuestionDto>, IQuestionAppService
{
    private readonly IFileUploadAppService _fileUploadAppService;

    public QuestionAppService(
        IRepository<Question, Guid> repository,
        IFileUploadAppService fileUploadAppService)
        : base(repository)
    {
        _fileUploadAppService = fileUploadAppService;
    }

    public override async Task<QuestionDto> CreateAsync(CreateUpdateQuestionDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateQuestionDto, Question>(input);

        if (input.MediaFile != null)
        {
            var uploadResult = await _fileUploadAppService.UploadAsync(input.MediaFile, "questions");
            entity.MediaUrl = uploadResult.FilePath; // ✅ نستخدم FilePath وليس Url
            entity.MediaFileName = uploadResult.FileName;
            entity.MediaType = input.MediaType;
        }

        await Repository.InsertAsync(entity);
        return ObjectMapper.Map<Question, QuestionDto>(entity);
    }

    public override async Task<QuestionDto> UpdateAsync(Guid id, CreateUpdateQuestionDto input)
    {
        var entity = await Repository.GetAsync(id);

        // أولاً حذف الملف القديم إن وجد
        if (!string.IsNullOrWhiteSpace(entity.MediaUrl) && input.MediaFile != null)
        {
            await _fileUploadAppService.DeleteFileAsync(entity.MediaUrl);
        }

        // تعديل خصائص الكيان
        ObjectMapper.Map(input, entity);

        // ثم رفع الملف الجديد إن وجد
        if (input.MediaFile != null)
        {
            var uploadResult = await _fileUploadAppService.UploadAsync(input.MediaFile, "questions");
            entity.MediaUrl = uploadResult.FilePath;
            entity.MediaFileName = uploadResult.FileName;
            entity.MediaType = input.MediaType;
        }

        await Repository.UpdateAsync(entity);
        return ObjectMapper.Map<Question, QuestionDto>(entity);
    }

    public override async Task DeleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (!string.IsNullOrWhiteSpace(entity.MediaUrl))
        {
            await _fileUploadAppService.DeleteFileAsync(entity.MediaUrl);
        }

        await base.DeleteAsync(id);
    }
}
