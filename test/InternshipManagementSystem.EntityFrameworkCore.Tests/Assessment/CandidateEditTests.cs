using System;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.People.Dtos;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Correcting one field must not delete the others.
/// <para>
/// The edit dialog loaded three of a person's five fields and sent three of
/// five, and the server assigns every field it is given. So a coordinator
/// fixing a misspelled name silently erased that person's phone number and
/// their group — no error, no warning, and nothing on the screen afterwards
/// that showed anything had gone.
/// </para>
/// <para>
/// This is the shape of loss that never gets reported as a bug: whoever finds
/// the blank field months later assumes it was never filled in.
/// </para>
/// </summary>
public class CandidateEditTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ICandidateAppService _candidates;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000091");

    public CandidateEditTests()
    {
        _candidates = GetRequiredService<ICandidateAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Fixing_a_spelling_keeps_the_phone_number_and_the_group()
    {
        await AsTenantAsync(async () =>
        {
            var categories = GetRequiredService<IRepository<Category, Guid>>();
            var category = await categories.InsertAsync(
                new Category(Guid.NewGuid(), Tenant, "edit-a", "edit-a"), autoSave: true);

            var person = await _candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "سامي الحرباوي",
                Email = "edit-a@example.test",
                PhoneNumber = "+966500000001",
                CategoryId = category.Id,
                Reference = "STU-4102",
            });

            person.PhoneNumber.ShouldBe("+966500000001");
            person.CategoryId.ShouldBe(category.Id);

            // What the screen does: send the person back with the name corrected.
            var corrected = await _candidates.UpdateAsync(person.Id, new CreateUpdateCandidateDto
            {
                FullName = "سامي الخرباوي",
                Email = person.Email,
                PhoneNumber = person.PhoneNumber,
                CategoryId = person.CategoryId,
                Reference = person.Reference,
            });

            corrected.FullName.ShouldBe("سامي الخرباوي");
            corrected.PhoneNumber.ShouldBe("+966500000001");
            corrected.CategoryId.ShouldBe(category.Id);
            corrected.Reference.ShouldBe("STU-4102");
        });
    }

    [Fact]
    public async Task Clearing_a_field_on_purpose_still_clears_it()
    {
        await AsTenantAsync(async () =>
        {
            var person = await _candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "من غادر مجموعته",
                Email = "edit-b@example.test",
                PhoneNumber = "+966500000002",
            });

            var cleared = await _candidates.UpdateAsync(person.Id, new CreateUpdateCandidateDto
            {
                FullName = person.FullName,
                Email = person.Email,
                PhoneNumber = null,
            });

            // The other half. Keeping what was not sent would have made a field
            // impossible to empty — somebody whose number changed to nothing
            // would be stuck with the old one, which is its own kind of wrong.
            cleared.PhoneNumber.ShouldBeNull();
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
