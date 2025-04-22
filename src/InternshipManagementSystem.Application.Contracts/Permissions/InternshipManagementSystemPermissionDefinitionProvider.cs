using InternshipManagementSystem.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace InternshipManagementSystem.Permissions;

public class InternshipManagementSystemPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(InternshipManagementSystemPermissions.GroupName, L("Permission:InternshipManagementSystem"));

        var trainingManagementGroup = myGroup.AddPermission(
            InternshipManagementSystemPermissions.TrainingManagement.Default,
            L("Permission:TrainingManagement")
        );

        // Exams Permissions
        var exams = trainingManagementGroup.AddChild(
            InternshipManagementSystemPermissions.TrainingManagement.Exams.Default,
            L("Permission:Exams")
        );

        exams.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Exams.Create, L("Permission:Exams.Create"));
        exams.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Exams.Update, L("Permission:Exams.Update"));
        exams.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Exams.Delete, L("Permission:Exams.Delete"));
        exams.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Exams.View, L("Permission:Exams.View"));

        // Questions Permissions
        var questions = trainingManagementGroup.AddChild(
            InternshipManagementSystemPermissions.TrainingManagement.Questions.Default,
            L("Permission:Questions")
        );

        questions.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.Create, L("Permission:Questions.Create"));
        questions.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.Update, L("Permission:Questions.Update"));
        questions.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.Delete, L("Permission:Questions.Delete"));
        questions.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.View, L("Permission:Questions.View"));

        // Exam Answers Permissions
        var examAnswers = trainingManagementGroup.AddChild(
            InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Default,
            L("Permission:ExamAnswers")
        );

        examAnswers.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Create, L("Permission:ExamAnswers.Create"));
        examAnswers.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Update, L("Permission:ExamAnswers.Update"));
        examAnswers.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.Delete, L("Permission:ExamAnswers.Delete"));
        examAnswers.AddChild(InternshipManagementSystemPermissions.TrainingManagement.ExamAnswers.View, L("Permission:ExamAnswers.View"));
        var examAttempts = trainingManagementGroup.AddChild(
            InternshipManagementSystemPermissions.TrainingManagement.Exams.Default,
            L("Permission:ExamAttempts")
        );
        examAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.Create, L("Permission:ExamAttempts.Create"));
        examAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.Update, L("Permission:ExamAttempts.Update"));
        examAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.Delete, L("Permission:ExamAttempts.Delete"));
        examAttempts.AddChild(InternshipManagementSystemPermissions.TrainingManagement.Questions.View, L("Permission:ExamAttempts.View"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<InternshipManagementSystemResource>(name);
    }
}