using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.People.Dtos;
using Shouldly;
using Volo.Abp;
using Volo.Abp.MultiTenancy;
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
