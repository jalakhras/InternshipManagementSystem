using InternshipManagementSystem.TrainingManagement.ExamLinks.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.TrainingManagement.ExamLinks
{
    public class ExamLinkAppService : ApplicationService, IExamLinkAppService
    {
        private readonly IRepository<ExamLink, Guid> _examLinkRepository;
        private readonly IRepository<CandidateExamAttempt, Guid> _attemptRepository;
        private readonly ILogger<ExamLinkAppService> _logger;

        public ExamLinkAppService(
            IRepository<ExamLink, Guid> examLinkRepository,
            IRepository<CandidateExamAttempt, Guid> attemptRepository,
            ILogger<ExamLinkAppService> logger)
        {
            _examLinkRepository = examLinkRepository;
            _attemptRepository = attemptRepository;
            _logger = logger;
        }

        public async Task<ExamLinkDto> GenerateExamLinkAsync(CreateExamLinkInput input)
        {
            var token = Guid.NewGuid().ToString("N");

            var link = new ExamLink(GuidGenerator.Create())
            {
                CandidateId = input.CandidateId,
                ExamId = input.ExamId,
                ExpiryDate = input.ExpiryDate,
                MaxAttempts = input.MaxAttempts,
                CurrentAttempts = 0,
                SecureToken = token
            };

            await _examLinkRepository.InsertAsync(link);

            return new ExamLinkDto
            {
                CandidateId = link.CandidateId,
                ExamId = link.ExamId,
                ExpiryDate = link.ExpiryDate,
                MaxAttempts = link.MaxAttempts,
                CurrentAttempts = link.CurrentAttempts,
                SecureLink = $"/exam/access/{token}"
            };
        }

        public async Task<List<ExamLinkDto>> GetLinksForCandidateAsync(Guid candidateId)
        {
            var links = await _examLinkRepository.GetListAsync(x => x.CandidateId == candidateId);

            return ObjectMapper.Map<List<ExamLink>, List<ExamLinkDto>>(links);
        }

        public async Task<bool> ValidateExamLinkAsync(string token)
        {
            var link = await _examLinkRepository.FirstOrDefaultAsync(x => x.SecureToken == token);
            if (link == null || link.ExpiryDate < Clock.Now || link.CurrentAttempts >= link.MaxAttempts)
            {
                return false;
            }

            link.CurrentAttempts++;
            await _examLinkRepository.UpdateAsync(link);

            return true;
        }
        public async Task<ExamAccessResultDto> GetExamAccessDetailsByTokenAsync(string token)
        {
            var link = await _examLinkRepository.FirstOrDefaultAsync(x => x.SecureToken == token);
            if (link == null)
            {
                return new ExamAccessResultDto
                {
                    IsAccessible = false,
                    Message = "Invalid link"
                };
            }

            if (link.ExpiryDate < Clock.Now)
            {
                return new ExamAccessResultDto
                {
                    IsAccessible = false,
                    Message = "Link expired"
                };
            }

            if (link.CurrentAttempts >= link.MaxAttempts)
            {
                return new ExamAccessResultDto
                {
                    IsAccessible = false,
                    Message = "Maximum attempts exceeded"
                };
            }

            return new ExamAccessResultDto
            {
                IsAccessible = true,
                Message = "Access granted",
                CandidateId = link.CandidateId,
                ExamId = link.ExamId,
                ExpiryDate = link.ExpiryDate,
                MaxAttempts = link.MaxAttempts,
                CurrentAttempts = link.CurrentAttempts
            };
        }

        public async Task<StartExamFromLinkResultDto> StartExamByTokenAsync(string token)
        {
            var link = await _examLinkRepository.FirstOrDefaultAsync(x => x.SecureToken == token);
            if (link == null)
            {
                return new StartExamFromLinkResultDto
                {
                    IsStarted = false,
                    Message = "Invalid exam link."
                };
            }

            if (link.ExpiryDate < Clock.Now)
            {
                return new StartExamFromLinkResultDto
                {
                    IsStarted = false,
                    Message = "Exam link has expired."
                };
            }

            if (link.CurrentAttempts >= link.MaxAttempts)
            {
                return new StartExamFromLinkResultDto
                {
                    IsStarted = false,
                    Message = "Maximum number of attempts reached."
                };
            }

            // يمكن هنا التحقق من وجود محاولة سابقة غير مكتملة ثم نعيدها إن وجدت (Optional)

            link.CurrentAttempts++;
            await _examLinkRepository.UpdateAsync(link);

            return new StartExamFromLinkResultDto
            {
                IsStarted = true,
                CandidateId = link.CandidateId,
                ExamId = link.ExamId,
                Message = "Exam started successfully."
            };
        }
    }

}
