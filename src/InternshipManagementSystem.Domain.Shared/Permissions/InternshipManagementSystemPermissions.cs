namespace InternshipManagementSystem.Permissions;

/// <summary>
/// Permission names, grouped by what a person does rather than by table.
/// A role is a bundle of these; see the definition provider for the tree.
/// </summary>
public static class InternshipManagementSystemPermissions
{
    public const string GroupName = "Assessment";

    /// <summary>Authoring exams, question groups and the blueprint.</summary>
    public static class Exams
    {
        public const string Default = GroupName + ".Exams";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";

        /// <summary>Moving an exam from draft to assignable. Separate from Edit on purpose.</summary>
        public const string Publish = Default + ".Publish";
    }

    /// <summary>The question bank. Answer keys live here, so View is a real privilege.</summary>
    public static class Questions
    {
        public const string Default = GroupName + ".Questions";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    /// <summary>The people being assessed.</summary>
    public static class Candidates
    {
        public const string Default = GroupName + ".Candidates";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    /// <summary>Cohorts, and who is in them.</summary>
    public static class Groups
    {
        public const string Default = GroupName + ".Groups";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    /// <summary>Handing an exam to a person or a cohort, and controlling the links.</summary>
    public static class Assignments
    {
        public const string Default = GroupName + ".Assignments";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";

        /// <summary>Killing a link that leaked or was sent in error.</summary>
        public const string Revoke = Default + ".Revoke";

        /// <summary>Sending or resending the email carrying the link.</summary>
        public const string SendEmail = Default + ".SendEmail";
    }

    /// <summary>Sittings and their answers.</summary>
    public static class Attempts
    {
        public const string Default = GroupName + ".Attempts";
        public const string View = Default + ".View";

        /// <summary>Ending someone's attempt on their behalf.</summary>
        public const string ForceSubmit = Default + ".ForceSubmit";
        public const string Delete = Default + ".Delete";
    }

    /// <summary>Marking what a machine cannot mark.</summary>
    public static class Review
    {
        public const string Default = GroupName + ".Review";

        /// <summary>Seeing the queue of attempts waiting on a human.</summary>
        public const string ViewQueue = Default + ".ViewQueue";

        /// <summary>Awarding marks and leaving comments.</summary>
        public const string Grade = Default + ".Grade";

        /// <summary>
        /// Seeing paste, focus-loss and timing observations. Held separately because
        /// these are behavioural data about a person, not just their answers.
        /// </summary>
        public const string ViewIntegritySignals = Default + ".ViewIntegritySignals";
    }

    /// <summary>Results and analytics.</summary>
    public static class Results
    {
        public const string Default = GroupName + ".Results";
        public const string View = Default + ".View";
        public const string Export = Default + ".Export";

        /// <summary>Question quality statistics: difficulty and discrimination.</summary>
        public const string ViewItemAnalysis = Default + ".ViewItemAnalysis";
    }

    /// <summary>The tenant's own vocabulary: categories, levels, topics, labels.</summary>
    public static class Catalog
    {
        public const string Default = GroupName + ".Catalog";
        public const string View = Default + ".View";
        public const string Manage = Default + ".Manage";
    }

    /// <summary>
    /// Staff accounts inside a tenant. Distinct from ABP's own Identity permissions:
    /// these guard this product's simplified user screens, not the full Identity module.
    /// </summary>
    public static class IdentityManagement
    {
        public const string Default = GroupName + ".IdentityManagement";

        public static class Users
        {
            public const string Default = IdentityManagement.Default + ".Users";
            public const string View = Default + ".View";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";

            /// <summary>Changing which roles a person holds.</summary>
            public const string ManageRoles = Default + ".ManageRoles";
        }
    }

    /// <summary>Tenant-wide configuration.</summary>
    public static class Administration
    {
        public const string Default = GroupName + ".Administration";
        // Access was removed. It promised "may reach the staff application" and
        // guarded nothing at all — everybody who can sign in is staff, and being
        // signed in is what the shell already requires. A permission that can be
        // granted and enforces nothing is a promise the administration screen
        // makes and the product does not keep.
        public const string ManageSettings = Default + ".ManageSettings";
    }
}
