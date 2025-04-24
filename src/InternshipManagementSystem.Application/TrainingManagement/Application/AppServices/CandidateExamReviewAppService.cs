using InternshipManagementSystem.TrainingManagement;
using InternshipManagementSystem.TrainingManagement.DTOs.CandidateExamAnswers;
using InternshipManagementSystem.TrainingManagement.Grading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.CandidateExamAttempts.Grading
{
    public class CandidateExamReviewAppService : ApplicationService , ICandidateExamReviewAppService
    {
        private readonly IRepository<CandidateExamAttempt, Guid> _attemptRepository;
        private readonly IRepository<CandidateExamAnswer, Guid> _answerRepository;

        public CandidateExamReviewAppService(
            IRepository<CandidateExamAttempt, Guid> attemptRepository,
            IRepository<CandidateExamAnswer, Guid> answerRepository)
        {
            _attemptRepository = attemptRepository;
            _answerRepository = answerRepository;
        }

        public async Task<List<CandidateExamAnswerDto>> GetAnswersForManualReviewAsync(Guid attemptId)
        {
            var attempt = await _attemptRepository.GetAsync(attemptId);
            if (!attempt.IsSubmitted)
            {
                throw new BusinessException("AttemptNotSubmittedYet");
            }

            var answers = await _answerRepository.GetListAsync(x => x.CandidateExamAttemptId == attemptId);
            return ObjectMapper.Map<List<CandidateExamAnswer>, List<CandidateExamAnswerDto>>(answers);
        }

        public async Task ReviewAnswerAsync(Guid answerId, int partialScore, string? comment)
        {
            var answer = await _answerRepository.GetAsync(answerId);

            answer.PartialScore = partialScore;
            answer.ReviewComments = comment;

            await _answerRepository.UpdateAsync(answer);
        }
    }
}