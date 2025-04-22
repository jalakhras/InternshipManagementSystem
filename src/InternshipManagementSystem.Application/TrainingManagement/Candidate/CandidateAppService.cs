using InternshipManagementSystem.Candidates.DTOs;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.Candidates
{
    [Authorize(InternshipManagementSystemPermissions.Candidates.Default)]
    public class CandidateAppService : CrudAppService<
        Candidate, // الكيان
        CandidateDto, // ما نعرضه
        Guid, // نوع المفتاح الأساسي
        PagedAndSortedResultRequestDto, // للـ List و Pagination
        CreateUpdateCandidateDto // للإنشاء والتحديث
        >, ICandidateAppService
    {
        public CandidateAppService(IRepository<Candidate, Guid> repository)
            : base(repository)
        {
        }

        [Authorize(InternshipManagementSystemPermissions.Candidates.Create)]
        public override Task<CandidateDto> CreateAsync(CreateUpdateCandidateDto input)
        {
            return base.CreateAsync(input);
        }

        [Authorize(InternshipManagementSystemPermissions.Candidates.Edit)]
        public override Task<CandidateDto> UpdateAsync(Guid id, CreateUpdateCandidateDto input)
        {
            return base.UpdateAsync(id, input);
        }

        [Authorize(InternshipManagementSystemPermissions.Candidates.Delete)]
        public override Task DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        [Authorize(InternshipManagementSystemPermissions.Candidates.View)]
        public override Task<PagedResultDto<CandidateDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return base.GetListAsync(input);
        }

        [Authorize(InternshipManagementSystemPermissions.Candidates.View)]
        public override Task<CandidateDto> GetAsync(Guid id)
        {
            return base.GetAsync(id);
        }
    }
}