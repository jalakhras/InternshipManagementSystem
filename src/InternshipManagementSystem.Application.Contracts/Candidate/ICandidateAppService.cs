using InternshipManagementSystem.Candidates.DTOs;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Candidates
{
    public interface ICandidateAppService :
        ICrudAppService<
            CandidateDto, // ما يتم عرضه
            Guid, // نوع المفتاح الأساسي
            PagedAndSortedResultRequestDto, // لعمليات التقسيم والترتيب
            CreateUpdateCandidateDto, // لإنشاء سجل جديد
            CreateUpdateCandidateDto // لتحديث سجل موجود
        >
    {
        // يمكنك إضافة دوال إضافية هنا لو أردت لاحقاً مثل: بحث متقدم، فلترة...
    }
}