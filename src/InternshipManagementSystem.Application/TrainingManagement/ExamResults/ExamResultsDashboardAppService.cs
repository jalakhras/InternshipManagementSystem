using InternshipManagementSystem.CandidateExamAttempts.DTOs;
using InternshipManagementSystem.TrainingManagement.ExamResults;
using InternshipManagementSystem.TrainingManagement.ExamResults.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.TrainingManagement.ExamResultsDashboard
{
    public class ExamResultsDashboardAppService : ApplicationService, IExamResultsDashboardAppService
    {
        private readonly IRepository<Exam, Guid> _examRepository;
        private readonly IRepository<CandidateExamAttempt, Guid> _attemptRepository;

        public ExamResultsDashboardAppService(
            IRepository<Exam, Guid> examRepository,
            IRepository<CandidateExamAttempt, Guid> attemptRepository)
        {
            _examRepository = examRepository;
            _attemptRepository = attemptRepository;
        }

        public async Task<ListResultDto<ExamResultSummaryDto>> GetExamResultsOverviewAsync()
        {
            var exams = await _examRepository.GetListAsync();
            var attempts = await _attemptRepository.GetListAsync();

            var summaries = exams.Select(exam =>
            {
                var examAttempts = attempts.Where(a => a.ExamId == exam.Id);
                var total = examAttempts.Count();
                var passed = examAttempts.Count(a => a.IsPassed);
                var failed = total - passed;

                return new ExamResultSummaryDto
                {
                    ExamId = exam.Id,
                    ExamTitle = exam.Title,
                    TotalAttempts = total,
                    PassedCount = passed,
                    FailedCount = failed
                };
            }).ToList();

            return new ListResultDto<ExamResultSummaryDto>(summaries);
        }

        public async Task<ListResultDto<CandidateExamAttemptResultDto>> GetResultsByExamAsync(Guid examId)
        {
            var attempts = await _attemptRepository.GetListAsync(x => x.ExamId == examId);
            var results = ObjectMapper.Map<List<CandidateExamAttempt>, List<CandidateExamAttemptResultDto>>(attempts);
            return new ListResultDto<CandidateExamAttemptResultDto>(results);
        }

        public async Task<ListResultDto<CandidateExamAttemptResultDto>> GetResultsByCandidateAsync(Guid candidateId)
        {
            var attempts = await _attemptRepository.GetListAsync(x => x.CandidateId == candidateId);
            var results = ObjectMapper.Map<List<CandidateExamAttempt>, List<CandidateExamAttemptResultDto>>(attempts);
            return new ListResultDto<CandidateExamAttemptResultDto>(results);
        }
    }
}