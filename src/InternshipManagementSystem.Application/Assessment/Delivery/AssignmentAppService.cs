using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Creating assignments and the links they produce.
/// </summary>
[Authorize(InternshipManagementSystemPermissions.Assignments.Default)]
public class AssignmentAppService : ApplicationService, IAssignmentAppService
{
    private readonly IRepository<Assignment, Guid> _assignments;
    private readonly IRepository<ExamLink, Guid> _links;
    private readonly IRepository<Exam, Guid> _exams;
    private readonly IRepository<Candidate, Guid> _candidates;
    private readonly IRepository<CandidateGroupMember, Guid> _members;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AssignmentAppService> _logger;

    public AssignmentAppService(
        IRepository<Assignment, Guid> assignments,
        IRepository<ExamLink, Guid> links,
        IRepository<Exam, Guid> exams,
        IRepository<Candidate, Guid> candidates,
        IRepository<CandidateGroupMember, Guid> members,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AssignmentAppService> logger)
    {
        _assignments = assignments;
        _links = links;
        _exams = exams;
        _candidates = candidates;
        _members = members;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    [Authorize(InternshipManagementSystemPermissions.Assignments.Create)]
    public async Task<AssignmentResultDto> CreateAsync(CreateAssignmentDto input)
    {
        if (input.CandidateId is null && input.CandidateGroupId is null)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AssignmentTargetMissing);
        }

        if (input.CandidateId is not null && input.CandidateGroupId is not null)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AssignmentTargetAmbiguous);
        }

        if (input.ExpiresAt <= Clock.Now)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AssignmentExpiryInPast);
        }

        var exam = await _exams.GetAsync(input.ExamId);

        if (exam.Status != ExamStatus.Published)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamNotPublished);
        }

        var recipients = await ResolveRecipientsAsync(input);

        if (recipients.Count == 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AssignmentGroupEmpty);
        }

        var assignment = new Assignment(GuidGenerator.Create(), CurrentTenant.Id, exam.Id, input.ExpiresAt)
        {
            CandidateId = input.CandidateId,
            CandidateGroupId = input.CandidateGroupId,
            MaxAttempts = input.MaxAttempts,
            SendEmail = input.SendEmail,
            Note = input.Note
        };

        await _assignments.InsertAsync(assignment, autoSave: true);

        var result = new AssignmentResultDto { AssignmentId = assignment.Id };
        var clientUrl = _configuration["App:ClientUrl"]?.TrimEnd('/') ?? string.Empty;

        foreach (var candidate in recipients)
        {
            // Each person gets their own token, so links stay individually
            // traceable and one can be revoked without touching the rest.
            var token = ExamSessionTokenService.NewLinkToken();

            var link = new ExamLink(
                GuidGenerator.Create(), CurrentTenant.Id, assignment.Id, exam.Id, candidate.Id,
                ExamSessionTokenService.HashLinkToken(token),
                token[..8],
                input.ExpiresAt,
                input.MaxAttempts);

            await _links.InsertAsync(link, autoSave: true);

            var url = $"{clientUrl}/exam/{token}";

            var recipient = new AssignmentRecipientDto
            {
                CandidateId = candidate.Id,
                CandidateName = candidate.FullName,
                Email = candidate.Email,
                Url = url
            };

            if (input.SendEmail)
            {
                try
                {
                    await SendInvitationAsync(candidate, exam, url, input.ExpiresAt);
                    link.EmailSentAt = Clock.Now;
                    await _links.UpdateAsync(link, autoSave: true);

                    recipient.EmailSent = true;
                    result.EmailsSent++;
                }
                catch (Exception ex)
                {
                    // One unreachable address must not abandon the other thirty-nine
                    // links, and the operator needs to know which one to chase.
                    _logger.LogError(ex, "Could not email the exam link to {Email}.", candidate.Email);
                    recipient.EmailError = ex.Message;
                    result.EmailsFailed++;
                }
            }

            result.Recipients.Add(recipient);
            result.LinksCreated++;
        }

        assignment.LinkCount = result.LinksCreated;
        assignment.EmailsSent = result.EmailsSent;
        await _assignments.UpdateAsync(assignment, autoSave: true);

        return result;
    }

    [Authorize(InternshipManagementSystemPermissions.Assignments.View)]
    public async Task<PagedResultDto<ExamLinkDto>> GetLinksAsync(Guid examId, PagedAndSortedResultRequestDto input)
    {
        var links = await _links.GetQueryableAsync();
        var candidates = await _candidates.GetQueryableAsync();

        var query = from link in links
                    join candidate in candidates on link.CandidateId equals candidate.Id
                    where link.ExamId == examId
                    orderby link.CreationTime descending
                    select new ExamLinkDto
                    {
                        Id = link.Id,
                        ExamId = link.ExamId,
                        CandidateId = link.CandidateId,
                        CandidateName = candidate.FullName,
                        // The token itself is not stored in a readable form and is
                        // never returned after creation.
                        TokenPrefix = link.TokenPrefix,
                        ExpiresAt = link.ExpiresAt,
                        MaxAttempts = link.MaxAttempts,
                        AttemptsUsed = link.AttemptsUsed,
                        IsRevoked = link.IsRevoked,
                        FirstOpenedAt = link.FirstOpenedAt,
                        EmailSentAt = link.EmailSentAt
                    };

        var totalCount = await query.CountAsync();
        var items = await query.Skip(input.SkipCount).Take(input.MaxResultCount).ToListAsync();

        return new PagedResultDto<ExamLinkDto>(totalCount, items);
    }

    /// <summary>
    /// Kills a link that leaked or went to the wrong person. Revoked is a distinct
    /// state from invalid so the person holding it is told what happened.
    /// </summary>
    [Authorize(InternshipManagementSystemPermissions.Assignments.Revoke)]
    public async Task RevokeLinkAsync(Guid linkId)
    {
        var link = await _links.GetAsync(linkId);

        link.IsRevoked = true;
        link.RevokedAt = Clock.Now;

        await _links.UpdateAsync(link, autoSave: true);
    }

    private async Task<List<Candidate>> ResolveRecipientsAsync(CreateAssignmentDto input)
    {
        if (input.CandidateId is { } candidateId)
        {
            return [await _candidates.GetAsync(candidateId)];
        }

        var members = await _members.GetQueryableAsync();
        var candidates = await _candidates.GetQueryableAsync();

        return await (from member in members
                      join candidate in candidates on member.CandidateId equals candidate.Id
                      where member.CandidateGroupId == input.CandidateGroupId!.Value
                      select candidate).ToListAsync();
    }

    /// <summary>
    /// Sends the invitation in both languages. The tenant's own recipients may read
    /// either, and guessing wrong on a one-shot exam invitation is expensive.
    /// </summary>
    private async Task SendInvitationAsync(Candidate candidate, Exam exam, string url, DateTime expiresAt)
    {
        var subject = $"{exam.Title} — دعوة لأداء اختبار / Assessment invitation";

        var body = $"""
            <div dir="rtl" style="font-family:system-ui,sans-serif;line-height:1.7">
              <p>مرحباً {candidate.FullName},</p>
              <p>لقد تم إسناد اختبار <strong>{exam.Title}</strong> إليك.</p>
              <ul>
                <li>المدة: {exam.TimeLimitInMinutes} دقيقة</li>
                <li>صلاحية الرابط حتى: {expiresAt:yyyy-MM-dd HH:mm}</li>
              </ul>
              <p><a href="{url}">ابدأ الاختبار</a></p>
              <p style="color:#666;font-size:.9em">لا يبدأ العدّ التنازلي إلا عند ضغطك على زر البدء.</p>
            </div>
            <hr>
            <div dir="ltr" style="font-family:system-ui,sans-serif;line-height:1.7">
              <p>Hello {candidate.FullName},</p>
              <p>You have been assigned <strong>{exam.Title}</strong>.</p>
              <ul>
                <li>Duration: {exam.TimeLimitInMinutes} minutes</li>
                <li>Link valid until: {expiresAt:yyyy-MM-dd HH:mm}</li>
              </ul>
              <p><a href="{url}">Start the assessment</a></p>
              <p style="color:#666;font-size:.9em">The timer does not start until you press start.</p>
            </div>
            """;

        await _emailSender.SendAsync(candidate.Email, subject, body, isBodyHtml: true);
    }
}
