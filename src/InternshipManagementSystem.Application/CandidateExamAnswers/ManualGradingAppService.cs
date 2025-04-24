using InternshipManagementSystem.CandidateExamAnswers;
using InternshipManagementSystem.CandidateExamAnswers.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.TrainingManagement.ManualGrading
{
    public class ManualGradingAppService : ApplicationService, IManualGradingAppService
    {
        private readonly IRepository<CandidateExamAnswer, Guid> _answerRepository;

        public ManualGradingAppService(IRepository<CandidateExamAnswer, Guid> answerRepository)
        {
            _answerRepository = answerRepository;
        }

        public async Task<ListResultDto<CandidateExamAnswerForReviewDto>> GetAnswersForManualReviewAsync(Guid attemptId)
        {
            var queryable = await _answerRepository.GetQueryableAsync();

            var answers = await queryable
                .Where(a => a.CandidateExamAttemptId == attemptId)
                .Include(a => a.Question)
                .ToListAsync();

            var result = answers.Select(a => new CandidateExamAnswerForReviewDto
            {
                Id = a.Id,
                QuestionText = a.Question.Text,
                Answer = a.Answer,
                AnswerFileUrl = a.AnswerFileUrl,
                AnswerFileName = a.AnswerFileName,
                PartialScore = a.PartialScore,
                ReviewComments = a.ReviewComments
            }).ToList();

            return new ListResultDto<CandidateExamAnswerForReviewDto>(result);
        }

        public async Task ReviewAnswerAsync(Guid answerId, ManualGradingInputDto input)
        {
            var answer = await _answerRepository.GetAsync(answerId);

            answer.PartialScore = input.PartialScore;
            answer.ReviewComments = input.ReviewComments;
            answer.IsCorrect = input.PartialScore > 0;

            await _answerRepository.UpdateAsync(answer);
        }
    }
}