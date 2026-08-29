using System;
using System.Threading.Tasks;
using InternshipManagementSystem.IdentityManagement;
using InternshipManagementSystem.IdentityManagement.DTOs;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.IdentityManagement;

/// <summary>
/// Editing a staff account, and what the password box actually does.
/// <para>
/// One DTO carries both creating an account and editing one, which is ABP's
/// pattern and is fine — but a password is required in the first and optional in
/// the second, and an attribute cannot say that. It said "required", so an
/// administrator correcting a phone number was answered 400 for a field the
/// screen shows as optional. And when they did type a new password, the field
/// was validated, carried the whole way through, and dropped on the floor: 200,
/// and the account kept the password it had.
/// </para>
/// <para>
/// These assert against <c>CheckPasswordAsync</c> rather than against the status
/// code, because the status code was never the thing that was wrong.
/// </para>
/// </summary>
public class StaffAccountTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IUserAppService _users;
    private readonly IdentityUserManager _userManager;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000061");

    private const string FirstPassword = "1q2w3E*";
    private const string SecondPassword = "Zx9!qwErTy";

    public StaffAccountTests()
    {
        _users = GetRequiredService<IUserAppService>();
        _userManager = GetRequiredService<IdentityUserManager>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_new_account_needs_a_password()
    {
        await AsTenantAsync(async () =>
        {
            var thrown = await Should.ThrowAsync<BusinessException>(
                async () => await _users.CreateAsync(Draft("nopass", password: null)));

            // Refused deliberately and by name. An account created with no
            // password is one nobody can sign in as, and the person who made it
            // has already moved on.
            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.UserPasswordRequired);
        });
    }

    [Fact]
    public async Task Correcting_a_detail_does_not_require_retyping_the_password()
    {
        await AsTenantAsync(async () =>
        {
            var created = await _users.CreateAsync(Draft("keeps"));

            var edited = Draft("keeps", password: null);
            edited.FullName = "اسم مُصحَّح";

            await _users.UpdateAsync(created.Id, edited);

            var user = await _userManager.GetByIdAsync(created.Id);

            user.Name.ShouldBe("اسم مُصحَّح");

            // Untouched, which is what an empty password box means to the person
            // looking at it.
            (await _userManager.CheckPasswordAsync(user, FirstPassword)).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Typing_a_new_password_actually_replaces_it()
    {
        await AsTenantAsync(async () =>
        {
            var created = await _users.CreateAsync(Draft("resets"));

            await _users.UpdateAsync(created.Id, Draft("resets", password: SecondPassword));

            var user = await _userManager.GetByIdAsync(created.Id);

            (await _userManager.CheckPasswordAsync(user, SecondPassword)).ShouldBeTrue();

            // The half that was actually broken. The old password kept working
            // while somebody was being told the new one down the phone.
            (await _userManager.CheckPasswordAsync(user, FirstPassword)).ShouldBeFalse();
        });
    }

    [Fact]
    public async Task A_number_written_with_its_country_code_fits()
    {
        await AsTenantAsync(async () =>
        {
            var draft = Draft("phone");
            draft.PhoneNumber = "+966501234567";

            var created = await _users.CreateAsync(draft);

            // Thirteen characters against a cap of ten, beside a comment that
            // said sixteen. Every number written the way people write them was
            // refused.
            created.PhoneNumber.ShouldBe("+966501234567");
        });
    }

    // ------------------------------------------------------------------ helpers

    private static CreateUpdateUserDto Draft(string code, string? password = FirstPassword) =>
        new()
        {
            UserName = code + "-staff",
            Email = code + "-staff@example.test",
            FullName = "موظّف",
            PhoneNumber = null,
            Password = password,
            Roles = [],
        };

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
