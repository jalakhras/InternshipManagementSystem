using InternshipManagementSystem.TrainingManagement.Grading.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using System.Linq;

namespace InternshipManagementSystem.TrainingManagement.Grading
{
    public class ReviewDashboardAppService : ApplicationService, IReviewDashboardAppService
    {
        private readonly IRepository<CandidateExamAttempt, Guid> _attemptRepository;

        public ReviewDashboardAppService(IRepository<CandidateExamAttempt, Guid> attemptRepository)
        {
            _attemptRepository = attemptRepository;
        }

        public async Task<ListResultDto<ManualReviewAttemptDto>> GetAttemptsNeedingManualReviewAsync()
        {
            var queryable = await _attemptRepository.GetQueryableAsync();

            var attempts = await queryable
                .Include(a => a.Candidate)
                .Include(a => a.Exam)
                .Where(a => a.IsSubmitted && a.NeedsManualReview)
                .ToListAsync();

            var result = attempts.Select(a => new ManualReviewAttemptDto
            {
                AttemptId = a.Id,
                CandidateName = a.Candidate.FullName,
                ExamTitle = a.Exam.Title,
                StartTime = a.StartTime,
                Score = a.Score,
                IsSubmitted = a.IsSubmitted
            }).ToList();

            return new ListResultDto<ManualReviewAttemptDto>(result);
        }
    }
}
