using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.IdentityManagement;
using InternshipManagementSystem.IdentityManagement.DTOs;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.IdentityManagement;

/// <summary>
/// An account's roles have to belong to the same organisation the account does.
/// <para>
/// A role carries a tenant id, and ABP resolves a user's roles through the
/// multi-tenant filter. So a user linked to a role from a <i>different</i>
/// organisation has, as far as the application is concerned, no roles at all —
/// and no permissions.
/// </para>
/// <para>
/// The failure is completely silent, which is what makes it worth a test. The
/// account signs in perfectly. It lands on the one page that needs no
/// permission. Every menu entry is hidden. Nothing throws, nothing is logged,
/// and the database looks right if you read it without joining on the tenant —
/// the user has a role called "admin", and that role holds sixty-five grants. It
/// reads exactly like "this account was never given anything", which is the one
/// explanation that is wrong.
/// </para>
/// <para>
/// It happened here to two host administrators, from when the seeder created
/// roles with a null tenant id and then looked them up by name across every
/// organisation. tools/repair-cross-tenant-roles.sql repoints such rows; this is
/// the guard that the code no longer writes them.
/// </para>
/// </summary>
public class RoleTenancyTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IUserAppService _users;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid TenantA = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("dddddddd-0000-0000-0000-000000000002");

    public RoleTenancyTests()
    {
        _users = GetRequiredService<IUserAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_role_created_in_one_organisation_belongs_to_it()
    {
        var roles = GetRequiredService<IRepository<IdentityRole, Guid>>();

        foreach (var tenant in new[] { TenantA, TenantB })
        {
            using (_currentTenant.Change(tenant))
            {
                await WithUnitOfWorkAsync(async () =>
                {
                    var role = new IdentityRole(Guid.NewGuid(), "Shared", tenant);

                    await roles.InsertAsync(role, autoSave: true);
                });
            }
        }

        // Two organisations may both have a role called "Shared" and they are two
        // different roles. IdentityRole's tenant id defaults to null and nothing
        // fills it in, so a role created inside ICurrentTenant.Change is a *host*
        // role unless the id is passed — which is how the ones this test guards
        // against came to exist.
        using (_currentTenant.Change(TenantA))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var mine = await (await roles.GetQueryableAsync())
                    .Where(r => r.Name == "Shared")
                    .ToListAsync();

                mine.Count.ShouldBe(1);
                mine.Single().TenantId.ShouldBe(TenantA);
            });
        }
    }

    [Fact]
    public async Task An_account_gets_its_own_organisations_role_and_can_use_it()
    {
        var roles = GetRequiredService<IRepository<IdentityRole, Guid>>();
        var userManager = GetRequiredService<IdentityUserManager>();

        // The same role name in both, so a lookup that ignores the tenant has
        // something wrong to find.
        foreach (var tenant in new[] { TenantA, TenantB })
        {
            using (_currentTenant.Change(tenant))
            {
                await WithUnitOfWorkAsync(async () =>
                    await roles.InsertAsync(
                        new IdentityRole(Guid.NewGuid(), "Marker", tenant), autoSave: true));
            }
        }

        Guid userId = default;

        using (_currentTenant.Change(TenantA))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var created = await _users.CreateAsync(new CreateUpdateUserDto
                {
                    UserName = "tenancy-marker",
                    Email = "tenancy-marker@example.test",
                    FullName = "مصحّح",
                    Password = "1q2w3E*",
                    Roles = ["Marker"],
                });

                userId = created.Id;
            });

            await WithUnitOfWorkAsync(async () =>
            {
                var user = await userManager.GetByIdAsync(userId);
                var held = await userManager.GetRolesAsync(user);

                // The assertion that matters. Reading the join row is not enough:
                // the link existed in the broken case too. What was missing was
                // the role being visible to the user's own tenant, and this is
                // the only way to ask that question.
                held.ShouldContain("Marker");
            });
        }
    }
}
