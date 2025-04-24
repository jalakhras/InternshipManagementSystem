namespace InternshipManagementSystem.Permissions;

public static class InternshipManagementSystemPermissions
{
    public const string GroupName = "InternshipManagementSystem";

    public static class TrainingManagement
    {
        public const string Default = GroupName + ".TrainingManagement";

        public static class Exams
        {
            public const string Default = TrainingManagement.Default + ".Exams";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
        }

        public static class Questions
        {
            public const string Default = TrainingManagement.Default + ".Questions";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
        }

        public static class ExamAnswers
        {
            public const string Default = TrainingManagement.Default + ".ExamAnswers";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
        }

        public static class ExamAttempts
        {
            public const string Default = TrainingManagement.Default + ".ExamAttempts";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
        }

        public static class Candidates
        {
            public const string Default = TrainingManagement.Default + ".Candidates";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
        }

        public static class CandidateExamAttempts
        {
            public const string Default = TrainingManagement.Default + ".CandidateExamAttempts";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";

        }
        public static class CandidateExamAnswers
        {
            public const string Default = TrainingManagement.Default + ".CandidateExamAnswers";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
        }


        public static class ManualGrading
        {
            public const string Default = TrainingManagement.Default + ".ManualGrading";
            public const string View = Default + ".View";
            public const string Edit = Default + ".Edit";
        }

    }

    public static class IdentityManagement
    {
        public const string Default = GroupName + ".IdentityManagement";

        public static class Users
        {
            public const string Default = IdentityManagement.Default + ".Users";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
            public const string ManageRoles = Default + ".ManageRoles";
        }

        public static class Roles
        {
            public const string Default = IdentityManagement.Default + ".Roles";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
        }
    }
}