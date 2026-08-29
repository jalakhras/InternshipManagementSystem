using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.People.Dtos;
using Shouldly;
using Volo.Abp;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Getting a roll of students into the product.
/// <para>
/// The single largest thing standing between a training centre and using this is
/// that their students are already in a spreadsheet. Retyping forty names is the
/// reason a trial stops on the first evening, so this path has to be forgiving
/// of what people actually paste.
/// </para>
/// </summary>
public class CandidateImportTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ICandidateAppService _candidates;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000021");

    public CandidateImportTests()
    {
        _candidates = GetRequiredService<ICandidateAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_comma_separated_paste_creates_everybody_on_it()
    {
        await AsTenantAsync(async () =>
        {
            var result = await _candidates.ImportAsync(new ImportCandidatesDto
            {
                Text = "Layla Hassan, layla@example.com\nOmar Nasser, omar@example.com",
            });

            result.Created.ShouldBe(2);
            result.Problems.ShouldBeEmpty();

            var list = await _candidates.GetListAsync(new CandidateListRequestDto());
            list.TotalCount.ShouldBe(2);
        });
    }

    [Fact]
    public async Task A_tab_separated_paste_works_the_same()
    {
        await AsTenantAsync(async () =>
        {
            // What arrives when somebody copies straight out of a spreadsheet.
            // Asking which separator they have is asking them to know something
            // they should not need to.
            var result = await _candidates.ImportAsync(new ImportCandidatesDto
            {
                Text = "Layla Hassan\tlayla@example.com\t0555\tSTU-1",
            });

            result.Created.ShouldBe(1);

            var created = (await _candidates.GetListAsync(new CandidateListRequestDto())).Items.Single();

            created.PhoneNumber.ShouldBe("0555");
            created.Reference.ShouldBe("STU-1");
        });
    }

    [Fact]
    public async Task The_address_is_found_rather_than_assumed_to_be_second()
    {
        await AsTenantAsync(async () =>
        {
            // Some rolls are written address-first. Refusing those would be
            // refusing a valid list over a column order.
            var result = await _candidates.ImportAsync(new ImportCandidatesDto
            {
                Text = "layla@example.com, Layla Hassan",
            });

            result.Created.ShouldBe(1);

            var created = (await _candidates.GetListAsync(new CandidateListRequestDto())).Items.Single();

            created.FullName.ShouldBe("Layla Hassan");
            created.Email.ShouldBe("layla@example.com");
        });
    }

    [Fact]
    public async Task Importing_the_same_list_twice_does_not_double_the_roll()
    {
        await AsTenantAsync(async () =>
        {
            var text = "Layla Hassan, layla@example.com\nOmar Nasser, omar@example.com";

            await _candidates.ImportAsync(new ImportCandidatesDto { Text = text });
            var second = await _candidates.ImportAsync(new ImportCandidatesDto { Text = text });

            // Matched by address and left alone, so a coordinator who pastes the
            // updated roll gets the new people and nothing else.
            second.Created.ShouldBe(0);
            second.AlreadyPresent.ShouldBe(2);

            (await _candidates.GetListAsync(new CandidateListRequestDto())).TotalCount.ShouldBe(2);
        });
    }

    [Fact]
    public async Task A_bad_line_is_reported_with_its_number_and_the_rest_still_import()
    {
        await AsTenantAsync(async () =>
        {
            var result = await _candidates.ImportAsync(new ImportCandidatesDto
            {
                Text = "Layla Hassan, layla@example.com\nNo address here\nOmar Nasser, not-an-email",
            });

            // One bad row must not lose the good ones. The line number is counted
            // over the pasted text so it matches what the person is looking at.
            result.Created.ShouldBe(1);
            result.Problems.Count.ShouldBe(2);

            result.Problems[0].Line.ShouldBe(2);
            result.Problems[0].Reason.ShouldBe("IMS:Import:NeedsNameAndEmail");

            result.Problems[1].Line.ShouldBe(3);
            result.Problems[1].Reason.ShouldBe("IMS:Import:NotAnEmail");
        });
    }

    [Fact]
    public async Task A_dry_run_reports_what_would_happen_and_writes_nothing()
    {
        await AsTenantAsync(async () =>
        {
            var result = await _candidates.ImportAsync(new ImportCandidatesDto
            {
                Text = "Layla Hassan, layla@example.com\nbroken line",
                DryRun = true,
            });

            // Somebody pasting forty rows should see the three that are wrong
            // before committing, not afterwards.
            result.Created.ShouldBe(1);
            result.Problems.Count.ShouldBe(1);

            (await _candidates.GetListAsync(new CandidateListRequestDto())).TotalCount.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Everyone_imported_can_land_in_a_cohort()
    {
        await AsTenantAsync(async () =>
        {
            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "Evening A1",
            });

            var result = await _candidates.ImportAsync(new ImportCandidatesDto
            {
                Text = "Layla Hassan, layla@example.com\nOmar Nasser, omar@example.com",
                GroupId = group.Id,
            });

            result.AddedToGroup.ShouldBe(2);

            var inGroup = await _candidates.GetListAsync(new CandidateListRequestDto { GroupId = group.Id });

            inGroup.TotalCount.ShouldBe(2);
            inGroup.Items.ShouldAllBe(c => c.GroupNames.Contains("Evening A1"));
        });
    }

    [Fact]
    public async Task Two_candidates_cannot_share_an_address()
    {
        await AsTenantAsync(async () =>
        {
            await _candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "Layla Hassan",
                Email = "layla@example.com",
            });

            // The address is how a link reaches them and how an import recognises
            // somebody already on the roll.
            var thrown = await Should.ThrowAsync<BusinessException>(() =>
                _candidates.CreateAsync(new CreateUpdateCandidateDto
                {
                    FullName = "Layla H",
                    Email = "LAYLA@example.com",
                }));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.CandidateEmailTaken);
        });
    }

    [Fact]
    public async Task Deleting_a_cohort_keeps_the_people_in_it()
    {
        await AsTenantAsync(async () =>
        {
            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto { Name = "Morning" });

            await _candidates.ImportAsync(new ImportCandidatesDto
            {
                Text = "Layla Hassan, layla@example.com",
                GroupId = group.Id,
            });

            await _candidates.DeleteGroupAsync(group.Id);

            // Deleting a heading must not delete a roll of students.
            (await _candidates.GetListAsync(new CandidateListRequestDto())).TotalCount.ShouldBe(1);
            (await _candidates.GetGroupsAsync()).ShouldBeEmpty();
        });
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
