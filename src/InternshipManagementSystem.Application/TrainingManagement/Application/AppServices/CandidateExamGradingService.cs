using InternshipManagementSystem.TrainingManagement.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace InternshipManagementSystem.TrainingManagement.Grading
{
    public class CandidateExamGradingService : ICandidateExamGradingService
    {
        private readonly IRepository<CandidateExamAttempt, Guid> _attemptRepository;
        private readonly IRepository<CandidateExamAnswer, Guid> _answerRepository;
        private readonly IRepository<Question, Guid> _questionRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ILogger<CandidateExamGradingService> _logger;

        public CandidateExamGradingService(
            IRepository<CandidateExamAttempt, Guid> attemptRepository,
            IRepository<CandidateExamAnswer, Guid> answerRepository,
            IRepository<Question, Guid> questionRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<CandidateExamGradingService> logger)
        {
            _attemptRepository = attemptRepository;
            _answerRepository = answerRepository;
            _questionRepository = questionRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _logger = logger;
        }

        public async Task EvaluateCandidateExamAttemptAsync(Guid attemptId)
        {
            using var uow = _unitOfWorkManager.Begin();

            var attempt = await _attemptRepository.GetAsync(attemptId);
            var answers = await _answerRepository.GetListAsync(a => a.CandidateExamAttemptId == attemptId);

            int totalScore = 0;

            foreach (var answer in answers)
            {
                var question = await _questionRepository.GetAsync(answer.QuestionId);
                int score = 0;

                if (question.Type == QuestionType.MultipleChoice || question.Type == QuestionType.TrueFalse)
                {
                    if (answer.Answer?.Trim().ToLower() == question.CorrectAnswer?.Trim().ToLower())
                    {
                        score = (int)question.Score;
                    }
                }
                else if (question.Type == QuestionType.Code && !string.IsNullOrWhiteSpace(question.CodeExpectedOutput))
                {
                    if (answer.Answer?.Trim() == question.CodeExpectedOutput.Trim())
                    {
                        score = (int)question.Score;
                    }
                }

                totalScore += score;
            }

            attempt.Score = totalScore;
            attempt.IsPassed = totalScore >= 60; // نسبة النجاح
            attempt.EndTime = DateTime.Now;

            await _attemptRepository.UpdateAsync(attempt);
            await uow.CompleteAsync();

            _logger.LogInformation($"تم تصحيح محاولة المرشح: {attempt.Id}، النتيجة = {attempt.Score}");
        }
    }
}
