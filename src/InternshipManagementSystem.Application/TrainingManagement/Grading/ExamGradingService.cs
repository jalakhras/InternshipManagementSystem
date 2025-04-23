using InternshipManagementSystem.TrainingManagement.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace InternshipManagementSystem.TrainingManagement.Grading
{
    public class ExamGradingService : IExamGradingService
    {
        private readonly IRepository<ExamAttempt, Guid> _attemptRepository;
        private readonly IRepository<ExamAnswer, Guid> _answerRepository;
        private readonly IRepository<Question, Guid> _questionRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ILogger<ExamGradingService> _logger;

        public ExamGradingService(
            IRepository<ExamAttempt, Guid> attemptRepository,
            IRepository<ExamAnswer, Guid> answerRepository,
            IRepository<Question, Guid> questionRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<ExamGradingService> logger)
        {
            _attemptRepository = attemptRepository;
            _answerRepository = answerRepository;
            _questionRepository = questionRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _logger = logger;
        }

        public async Task EvaluateExamAttemptAsync(Guid attemptId)
        {
            using var uow = _unitOfWorkManager.Begin();

            var attempt = await _attemptRepository.GetAsync(attemptId);
            var answers = await _answerRepository.GetListAsync(a => a.ExamAttemptId == attemptId);

            int totalScore = 0;
            bool needsManualReview = false;

            foreach (var answer in answers)
            {
                var question = await _questionRepository.GetAsync(answer.QuestionId);
                int score = 0;

                switch (question.Type)
                {
                    case QuestionType.MultipleChoice:
                    case QuestionType.TrueFalse:
                        if (answer.Answer?.Trim().ToLower() == question.CorrectAnswer?.Trim().ToLower())
                        {
                            score = (int)question.Score;
                        }
                        break;

                    case QuestionType.MultiSelect:
                        try
                        {
                            var correctOptions = question.CorrectAnswer?.Split(',').Select(o => o.Trim()).ToHashSet() ?? new HashSet<string>();
                            var userOptions = answer.Answer?.Split(',').Select(o => o.Trim()).ToHashSet() ?? new HashSet<string>();

                            int correctCount = correctOptions.Intersect(userOptions).Count();
                            int totalCorrect = correctOptions.Count;

                            if (totalCorrect > 0)
                            {
                                score = (int)Math.Round((question.Score * correctCount) / (double)totalCorrect);
                            }
                        }
                        catch
                        {
                            _logger.LogWarning($"فشل تقييم السؤال {question.Id} بسبب صيغة الإجابات.");
                        }
                        break;

                    case QuestionType.Code:
                        if (!string.IsNullOrWhiteSpace(question.CodeExpectedOutput) &&
                            answer.Answer?.Trim() == question.CodeExpectedOutput.Trim())
                        {
                            score = (int)question.Score;
                        }
                        break;

                    case QuestionType.Text:
                    case QuestionType.FileUpload:
                        needsManualReview = true;
                        break;
                }

                totalScore += score;
            }

            attempt.Score = totalScore;
            attempt.IsPassed = totalScore >= 60;
            attempt.IsGraded = true;
            attempt.NeedsManualReview = needsManualReview;
            attempt.EndTime = DateTime.Now;

            await _attemptRepository.UpdateAsync(attempt);
            await uow.CompleteAsync();

            _logger.LogInformation($" تم تقييم المحاولة {attempt.Id}: النتيجة = {attempt.Score}, تصحيح يدوي = {needsManualReview}");
        }
    }
}