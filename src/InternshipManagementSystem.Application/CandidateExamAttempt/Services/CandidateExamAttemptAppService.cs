using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.CandidateExamAttempts.DTOs;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using InternshipManagementSystem.Candidate;
using InternshipManagementSystem.TrainingManagement.CandidateExamAttempts;
using Volo.Abp;

namespace InternshipManagementSystem.CandidateExamAttempts
{
    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Default)]
    public class CandidateExamAttemptAppService : ApplicationService, ICandidateExamAttemptAppService
    {
        private readonly IRepository<CandidateExamAttempt, Guid> _candidateExamAttemptRepository;
        private readonly IRepository<Candidate.Candidate, Guid> _candidateRepository;
        private readonly IRepository<Exam, Guid> _examRepository;
        private readonly IRepository<Question, Guid> _questionRepository;

        public CandidateExamAttemptAppService(
            IRepository<CandidateExamAttempt, Guid> candidateExamAttemptRepository,
            IRepository<Candidate.Candidate, Guid> candidateRepository,
            IRepository<Exam, Guid> examRepository,
            IRepository<Question, Guid> questionRepository)
        {
            _candidateExamAttemptRepository = candidateExamAttemptRepository;
            _candidateRepository = candidateRepository;
            _examRepository = examRepository;
            _questionRepository = questionRepository;
        }

        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Create)]
        public async Task<CandidateExamAttemptDto> CreateCandidateExamAttemptAsync(CreateCandidateExamAttemptDto input)
        {
            var exam = await _examRepository.GetAsync(input.ExamId);
            var candidate = await _candidateRepository.GetAsync(input.CandidateId);

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
                Passed = attempt.Score >= 60 // مثلا نعتبر النجاح فوق 60%
            };
        }
    }
}
