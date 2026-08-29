using InternshipManagementSystem.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace InternshipManagementSystem.Permissions;

public class InternshipManagementSystemPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup(InternshipManagementSystemPermissions.GroupName, L("Permission:Assessment"));

        var exams = group.AddPermission(InternshipManagementSystemPermissions.Exams.Default, L("Permission:Exams"));
        exams.AddChild(InternshipManagementSystemPermissions.Exams.View, L("Permission:View"));
        exams.AddChild(InternshipManagementSystemPermissions.Exams.Create, L("Permission:Create"));
        exams.AddChild(InternshipManagementSystemPermissions.Exams.Edit, L("Permission:Edit"));
        exams.AddChild(InternshipManagementSystemPermissions.Exams.Delete, L("Permission:Delete"));
        exams.AddChild(InternshipManagementSystemPermissions.Exams.Publish, L("Permission:Publish"));

        var questions = group.AddPermission(InternshipManagementSystemPermissions.Questions.Default, L("Permission:Questions"));
        questions.AddChild(InternshipManagementSystemPermissions.Questions.View, L("Permission:View"));
        questions.AddChild(InternshipManagementSystemPermissions.Questions.Create, L("Permission:Create"));
        questions.AddChild(InternshipManagementSystemPermissions.Questions.Edit, L("Permission:Edit"));
        questions.AddChild(InternshipManagementSystemPermissions.Questions.Delete, L("Permission:Delete"));

        var candidates = group.AddPermission(InternshipManagementSystemPermissions.Candidates.Default, L("Permission:Candidates"));
        candidates.AddChild(InternshipManagementSystemPermissions.Candidates.View, L("Permission:View"));
        candidates.AddChild(InternshipManagementSystemPermissions.Candidates.Create, L("Permission:Create"));
        candidates.AddChild(InternshipManagementSystemPermissions.Candidates.Edit, L("Permission:Edit"));
        candidates.AddChild(InternshipManagementSystemPermissions.Candidates.Delete, L("Permission:Delete"));

        var groups = group.AddPermission(InternshipManagementSystemPermissions.Groups.Default, L("Permission:Groups"));
        groups.AddChild(InternshipManagementSystemPermissions.Groups.View, L("Permission:View"));
        groups.AddChild(InternshipManagementSystemPermissions.Groups.Create, L("Permission:Create"));
        groups.AddChild(InternshipManagementSystemPermissions.Groups.Edit, L("Permission:Edit"));
        groups.AddChild(InternshipManagementSystemPermissions.Groups.Delete, L("Permission:Delete"));

        var assignments = group.AddPermission(InternshipManagementSystemPermissions.Assignments.Default, L("Permission:Assignments"));
        assignments.AddChild(InternshipManagementSystemPermissions.Assignments.View, L("Permission:View"));
        assignments.AddChild(InternshipManagementSystemPermissions.Assignments.Create, L("Permission:Create"));
        assignments.AddChild(InternshipManagementSystemPermissions.Assignments.Revoke, L("Permission:Revoke"));
        assignments.AddChild(InternshipManagementSystemPermissions.Assignments.SendEmail, L("Permission:SendEmail"));

        var attempts = group.AddPermission(InternshipManagementSystemPermissions.Attempts.Default, L("Permission:Attempts"));
        attempts.AddChild(InternshipManagementSystemPermissions.Attempts.View, L("Permission:View"));
        attempts.AddChild(InternshipManagementSystemPermissions.Attempts.ForceSubmit, L("Permission:ForceSubmit"));
        attempts.AddChild(InternshipManagementSystemPermissions.Attempts.Delete, L("Permission:Delete"));

        var review = group.AddPermission(InternshipManagementSystemPermissions.Review.Default, L("Permission:Review"));
        review.AddChild(InternshipManagementSystemPermissions.Review.ViewQueue, L("Permission:ViewQueue"));
        review.AddChild(InternshipManagementSystemPermissions.Review.Grade, L("Permission:Grade"));
        review.AddChild(InternshipManagementSystemPermissions.Review.ViewIntegritySignals, L("Permission:ViewIntegritySignals"));

        var results = group.AddPermission(InternshipManagementSystemPermissions.Results.Default, L("Permission:Results"));

        // Export and item analysis nest under View for the same reason the
        // catalogue's Manage does: the service demands View at class level and the
        // finer permission at method level, and the two combine with AND. Beside
        // each other they would describe roles that cannot work.
        var resultsView = results.AddChild(
            InternshipManagementSystemPermissions.Results.View, L("Permission:View"));

        resultsView.AddChild(InternshipManagementSystemPermissions.Results.Export, L("Permission:Export"));
        resultsView.AddChild(
            InternshipManagementSystemPermissions.Results.ViewItemAnalysis, L("Permission:ViewItemAnalysis"));

        var catalog = group.AddPermission(InternshipManagementSystemPermissions.Catalog.Default, L("Permission:Catalog"));

        // Manage sits under View rather than beside it, because the service
        // requires both: ASP.NET combines the class and method attributes with
        // AND. Nested, ABP's permission screen grants the parent along with the
        // child, so "let this person maintain the catalogue" cannot be expressed
        // as a role that then fails on every request.
        var catalogView = catalog.AddChild(
            InternshipManagementSystemPermissions.Catalog.View, L("Permission:View"));

        catalogView.AddChild(InternshipManagementSystemPermissions.Catalog.Manage, L("Permission:Manage"));

        var identity = group.AddPermission(
            InternshipManagementSystemPermissions.IdentityManagement.Default, L("Permission:Users"));

        // The service authorises against Users.Default at class level, so it has to
        // exist as a policy in its own right. Hanging the four children directly off
        // the group left that name undefined, and ASP.NET answers an undefined
        // policy with a 500 rather than a 403 — so the screen looked broken rather
        // than forbidden.
        var users = identity.AddChild(
            InternshipManagementSystemPermissions.IdentityManagement.Users.Default, L("Permission:Users"));

        users.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Users.View, L("Permission:View"));
        users.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Users.Create, L("Permission:Create"));
        users.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Users.Edit, L("Permission:Edit"));
        users.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Users.Delete, L("Permission:Delete"));
        users.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Users.ManageRoles, L("Permission:ManageRoles"));

        var administration = group.AddPermission(InternshipManagementSystemPermissions.Administration.Default, L("Permission:Administration"));
        administration.AddChild(InternshipManagementSystemPermissions.Administration.ManageSettings, L("Permission:ManageSettings"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<InternshipManagementSystemResource>(name);
    }
}
