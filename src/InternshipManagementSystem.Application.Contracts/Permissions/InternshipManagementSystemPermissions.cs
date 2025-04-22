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
    }
}