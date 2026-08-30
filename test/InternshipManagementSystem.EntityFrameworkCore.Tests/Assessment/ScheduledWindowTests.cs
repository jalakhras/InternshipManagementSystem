using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.People.Dtos;
using InternshipManagementSystem.Settings;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// When a scheduled exam opens, and whose clock decides.
/// <para>
/// A coordinator typing 09:00 into a date-and-time box means nine in the morning
/// where they are. The comparison used the server's own clock, and the time-zone
/// setting — whose hint reads "every exam clock and scheduled window is read in
/// this zone; getting it wrong opens exams at the wrong hour" — was read by
/// nothing.
/// </para>
/// <para>
/// On one machine in one country that is invisible, which is why it survived. On
/// a container running UTC serving a Riyadh academy it opens the exam three
/// hours late, to a room of people already sitting there.
/// </para>
/// </summary>
public class ScheduledWindowTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly ISettingManager _settings;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000091");

    public ScheduledWindowTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _settings = GetRequiredService<ISettingManager>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_window_is_read_in_the_organisations_zone_not_the_servers()
    {
        await AsTenantAsync(async () =>
        {
            // Tokyo, and not Riyadh, on purpose. This machine sits at UTC+03:00
            // and so does Riyadh — the conversion would be a no-op and the test
            // would pass whether or not the zone was read at all. Tokyo is six
            // hours away from here and observes no daylight saving, so the offset
            // is the same in March as in September and the assertion means the
            // same thing every day of the year.
            const string Far = "Tokyo Standard Time";

            await _settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.TimeZone, Far);

            var zone = TimeZoneInfo.FindSystemTimeZoneById(Far);
            var thereNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);

            // Open for an hour around that organisation's own "now".
            var token = await SendAsync(
                "window-a", thereNow.AddMinutes(-30), thereNow.AddMinutes(30));

            var preview = await _taking.OpenLinkAsync(token);

            preview.IsAccessible.ShouldBeTrue(preview.BlockReason);

            await _settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.TimeZone, "Asia/Riyadh");
        });
    }

    [Fact]
    public async Task A_window_that_has_not_opened_yet_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            const string Far = "Tokyo Standard Time";

            await _settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.TimeZone, Far);

            var zone = TimeZoneInfo.FindSystemTimeZoneById(Far);
            var thereNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);

            var token = await SendAsync(
                "window-b", thereNow.AddHours(6), thereNow.AddHours(8));

            var preview = await _taking.OpenLinkAsync(token);

            // The other half. A rule that only ever says yes is not a rule, and a
            // conversion that shifted everything into the past would pass the
            // test above while opening every exam early.
            preview.IsAccessible.ShouldBeFalse();

            await _settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.TimeZone, "Asia/Riyadh");
        });
    }

    [Fact]
    public async Task An_unusable_zone_falls_back_rather_than_closing_the_exam()
    {
        await AsTenantAsync(async () =>
        {
            await _settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.TimeZone, "Not/AZone");

            var token = await SendAsync(
                "window-c", DateTime.Now.AddMinutes(-30), DateTime.Now.AddMinutes(30));

            var preview = await _taking.OpenLinkAsync(token);

            // A mistyped zone must not stop an exam from opening at all. It falls
            // back to the clock that was used before this existed, and says so in
            // the log.
            preview.IsAccessible.ShouldBeTrue(preview.BlockReason);

            await _settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.TimeZone, "Asia/Riyadh");
        });
    }

    // ------------------------------------------------------------------ helpers

    private async Task<string> SendAsync(string code, DateTime opensAt, DateTime closesAt)
    {
        var categories = GetRequiredService<IRepository<Category, Guid>>();

        var category = await categories.InsertAsync(
            new Category(Guid.NewGuid(), Tenant, code, code), autoSave: true);

        var exam = await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = code,
            TimeLimitInMinutes = 30,
            PassingPercentage = 50m,
            CategoryId = category.Id,
            IsScheduled = true,
            ScheduledStartTime = opensAt,
            ScheduledEndTime = closesAt,
        });

        await _questions.CreateAsync(new CreateUpdateQuestionDto
        {
            ExamId = exam.Id,
            Type = QuestionTypes.SingleChoice,
            Text = code + " question",
            Score = 1m,
            Payload = PayloadJson.Write(new ChoicePayload
            {
                Options =
                [
                    new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                    new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
                ],
            }),
        });

        await _exams.PublishAsync(exam.Id);

        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = "Waiting in the room",
            Email = code + "@example.test",
        });

        var result = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = exam.Id,
            CandidateId = candidate.Id,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 1,
            SendEmail = false,
        });

        return result.Recipients.Single().Url.Split('/').Last();
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
