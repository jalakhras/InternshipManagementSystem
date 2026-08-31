using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.SettingManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace InternshipManagementSystem
{
    public class InternshipManagementSystemDataSeedContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly IdentityUserManager _userManager;
        private readonly IdentityRoleManager _roleManager;
        private readonly IIdentityUserRepository _userRepository;
        private readonly IIdentityRoleRepository _roleRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IPermissionManager _permissionManager;
        private readonly IPermissionDefinitionManager _permissionDefinitions;
        private readonly ISettingManager _settings;
        private readonly ICurrentTenant _currentTenant;

        public InternshipManagementSystemDataSeedContributor(
            IdentityUserManager userManager,
            IdentityRoleManager roleManager,
            IIdentityUserRepository userRepository,
            IIdentityRoleRepository roleRepository,
            IUnitOfWorkManager unitOfWorkManager,
            IGuidGenerator guidGenerator,
            PermissionManager permissionManager,
            IPermissionDefinitionManager permissionDefinitions,
            ISettingManager settings,
            ICurrentTenant currentTenant)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _guidGenerator = guidGenerator;
            _permissionManager = permissionManager;
            _permissionDefinitions = permissionDefinitions;
            _settings = settings;
            _currentTenant = currentTenant;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            using var uow = _unitOfWorkManager.Begin();

            // 1. إنشاء الأدوار الأساسية
            await CreateRoleIfNotExistsAsync("Admin");
            await GrantAdminPanelAccessToAdminRoleAsync();

            // The four roles a real organisation runs on. Admin above is the one
            // that holds everything; these are the ones that hold a job.
            await SeedAssessmentRolesAsync();

            // 2. إنشاء المستخدمين الأساسيين
            //
            // Development accounts for whoever runs the platform, and the two
            // leftover roles from the internship product this grew out of. Host
            // only, deliberately. They were never scoped, so every tenant pass
            // created another copy of each in the host — nineteen of
            // "admin@internship.com", every one of them holding Admin with a
            // password written down in this file. Scoping them stops the growth,
            // and stops the fix below from doing something worse: with roles and
            // users finally landing in the tenant they were seeded for, an
            // unscoped block would put a known-password administrator inside every
            // customer organisation.
            if (_currentTenant.Id == null)
            {
                await CreateRoleIfNotExistsAsync("Supervisor");
                await CreateRoleIfNotExistsAsync("Trainee");

                await CreateUserIfNotExistsAsync("admin@internship.com", "123456Aa@", "Admin");
                await CreateUserIfNotExistsAsync("Jassar1994@gmail.com", "123456Aa@", "Admin");
                await CreateUserIfNotExistsAsync("Supervisor@internship.com", "123456Aa@", "Supervisor");
                await CreateUserIfNotExistsAsync("Trainee@internship.com", "123456Aa@", "Trainee");
            }

            await uow.CompleteAsync();
        }

        /// <summary>
        /// The role, in the tenant being seeded.
        /// <para>
        /// <c>IdentityRole</c>'s tenant id defaults to null and nothing fills it
        /// in, so this created a <i>host</i> role however deep inside
        /// <c>ICurrentTenant.Change</c> it ran. The lookup above it then could not
        /// see what it had just written — the repository applies the multi-tenant
        /// filter, and a host row is not in a tenant's results — so every pass for
        /// every tenant created one more copy in the host and none anywhere else.
        /// That is why the database held nineteen roles named "Supervisor" and no
        /// organisation held any role but its own administrator's.
        /// </para>
        /// </summary>
        private async Task CreateRoleIfNotExistsAsync(string roleName)
        {
            var normalizedRoleName = roleName.ToUpperInvariant();
            var existingRole = await _roleRepository.FindByNormalizedNameAsync(normalizedRoleName);
            if (existingRole != null)
            {
                return; // موجود بالفعل - لا تفعل شيء
            }

            var newRole = new IdentityRole(_guidGenerator.Create(), roleName, _currentTenant.Id);
            await _roleManager.CreateAsync(newRole);
        }

        private async Task CreateUserIfNotExistsAsync(string email, string password, string roleName)
        {
            var normalizedEmail = email.ToUpperInvariant();
            var existingUser = await _userRepository.FindByNormalizedUserNameAsync(normalizedEmail);

            if (existingUser != null)
            {
                return; // موجود بالفعل - لا تفعل شيء
            }

            var user = new IdentityUser(_guidGenerator.Create(), email, email);
            (await _userManager.CreateAsync(user, password)).CheckErrors();

            var role = await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpperInvariant());
            if (role != null)
            {
                (await _userManager.AddToRoleAsync(user, role.Name)).CheckErrors();
            }
        }

        /// <summary>
        /// Grants the admin role everything this application defines.
        /// <para>
        /// Read from the definition manager rather than listed here. A hardcoded
        /// list drifts the moment a permission is added, and the failure is
        /// invisible in tests: the admin simply gets a 403 on a screen that was
        /// working yesterday. That is exactly what happened — the seeder granted
        /// only Administration.Access, so every assessment screen returned 403
        /// and its loader never resolved.
        /// </para>
        /// <para>
        /// Scoped to this application's own group. ABP's own permissions
        /// (identity, tenant management, feature management) are seeded by their
        /// own modules, and granting them from here would silently override a
        /// deliberate revocation.
        /// </para>
        /// <para>
        /// Each permission is granted <i>once</i>, and the names already offered
        /// are remembered. Re-granting on every start looked idempotent and was
        /// not: an administrator who deliberately took Results.Export away from
        /// the admin role would find it back after the next deployment, with
        /// nothing to explain why. ABP's store cannot tell "revoked" from "never
        /// granted" — both are simply the absence of a row — so the record has to
        /// be kept here.
        /// </para>
        /// </summary>
        private async Task GrantAdminPanelAccessToAdminRoleAsync()
        {
            var adminRole = await _roleRepository.FindByNormalizedNameAsync("ADMIN");
            if (adminRole == null)
            {
                return;
            }

            var groups = await _permissionDefinitions.GetGroupsAsync();

            var ours = groups
                .Where(group => group.Name == InternshipManagementSystemPermissions.GroupName)
                .SelectMany(group => group.GetPermissionsWithChildren())
                .Select(permission => permission.Name)
                .ToList();

            // Per tenant, because seeding runs per tenant and the grants it writes
            // are tenant-scoped. Held globally this marker was filled by the host's
            // pass and then read as "already done" by every tenant after it — so
            // the second organisation on a deployment got an Admin role with no
            // permission at all, and every screen returned 403. That is the exact
            // failure this method was written to fix, reintroduced one level down.
            var alreadyOffered = (await _settings.GetOrNullForCurrentTenantAsync(
                    Settings.InternshipManagementSystemSettings.SeededPermissions,
                    fallback: false) ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet();

            // What the role actually holds, asked of the store rather than assumed
            // from the marker.
            //
            // Creating an organisation from the screen goes through ABP's tenant
            // management, which seeds the new organisation's administrator with
            // every permission on the deployment — ours included — before this
            // contributor runs. Granting them a second time inserts a row that
            // already exists, and the unique index on
            // (TenantId, Name, ProviderName, ProviderKey) refuses it: the whole
            // creation was rolled back with a SQL error, so **no organisation
            // could be created from the host screen at all**. The marker could
            // not see that, because a marker records what this code has done and
            // the duplicate came from somebody else's code.
            // Flushed first, or the reading below cannot see the writing above it.
            // ABP's own seeding runs in this same unit of work and its grants are
            // still in the change tracker; a LINQ query goes to the database and
            // returns rows that do not include them, so "already granted" reads
            // as false for every one of them.
            if (_unitOfWorkManager.Current is { } current)
            {
                await current.SaveChangesAsync();
            }

            var alreadyHeld = (await _permissionManager.GetAllForRoleAsync(adminRole.Name))
                .Where(permission => permission.IsGranted)
                .Select(permission => permission.Name)
                .ToHashSet();

            var newlyDefined = ours
                .Where(name => !alreadyOffered.Contains(name) && !alreadyHeld.Contains(name))
                .ToList();

            foreach (var name in newlyDefined)
            {
                await _permissionManager.SetForRoleAsync(adminRole.Name, name, true);
            }

            // Everything considered on this pass is marked, not only what was
            // written. A permission the organisation already held has been
            // offered; recording it is what stops the next deployment from
            // offering it again and overruling a deliberate revocation.
            var considered = ours.Where(name => !alreadyOffered.Contains(name)).ToList();

            if (considered.Count > 0)
            {
                // Written after granting, so a failure half way means the rest are
                // offered again on the next run rather than lost.
                await _settings.SetForCurrentTenantAsync(
                    Settings.InternshipManagementSystemSettings.SeededPermissions,
                    string.Join(',', alreadyOffered.Concat(considered).Distinct().OrderBy(n => n)));
            }
        }

        // ------------------------------------------------------------- the roles

        /// <summary>
        /// The roles an assessment organisation actually runs on, and what each holds.
        /// <para>
        /// Until now the only role was <c>Admin</c>, which holds everything, so no
        /// permission in this product had ever been exercised as a restriction. A
        /// permission that is only ever granted is not a permission; it is a
        /// checkbox. These four are the sets that make the tree mean something.
        /// </para>
        /// <para>
        /// Leaves only, and the ancestors are added by <see cref="WithAncestorsAsync"/>.
        /// That is not cosmetic: ASP.NET combines a service's class-level
        /// <c>[Authorize]</c> with its method-level one using AND, so
        /// <c>Questions.Create</c> without <c>Questions</c> describes a role that
        /// looks correct in the permission screen and is refused on every request.
        /// </para>
        /// <para>
        /// Role names are English identifiers, deliberately. ABP's
        /// <c>IdentityRole</c> has a name and no display name, so a role's name is
        /// what the permission store keys on, what the user screen renders, and
        /// what a script or a test writes down. The Arabic names each of these goes
        /// by — منسّق, مُعِدّ الاختبارات, مصحّح, مشاهد النتائج — belong in the
        /// localisation resource beside every other label, not in a database key.
        /// The reasoning for each set is in <c>docs/business/roles.md</c>.
        /// </para>
        /// </summary>
        private static readonly Dictionary<string, string[]> AssessmentRoles = new()
        {
            // منسّق — runs sittings. Reads the exam catalogue in order to send an
            // exam; never writes one. Holds SendEmail because the coordinator is
            // the person who sends, and ForceSubmit because closing out a sitting
            // that hung is what the attempt monitor is for. Not Attempts.Delete:
            // destroying a sitting destroys the evidence of it.
            ["Coordinator"] = new[]
            {
                InternshipManagementSystemPermissions.Exams.View,

                InternshipManagementSystemPermissions.Candidates.View,
                InternshipManagementSystemPermissions.Candidates.Create,
                InternshipManagementSystemPermissions.Candidates.Edit,
                InternshipManagementSystemPermissions.Candidates.Delete,

                InternshipManagementSystemPermissions.Groups.View,
                InternshipManagementSystemPermissions.Groups.Create,
                InternshipManagementSystemPermissions.Groups.Edit,
                InternshipManagementSystemPermissions.Groups.Delete,

                InternshipManagementSystemPermissions.Assignments.View,
                InternshipManagementSystemPermissions.Assignments.Create,
                InternshipManagementSystemPermissions.Assignments.Revoke,
                InternshipManagementSystemPermissions.Assignments.SendEmail,

                InternshipManagementSystemPermissions.Attempts.View,
                InternshipManagementSystemPermissions.Attempts.ForceSubmit,

                InternshipManagementSystemPermissions.Results.View,
                InternshipManagementSystemPermissions.Results.Export,

                // Read-only. A class is created against a category and a level, so
                // the coordinator has to be able to read the vocabulary; deciding
                // what the vocabulary is belongs to whoever writes the exams.
                InternshipManagementSystemPermissions.Catalog.View,
            },

            // مُعِدّ الاختبارات — writes the assessment and nothing else. No
            // candidates, no assignments, no results: an author who can see who
            // scored what on the question they wrote is an author with a reason to
            // change the question after the fact.
            ["Author"] = new[]
            {
                InternshipManagementSystemPermissions.Exams.View,
                InternshipManagementSystemPermissions.Exams.Create,
                InternshipManagementSystemPermissions.Exams.Edit,
                InternshipManagementSystemPermissions.Exams.Delete,
                InternshipManagementSystemPermissions.Exams.Publish,

                InternshipManagementSystemPermissions.Questions.View,
                InternshipManagementSystemPermissions.Questions.Create,
                InternshipManagementSystemPermissions.Questions.Edit,
                InternshipManagementSystemPermissions.Questions.Delete,

                // The tenant's own vocabulary is the author's tool: a question is
                // tagged to a topic and an exam sits at a level.
                InternshipManagementSystemPermissions.Catalog.View,
                InternshipManagementSystemPermissions.Catalog.Manage,
            },

            // مصحّح — the review queue and nothing else.
            //
            // ViewIntegritySignals is granted, and it is the one judgement call in
            // this table. The marker is the only person who reads a free-text
            // answer and decides whether it is the candidate's own work; a paste
            // event on a 400-word essay is the single most relevant fact to that
            // decision, and the report is scoped to the one attempt they already
            // have open rather than being a browsable record of anybody's
            // behaviour. Withholding it does not protect the candidate — it makes
            // the mark worse and sends the marker to ask a coordinator who has not
            // read the answer. The permission stays separate so an organisation
            // running low-stakes practice can take it back without touching
            // grading, which is exactly what a separate permission is worth.
            ["Marker"] = new[]
            {
                InternshipManagementSystemPermissions.Review.ViewQueue,
                InternshipManagementSystemPermissions.Review.Grade,
                InternshipManagementSystemPermissions.Review.ViewIntegritySignals,
            },

            // مشاهد النتائج — reads and exports, writes nothing.
            //
            // Exams.View is here because both results screens begin by asking which
            // exam: the roster's filter and the item analysis both load the exam
            // list, and item analysis is addressed by exam id and unreachable
            // without one. It grants the exam's shape, not its questions — those
            // are behind Questions.Default, which the observer does not hold.
            ["Observer"] = new[]
            {
                InternshipManagementSystemPermissions.Exams.View,

                InternshipManagementSystemPermissions.Results.View,
                InternshipManagementSystemPermissions.Results.Export,
                InternshipManagementSystemPermissions.Results.ViewItemAnalysis,
            },
        };

        /// <summary>
        /// Creates the roles above and offers each its permissions exactly once.
        /// <para>
        /// The once-only property is the same one
        /// <see cref="GrantAdminPanelAccessToAdminRoleAsync"/> exists to hold, and
        /// it matters more here, not less: these roles are the ones a customer will
        /// actually tune. An organisation that decides its coordinators may not
        /// export a roster of named people must be able to untick
        /// <c>Results.Export</c> and have it stay unticked. ABP's store cannot tell
        /// "revoked" from "never granted" — both are the absence of a row — so a
        /// re-grant on every deployment would quietly overrule them, and nothing in
        /// the product would say why.
        /// </para>
        /// <para>
        /// A role that already exists is used as it is rather than recreated; only
        /// the grants it has never been offered are written.
        /// </para>
        /// </summary>
        private async Task SeedAssessmentRolesAsync()
        {
            // Per tenant, for the reason recorded on the admin marker: seeding runs
            // per tenant and the grants it writes are tenant-scoped, so a marker
            // held globally would let the first organisation's pass be read as
            // "already done" by every organisation after it.
            // `fallback: false`, so an organisation reads its own marker. With the
            // fallback ABP applies by default, every organisation read the host's
            // — which is filled — and concluded that all its grants had already
            // been offered, so it was seeded with roles holding nothing. That is
            // the failure the comment above warns about, arriving through the
            // default value of an argument rather than through the key.
            var alreadyOffered = (await _settings.GetOrNullForCurrentTenantAsync(
                    Settings.InternshipManagementSystemSettings.SeededRolePermissions,
                    fallback: false) ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet();

            var newlyOffered = new List<string>();

            foreach (var (roleName, leaves) in AssessmentRoles)
            {
                await CreateRoleIfNotExistsAsync(roleName);

                var role = await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpperInvariant());

                if (role == null)
                {
                    continue;
                }

                // What this role already holds, for the reason recorded on the
                // administrator's grants: a permission granted by somebody else's
                // seeding cannot be granted again, and the unique index turns the
                // attempt into a failed creation rather than a warning.
                if (_unitOfWorkManager.Current is { } pending)
                {
                    await pending.SaveChangesAsync();
                }

                var alreadyHeld = (await _permissionManager.GetAllForRoleAsync(role.Name))
                    .Where(permission => permission.IsGranted)
                    .Select(permission => permission.Name)
                    .ToHashSet();

                foreach (var permission in await WithAncestorsAsync(leaves))
                {
                    // Role and permission together, because two roles legitimately
                    // hold the same permission and a flat list of names would read
                    // the first one's grant as covering the second.
                    var record = roleName + ':' + permission;

                    if (alreadyOffered.Contains(record))
                    {
                        continue;
                    }

                    if (!alreadyHeld.Contains(permission))
                    {
                        await _permissionManager.SetForRoleAsync(role.Name, permission, true);
                    }

                    newlyOffered.Add(record);
                }
            }

            if (newlyOffered.Count > 0)
            {
                // Written after granting, so a failure half way through means the
                // rest are offered again on the next run rather than lost.
                await _settings.SetForCurrentTenantAsync(
                    Settings.InternshipManagementSystemSettings.SeededRolePermissions,
                    string.Join(',', alreadyOffered.Concat(newlyOffered).Distinct().OrderBy(n => n)));
            }
        }

        /// <summary>
        /// Each of these permissions plus every permission it hangs from, ordered
        /// parent-first and de-duplicated.
        /// <para>
        /// Read from the definition tree rather than by splitting on dots, because
        /// the dots lie: <c>Assessment.IdentityManagement.Users.View</c> has three
        /// dotted prefixes and only two of them are permissions, and the third is
        /// the group name. The tree knows which is which.
        /// </para>
        /// <para>
        /// A name that is not in the tree throws rather than being skipped. Silence
        /// here would seed a role with a hole in it — the sort of defect that shows
        /// up as one screen returning 403 for one role, months later. The names in
        /// the table are compile-time constants, so the only way to reach this is
        /// to delete a permission from the definition provider and leave it in a
        /// role, which is precisely the moment somebody should be told.
        /// </para>
        /// </summary>
        private async Task<List<string>> WithAncestorsAsync(IEnumerable<string> permissions)
        {
            var groups = await _permissionDefinitions.GetGroupsAsync();

            var byName = groups
                .Where(group => group.Name == InternshipManagementSystemPermissions.GroupName)
                .SelectMany(group => group.GetPermissionsWithChildren())
                .ToDictionary(permission => permission.Name);

            var expanded = new List<string>();

            foreach (var name in permissions)
            {
                if (!byName.TryGetValue(name, out var definition))
                {
                    throw new InvalidOperationException(
                        $"The role table names \"{name}\", which the permission definition provider "
                        + "does not define. Either define it or take it out of the role — a role "
                        + "seeded against a permission that does not exist is a role with a "
                        + "silent hole in it.");
                }

                // Parent-first, so the permission screen reads top-down and a
                // half-written grant leaves the parent rather than the child.
                var chain = new List<string>();

                for (var node = definition; node != null; node = node.Parent)
                {
                    chain.Insert(0, node.Name);
                }

                expanded.AddRange(chain);
            }

            return expanded.Distinct().ToList();
        }
    }
}