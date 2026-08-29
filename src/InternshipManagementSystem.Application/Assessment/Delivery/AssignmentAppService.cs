using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.Settings;
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
    private readonly IRepository<ExamForm, Guid> _forms;
    private readonly IRepository<Candidate, Guid> _candidates;
    private readonly IRepository<CandidateGroupMember, Guid> _members;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AssignmentAppService> _logger;

    public AssignmentAppService(
        IRepository<Assignment, Guid> assignments,
        IRepository<ExamLink, Guid> links,
        IRepository<Exam, Guid> exams,
        IRepository<ExamForm, Guid> forms,
        IRepository<Candidate, Guid> candidates,
        IRepository<CandidateGroupMember, Guid> members,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AssignmentAppService> logger)
    {
        _assignments = assignments;
        _links = links;
        _exams = exams;
        _forms = forms;
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

        // Checked before anybody is emailed. A sitting sent on a paper that turns
        // out to be a draft cannot be taken back once the links are out.
        if (input.ExamFormId is { } formId)
        {
            // FindAsync rather than GetAsync: a form belonging to another tenant is
            // filtered out and comes back null, which is the same answer as deleted
            // and should read the same to the caller.
            var form = await _forms.FindAsync(formId);

            if (form is null || form.ExamId != exam.Id)
            {
                throw new BusinessException(
                    InternshipManagementSystemDomainErrorCodes.AssignmentFormNotAvailable);
            }

            if (!form.IsUsable)
            {
                throw new BusinessException(
                    InternshipManagementSystemDomainErrorCodes.AssignmentFormNotPublished);
            }
        }

        // Sending is its own permission and always was; nothing enforced it, so
        // anybody who could create a sitting could also mail forty people. The
        // distinction matters to an organisation that lets coordinators prepare
        // exams and reserves the sending to one person.
        if (input.SendEmail)
        {
            await AuthorizationService.CheckAsync(
                InternshipManagementSystemPermissions.Assignments.SendEmail);
        }

        var recipients = await ResolveRecipientsAsync(input);

        if (recipients.Count == 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AssignmentGroupEmpty);
        }

        var assignment = new Assignment(GuidGenerator.Create(), CurrentTenant.Id, exam.Id, input.ExpiresAt)
        {
            ExamFormId = input.ExamFormId,
            RotateForms = input.RotateForms,
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
    [Authorize(InternshipManagementSystemPermissions.Assignments.Create)]
    public async Task<AssignmentRecipientDto> ReissueLinkAsync(Guid linkId)
    {
        var link = await _links.GetAsync(linkId);
        var candidate = await _candidates.GetAsync(link.CandidateId);

        var token = ExamSessionTokenService.NewLinkToken();

        link.TokenHash = ExamSessionTokenService.HashLinkToken(token);

        // Revoked links stay revoked unless somebody deliberately reissues one;
        // reissuing is that deliberate act, so it also brings the link back.
        link.IsRevoked = false;

        // The old address stops working now. Two live links for one sitting are
        // two ways to spend the same attempt, and the second one to arrive is the
        // one somebody will use.
        //
        // FirstOpenedAt is cleared with it: it described the link that no longer
        // exists, and leaving it would tell a coordinator this person has already
        // opened an address they have never been sent.
        link.FirstOpenedAt = null;

        await _links.UpdateAsync(link, autoSave: true);

        var clientUrl = _configuration["App:ClientUrl"]?.TrimEnd('/') ?? string.Empty;

        _logger.LogInformation(
            "Reissued the link for candidate {CandidateId} on exam {ExamId}.",
            link.CandidateId, link.ExamId);

        return new AssignmentRecipientDto
        {
            CandidateId = candidate.Id,
            CandidateName = candidate.FullName,
            Email = candidate.Email,
            Url = $"{clientUrl}/exam/{token}",
            EmailSent = false,
        };
    }

    /// <summary>
    /// Moves a link's deadline, so somebody who missed it can still sit.
    /// <para>
    /// Reissuing does not do this and should not: a new address for a lost link
    /// and a new deadline for a missed one are different decisions, and one of
    /// them is the coordinator's to make deliberately. But without this, they
    /// could only make the first — so a coordinator helping somebody who missed
    /// Friday reissued the link, handed over a fresh address, and it was already
    /// expired. The token was new and the deadline was not.
    /// </para>
    /// <para>
    /// Forward only. Pulling a deadline back onto a sitting somebody is part way
    /// through ends it under them with no warning; closing an exam early is what
    /// revoking is for, and it says so to the person holding the link.
    /// </para>
    /// </summary>
    [Authorize(InternshipManagementSystemPermissions.Assignments.Create)]
    public async Task<ExamLinkDto> ExtendLinkAsync(Guid linkId, DateTime expiresAt)
    {
        var link = await _links.GetAsync(linkId);

        if (expiresAt <= Clock.Now)
        {
            throw new BusinessException(
                InternshipManagementSystemDomainErrorCodes.ExamLinkExpiryInPast);
        }

        if (expiresAt < link.ExpiresAt)
        {
            throw new BusinessException(
                InternshipManagementSystemDomainErrorCodes.ExamLinkExpiryMovedBack);
        }

        link.ExpiresAt = expiresAt;

        // An extended link is one somebody is meant to use, so a revocation would
        // contradict the act. Left alone deliberately all the same: revoking is
        // how a leaked link is killed, and quietly undoing that because a date
        // moved would hand the exam back to whoever it leaked to.
        await _links.UpdateAsync(link, autoSave: true);

        _logger.LogInformation(
            "Extended the link for candidate {CandidateId} on exam {ExamId} to {ExpiresAt}.",
            link.CandidateId, link.ExamId, expiresAt);

        var candidate = await _candidates.GetAsync(link.CandidateId);

        return new ExamLinkDto
        {
            Id = link.Id,
            ExamId = link.ExamId,
            CandidateId = link.CandidateId,
            CandidateName = candidate.FullName,
            TokenPrefix = link.TokenPrefix,
            ExpiresAt = link.ExpiresAt,
            MaxAttempts = link.MaxAttempts,
            AttemptsUsed = link.AttemptsUsed,
            IsRevoked = link.IsRevoked,
            FirstOpenedAt = link.FirstOpenedAt,
            EmailSentAt = link.EmailSentAt,
        };
    }

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
    /// Sends the invitation, in the organisation's name and both languages.
    /// <para>
    /// The message itself is built by <see cref="InvitationEmail"/> — a pure
    /// function, so what a candidate receives can be asserted without a mail
    /// server. This reads the two tenant settings it needs and hands them over.
    /// </para>
    /// </summary>
    private async Task SendInvitationAsync(Candidate candidate, Exam exam, string url, DateTime expiresAt)
    {
        var message = InvitationEmail.Build(
            await SettingProvider.GetOrNullAsync(InternshipManagementSystemSettings.OrganizationName),
            await SettingProvider.GetOrNullAsync(InternshipManagementSystemSettings.BrandColor),
            candidate.FullName,
            exam.Title,
            exam.TimeLimitInMinutes,
            expiresAt,
            url);

        await _emailSender.SendAsync(candidate.Email, message.Subject, message.Body, isBodyHtml: true);
    }
}
