using InternshipManagementSystem.FileManagement;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement;
using InternshipManagementSystem.TrainingManagement.CandidateExamAnswers;
using InternshipManagementSystem.TrainingManagement.DTOs.CandidateExamAnswers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

[Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Default)]
public class CandidateExamAnswerAppService : CrudAppService<
    CandidateExamAnswer,
    CandidateExamAnswerDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateCandidateExamAnswerDto>, ICandidateExamAnswerAppService
{
    private readonly IFileUploadAppService _fileUploadAppService;

    public CandidateExamAnswerAppService(
        IRepository<CandidateExamAnswer, Guid> repository,
        IFileUploadAppService fileUploadAppService)
        : base(repository)
    {
        _fileUploadAppService = fileUploadAppService;
    }

    public override async Task<CandidateExamAnswerDto> CreateAsync(CreateUpdateCandidateExamAnswerDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateCandidateExamAnswerDto, CandidateExamAnswer>(input);

        if (input.AnswerFile != null)
        {
            var uploadResult = await _fileUploadAppService.UploadAsync(input.AnswerFile, "candidate-answers");
            entity.AnswerFileUrl = uploadResult.FilePath;
            entity.AnswerFileName = uploadResult.FileName;
        }

        await Repository.InsertAsync(entity);
        return ObjectMapper.Map<CandidateExamAnswer, CandidateExamAnswerDto>(entity);
    }

    public override async Task<CandidateExamAnswerDto> UpdateAsync(Guid id, CreateUpdateCandidateExamAnswerDto input)
    {
        var entity = await Repository.GetAsync(id);

        if (!string.IsNullOrWhiteSpace(entity.AnswerFileUrl) && input.AnswerFile != null)
        {
            await _fileUploadAppService.DeleteFileAsync(entity.AnswerFileUrl);
        }

        ObjectMapper.Map(input, entity);

        if (input.AnswerFile != null)
        {
            var uploadResult = await _fileUploadAppService.UploadAsync(input.AnswerFile, "candidate-answers");
            entity.AnswerFileUrl = uploadResult.FilePath;
            entity.AnswerFileName = uploadResult.FileName;
        }

        await Repository.UpdateAsync(entity);
        return ObjectMapper.Map<CandidateExamAnswer, CandidateExamAnswerDto>(entity);
    }
}