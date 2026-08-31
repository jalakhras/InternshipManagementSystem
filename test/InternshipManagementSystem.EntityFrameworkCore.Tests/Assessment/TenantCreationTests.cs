using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Permissions;
using Shouldly;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TenantManagement;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Adding an organisation from the screen that exists to add organisations.
/// <para>
/// It threw. Every time, on the first one as well as the second: a SQL error
/// from a unique index, and the whole creation rolled back. The screen was
/// written so that the product could be sold to a second customer without an
/// engineer present, and it could not add a first.
/// </para>
/// <para>
/// The cause is worth recording, because it is not visible in either piece of
/// code alone. Creating a tenant through ABP runs every seed contributor in one
/// unit of work. ABP's own identity seeding grants the new organisation's
/// administrator every permission on the deployment, and those rows are still
/// in the change tracker when ours runs. Ours asked the store what the role
/// already held; a query goes to the database, the database had not been
/// written yet, and the answer came back "nothing" — so it granted them again,
/// and the index on (TenantId, Name, ProviderName, ProviderKey) refused the
/// duplicate.
/// </para>
/// <para>
/// No test had ever created a tenant. The three organisations the live suite
/// runs against are seeded by the migrator, which takes a different path, so
/// the one path a customer's first day goes through was the one path nothing
/// exercised.
/// </para>
/// </summary>
public class TenantCreationTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ITenantAppService _tenants;
    private readonly ICurrentTenant _currentTenant;

    public TenantCreationTests()
    {
        _tenants = GetRequiredService<ITenantAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task An_organisation_can_be_added_from_the_screen()
    {
        var created = await CreateAsync("first-customer");

        created.Name.ShouldBe("first-customer");
    }

    [Fact]
    public async Task And_then_a_second_one()
    {
        await CreateAsync("customer-one");

        // The sentence the screen was built for. A deployment that can hold one
        // organisation is not multi-tenant, it is an installation.
        var second = await CreateAsync("customer-two");

        second.Name.ShouldBe("customer-two");
    }

    [Fact]
    public async Task A_new_organisations_administrator_can_actually_use_it()
    {
        var tenant = await CreateAsync("usable-day-one");

        using (_currentTenant.Change(tenant.Id))
        {
            var granted = await WithUnitOfWorkAsync(async () =>
            {
                var permissions = GetRequiredService<IPermissionManager>();
                var roles = GetRequiredService<IIdentityRoleRepository>();

                var admin = await roles.FindByNormalizedNameAsync("ADMIN");

                admin.ShouldNotBeNull();

                return (await permissions.GetAllForRoleAsync(admin!.Name))
                    .Where(permission => permission.IsGranted)
                    .Select(permission => permission.Name)
                    .ToHashSet();
            });

            // An organisation created with an administrator who holds nothing is
            // an empty shell: every screen answers 403, and the person who just
            // paid for the product cannot open the first page of it.
            granted.ShouldContain(InternshipManagementSystemPermissions.Exams.Create);
            granted.ShouldContain(InternshipManagementSystemPermissions.Candidates.View);
            granted.ShouldContain(InternshipManagementSystemPermissions.Assignments.Create);
        }
    }

    [Fact]
    public async Task A_new_organisation_gets_the_four_roles_a_centre_runs_on()
    {
        var tenant = await CreateAsync("four-roles");

        using (_currentTenant.Change(tenant.Id))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var roles = GetRequiredService<IIdentityRoleRepository>();

                // Inside the organisation, not in the host. A role seeded into the
                // host is invisible to the organisation it was meant for, and the
                // organisation is left with one administrator and no way to give
                // anybody a job.
                foreach (var name in new[] { "Coordinator", "Author", "Marker", "Observer" })
                {
                    var role = await roles.FindByNormalizedNameAsync(name.ToUpperInvariant());

                    role.ShouldNotBeNull();
                    role!.TenantId.ShouldBe(tenant.Id);
                }
            });
        }
    }

    [Fact]
    public async Task A_coordinator_in_a_new_organisation_holds_a_coordinators_permissions()
    {
        var tenant = await CreateAsync("coordinator-works");

        using (_currentTenant.Change(tenant.Id))
        {
            var granted = await WithUnitOfWorkAsync(async () =>
            {
                var permissions = GetRequiredService<IPermissionManager>();

                return (await permissions.GetAllForRoleAsync("Coordinator"))
                    .Where(permission => permission.IsGranted)
                    .Select(permission => permission.Name)
                    .ToHashSet();
            });

            // The marker that records what has been offered is read per
            // organisation. Read with ABP's default fallback it returned the
            // host's — which is full — so a new organisation's roles were seeded
            // with nothing at all while the code believed it had already done the
            // work.
            granted.ShouldContain(InternshipManagementSystemPermissions.Assignments.Create);
            granted.ShouldContain(InternshipManagementSystemPermissions.Candidates.Create);

            // And not everything: a coordinator who can write exams is not a
            // coordinator, and a role that holds all of it is the administrator
            // under another name.
            granted.ShouldNotContain(InternshipManagementSystemPermissions.Exams.Create);
        }
    }

    private async Task<TenantDto> CreateAsync(string name) =>
        await WithUnitOfWorkAsync(async () => await _tenants.CreateAsync(new TenantCreateDto
        {
            Name = name,
            AdminEmailAddress = name + "@example.test",
            AdminPassword = "1q2w3E*",
        }));
}
