using InternshipManagementSystem.CandidateExamAttempts.DTOs;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement;
using InternshipManagementSystem.TrainingManagement.Grading;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.CandidateExamAttempts
{
    
    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Default)]
    public class CandidateExamAttemptAppService : ApplicationService, ICandidateExamAttemptAppService
    {
        private readonly IRepository<CandidateExamAttempt, Guid> _candidateExamAttemptRepository;
        private readonly IRepository<Candidate, Guid> _candidateRepository;
        private readonly IRepository<Exam, Guid> _examRepository;
        private readonly IRepository<Question, Guid> _questionRepository;
        private readonly ICandidateExamGradingService _gradingService;

        public CandidateExamAttemptAppService(
            IRepository<CandidateExamAttempt, Guid> candidateExamAttemptRepository,
            IRepository<Candidate, Guid> candidateRepository,
            IRepository<Exam, Guid> examRepository,
            IRepository<Question, Guid> questionRepository,
            ICandidateExamGradingService gradingService)
        {
            _candidateExamAttemptRepository = candidateExamAttemptRepository;
            _candidateRepository = candidateRepository;
            _examRepository = examRepository;
            _questionRepository = questionRepository;
            _gradingService = gradingService;
        }

        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Create)]
        public async Task<CandidateExamAttemptDto> CreateCandidateExamAttemptAsync(CreateCandidateExamAttemptDto input)
        {
            var exam = await _examRepository.GetAsync(input.ExamId);
            var candidate = await _candidateRepository.GetAsync(input.CandidateId);

            if (exam.IsScheduled)
            {
                var now = Clock.Now;
                if (now < exam.ScheduledStartTime || now > exam.ScheduledEndTime)
                {
                    throw new BusinessException("The exam is not available at this time.");
                }
            }

            var attempt = new CandidateExamAttempt(GuidGenerator.Create())
            {
                CandidateId = input.CandidateId,
                ExamId = input.ExamId,
                StartTime = Clock.Now,
                IsSubmitted = false
            };

            await _candidateExamAttemptRepository.InsertAsync(attempt);

            return ObjectMapper.Map<CandidateExamAttempt, CandidateExamAttemptDto>(attempt);
        }


        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Edit)]
        public async Task SubmitCandidateExamAttemptAsync(SubmitCandidateExamAttemptDto input)
        {
            var attempt = await _candidateExamAttemptRepository.GetAsync(input.AttemptId);

            if (attempt.IsSubmitted)
            {
                throw new BusinessException("CandidateExamAttemptAlreadySubmitted");
            }

            attempt.EndTime = Clock.Now;
            attempt.IsSubmitted = true;
            attempt.Score = input.Score;

            await _candidateExamAttemptRepository.UpdateAsync(attempt);
            await _gradingService.EvaluateCandidateExamAttemptAsync(input.AttemptId);
        }

        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.View)]
        public async Task<CandidateExamAttemptDto> GetAttemptForCandidateAsync(Guid candidateId, Guid examId)
        {
            var attempt = await _candidateExamAttemptRepository.FirstOrDefaultAsync(
                x => x.CandidateId == candidateId && x.ExamId == examId && !x.IsSubmitted);

            if (attempt == null)
            {
                throw new BusinessException("NoActiveAttemptFound");
            }

            return ObjectMapper.Map<CandidateExamAttempt, CandidateExamAttemptDto>(attempt);
        }

        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.View)]
        public async Task<CandidateExamAttemptResultDto> GetAttemptResultAsync(Guid attemptId)
        {
            var attempt = await _candidateExamAttemptRepository.GetAsync(attemptId);

            if (!attempt.IsSubmitted)
            {
                throw new BusinessException("AttemptNotSubmittedYet");
            }

            return new CandidateExamAttemptResultDto
            {
                AttemptId = attempt.Id,
                Score = attempt.Score,
                Passed = attempt.Score >= 60
            };
        }

        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.View)]
        public async Task<PagedResultDto<CandidateExamAttemptDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            var queryable = await _candidateExamAttemptRepository.GetQueryableAsync();

            var totalCount = await AsyncExecuter.CountAsync(queryable);
            var items = await AsyncExecuter.ToListAsync(queryable
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
            );

            return new PagedResultDto<CandidateExamAttemptDto>(
                totalCount,
                ObjectMapper.Map<List<CandidateExamAttempt>, List<CandidateExamAttemptDto>>(items)
            );
        }

    }
}
