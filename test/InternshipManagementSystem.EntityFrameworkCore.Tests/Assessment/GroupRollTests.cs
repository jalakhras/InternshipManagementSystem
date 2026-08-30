using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.People.Dtos;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Validation;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// An empty list is not self-explanatory.
/// <para>
/// The roll editor sends the whole class on every save, so an empty list means
/// "remove everybody" — and the server treated it as a complete and
/// authoritative statement of intent. But an empty list arrives for two
/// entirely different reasons: a coordinator who unticked everyone, and a
/// screen whose read of the current roll failed and which therefore believes
/// the class is already empty.
/// </para>
/// <para>
/// The second really happened: one failed request, no error anywhere on the
/// screen, a dialog that looked healthy with nothing ticked, and a class of
/// twelve became a class of none — reported as a successful save, with no undo.
/// </para>
/// </summary>
public class GroupRollTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ICandidateAppService _candidates;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-0000000000b1");

    public GroupRollTests()
    {
        _candidates = GetRequiredService<ICandidateAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task An_empty_list_that_does_not_say_it_means_it_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            var (groupId, _) = await ClassOfOneAsync("roll-a");

            var refused = await Should.ThrowAsync<BusinessException>(() =>
                _candidates.SetGroupMembersAsync(groupId, new SetGroupMembersDto
                {
                    CandidateIds = new List<Guid>(),
                }));

            refused.Code.ShouldBe("IMS:Group:EmptyingNotConfirmed");

            // And nothing was removed on the way to refusing.
            var after = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                GroupId = groupId,
                MaxResultCount = 50,
            });

            after.Items.Count.ShouldBe(1);
        });
    }

    [Fact]
    public async Task A_class_can_still_be_emptied_on_purpose()
    {
        await AsTenantAsync(async () =>
        {
            var (groupId, _) = await ClassOfOneAsync("roll-b");

            // The other half. A course that ended is a real thing, and refusing
            // to let anybody empty a class would be its own kind of wrong — the
            // guard is about *unmeant* emptiness, not about emptiness.
            await _candidates.SetGroupMembersAsync(groupId, new SetGroupMembersDto
            {
                CandidateIds = new List<Guid>(),
                ConfirmEmptied = true,
            });

            var after = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                GroupId = groupId,
                MaxResultCount = 50,
            });

            after.Items.Count.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Emptying_a_class_that_is_already_empty_needs_no_confirmation()
    {
        await AsTenantAsync(async () =>
        {
            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "roll-c",
            });

            // Nothing to lose, so nothing to protect: a guard that fires when
            // there is no harm to prevent only teaches people to click past it.
            await _candidates.SetGroupMembersAsync(group.Id, new SetGroupMembersDto
            {
                CandidateIds = new List<Guid>(),
            });
        });
    }

    // ------------------------------------------------------------------------
    // Sending the change instead of the whole roll.
    //
    // The whole-list save is only a true sentence while the caller holds the
    // whole class, and the screen never did: it read 500 candidates and searched
    // them in the browser. Past 500 a person could not be found, could not be
    // ticked, and so could not be put into any class at all — and raising the
    // number only moves the wall, because ABP refuses a page over 1000.
    // ------------------------------------------------------------------------

    [Fact]
    public async Task A_roll_longer_than_the_screen_reads_is_not_truncated_by_editing_it()
    {
        await AsTenantAsync(async () =>
        {
            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "roll-long",
            });

            var enrolled = new List<Guid>();

            for (var i = 0; i < 30; i++)
            {
                enrolled.Add(await PersonAsync($"roll-long-{i:00}"));
            }

            await _candidates.SetGroupMembersAsync(group.Id, new SetGroupMembersDto
            {
                CandidateIds = enrolled,
            });

            // What the screen actually holds: one page, not the class. Twenty
            // here stands for the five hundred the roll editor used to read —
            // the number differs, the fact that it is smaller than the class
            // does not.
            var page = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                GroupId = group.Id,
                MaxResultCount = 20,
            });

            page.Items.Count.ShouldBe(20);
            page.TotalCount.ShouldBe(30);

            var late = await PersonAsync("roll-long-late");

            // One person added, and nothing said about the other thirty. Under
            // the whole-list protocol this same screen would have sent the
            // twenty it holds plus the new one, and the ten it never read would
            // have been removed by a save that reported success.
            await _candidates.ChangeGroupMembersAsync(group.Id, new ChangeGroupMembersDto
            {
                Add = new List<Guid> { late },
            });

            var after = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                GroupId = group.Id,
                MaxResultCount = 100,
            });

            after.TotalCount.ShouldBe(31);
        });
    }

    [Fact]
    public async Task Two_coordinators_editing_the_same_class_both_keep_their_work()
    {
        await AsTenantAsync(async () =>
        {
            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "roll-race",
            });

            var already = await PersonAsync("roll-race-0");

            await _candidates.SetGroupMembersAsync(group.Id, new SetGroupMembersDto
            {
                CandidateIds = new List<Guid> { already },
            });

            var hers = await PersonAsync("roll-race-1");
            var his = await PersonAsync("roll-race-2");

            // Both opened the roll and both saw the same single name. She saves
            // first; he saves a moment later, from a screen that still believes
            // the class holds only that one name.
            //
            // Worklist 6.1 is the last write winning silently, and it was a
            // property of the sentence being sent rather than of the timing: his
            // whole-list save would have said "this class is roll-race-0 and
            // roll-race-2", which is a complete and authoritative removal of
            // hers. "Add roll-race-2" says nothing about her at all, so there is
            // no lost update left for a version stamp to detect.
            await _candidates.ChangeGroupMembersAsync(group.Id, new ChangeGroupMembersDto
            {
                Add = new List<Guid> { hers },
            });

            await _candidates.ChangeGroupMembersAsync(group.Id, new ChangeGroupMembersDto
            {
                Add = new List<Guid> { his },
            });

            var after = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                GroupId = group.Id,
                MaxResultCount = 50,
            });

            after.TotalCount.ShouldBe(3);
            after.Items.Select(p => p.Id).ShouldContain(hers);
            after.Items.Select(p => p.Id).ShouldContain(his);
        });
    }

    [Fact]
    public async Task A_change_touches_only_the_people_it_names()
    {
        await AsTenantAsync(async () =>
        {
            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "roll-remove",
            });

            var staying = await PersonAsync("roll-remove-1");
            var leaving = await PersonAsync("roll-remove-2");
            var joining = await PersonAsync("roll-remove-3");

            await _candidates.SetGroupMembersAsync(group.Id, new SetGroupMembersDto
            {
                CandidateIds = new List<Guid> { staying, leaving },
            });

            await _candidates.ChangeGroupMembersAsync(group.Id, new ChangeGroupMembersDto
            {
                Add = new List<Guid> { joining },
                Remove = new List<Guid> { leaving },
            });

            var after = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                GroupId = group.Id,
                MaxResultCount = 50,
            });

            after.Items.Select(p => p.Id).OrderBy(x => x)
                .ShouldBe(new[] { staying, joining }.OrderBy(x => x));
        });
    }

    [Fact]
    public async Task A_candidate_who_does_not_exist_is_not_quietly_put_into_a_class()
    {
        await AsTenantAsync(async () =>
        {
            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "roll-unknown",
            });

            // Left to the foreign key this is a database error, which tells the
            // caller nothing about which of the ids it sent was the wrong one.
            await Should.ThrowAsync<EntityNotFoundException>(() =>
                _candidates.ChangeGroupMembersAsync(group.Id, new ChangeGroupMembersDto
                {
                    Add = new List<Guid> { Guid.NewGuid() },
                }));
        });
    }

    [Fact]
    public async Task Adding_and_removing_the_same_person_at_once_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "roll-contradiction",
            });

            var person = await PersonAsync("roll-contradiction-1");

            // Not an edit with an obvious winner — a caller that has lost track
            // of what it is asking for. Choosing a half silently would make the
            // roll depend on the order this happens to apply them in.
            await Should.ThrowAsync<AbpValidationException>(() =>
                _candidates.ChangeGroupMembersAsync(group.Id, new ChangeGroupMembersDto
                {
                    Add = new List<Guid> { person },
                    Remove = new List<Guid> { person },
                }));
        });
    }

    private async Task<Guid> PersonAsync(string code)
    {
        var person = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = code,
            Email = code + "@example.test",
        });

        return person.Id;
    }

    private async Task<(Guid GroupId, Guid CandidateId)> ClassOfOneAsync(string code)
    {
        var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
        {
            Name = code,
        });

        var person = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = code,
            Email = code + "@example.test",
        });

        await _candidates.SetGroupMembersAsync(group.Id, new SetGroupMembersDto
        {
            CandidateIds = new List<Guid> { person.Id },
        });

        return (group.Id, person.Id);
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
