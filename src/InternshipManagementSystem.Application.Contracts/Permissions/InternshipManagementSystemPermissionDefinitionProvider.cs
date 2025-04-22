using InternshipManagementSystem.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace InternshipManagementSystem.Permissions;

public class InternshipManagementSystemPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(InternshipManagementSystemPermissions.GroupName, L("InternshipManagementSystem"));

        // Training Management
        var trainingManagementGroup = myGroup.AddPermission(
            InternshipManagementSystemPermissions.TrainingManagement.Default,
            L("TrainingManagement"));

        // Exams
        var exams = trainingManagementGroup.AddChild(
            InternshipManagementSystemPermissions.TrainingManagement.Exams.Default,
            L("Exams"));
        exams.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Exams.Create, L("Create"));
        exams.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Exams.Edit, L("Edit"));
        exams.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Exams.Delete, L("Delete"));
        exams.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Exams.View, L("View"));

        // Questions
        var questions = trainingManagementGroup.AddChild(
            InternshipManagementSystemPermissions.TrainingManagement.Questions.Default,
            L("Questions"));
        questions.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.Create, L("Create"));
        questions.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.Edit, L("Edit"));
        questions.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.Delete, L("Delete"));
        questions.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.View, L("View"));

        // Exam Answers
        var examAnswers = trainingManagementGroup.AddChild(
            InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Default,
            L("ExamAnswers"));
        examAnswers.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Create, L("Create"));
        examAnswers.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Edit, L("Edit"));
        examAnswers.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Delete, L("Delete"));
        examAnswers.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.View, L("View"));

        // Exam Attempts
        var examAttempts = trainingManagementGroup.AddChild(
            InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.Default,
            L("ExamAttempts"));
        examAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.Create, L("Create"));
        examAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.Edit, L("Edit"));
        examAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.Delete, L("Delete"));
        examAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAttempts.View, L("View"));

        // Candidates
        var candidates = trainingManagementGroup.AddChild(
            InternshipManagementSystemPermissions.TrainingManagement.Candidates.Default,
            L("Candidates"));
        candidates.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Candidates.Create, L("Create"));
        candidates.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Candidates.Edit, L("Edit"));
        candidates.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Candidates.Delete, L("Delete"));
        candidates.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Candidates.View, L("View"));

        // Candidate Exam Attempts
        var candidateExamAttempts = trainingManagementGroup.AddChild(
            InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Default,
            L("CandidateExamAttempts"));
        candidateExamAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Create, L("Create"));
        candidateExamAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Edit, L("Edit"));
        candidateExamAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Delete, L("Delete"));
        candidateExamAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.View, L("View"));

        // Identity Management
        var identityManagementGroup = myGroup.AddPermission(
            InternshipManagementSystemPermissions.IdentityManagement.Default,
            L("IdentityManagement"));

        var users = identityManagementGroup.AddChild(
            InternshipManagementSystemPermissions.IdentityManagement.Users.Default,
            L("Users"));
        users.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Users.Create, L("Create"));
        users.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Users.Edit, L("Edit"));
        users.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Users.Delete, L("Delete"));
        users.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Users.View, L("View"));
        users.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Users.ManageRoles, L("ManageRoles"));

        var roles = identityManagementGroup.AddChild(
            InternshipManagementSystemPermissions.IdentityManagement.Roles.Default,
            L("Roles"));
        roles.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Roles.Create, L("Create"));
        roles.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Roles.Edit, L("Edit"));
        roles.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Roles.Delete, L("Delete"));
        roles.AddChild(InternshipManagementSystemPermissions.IdentityManagement.Roles.View, L("View"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<InternshipManagementSystemResource>(name);
    }
}
