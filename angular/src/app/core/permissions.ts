/**
 * Mirrors the server's permission names.
 *
 * Duplicated deliberately rather than generated: these strings gate what a person
 * sees, and a typo silently hides a whole section from everyone who should have
 * it. Keeping them in one typed object means the compiler catches the typo, and
 * the shape matches InternshipManagementSystemPermissions.cs one-for-one so a
 * reviewer can diff the two by eye.
 */
export const InternshipManagementSystemPermissions = {
  GroupName: 'Assessment',

  Exams: {
    Default: 'Assessment.Exams',
    View: 'Assessment.Exams.View',
    Create: 'Assessment.Exams.Create',
    Edit: 'Assessment.Exams.Edit',
    Delete: 'Assessment.Exams.Delete',
    Publish: 'Assessment.Exams.Publish',
  },

  Questions: {
    Default: 'Assessment.Questions',
    View: 'Assessment.Questions.View',
    Create: 'Assessment.Questions.Create',
    Edit: 'Assessment.Questions.Edit',
    Delete: 'Assessment.Questions.Delete',
  },

  Candidates: {
    Default: 'Assessment.Candidates',
    View: 'Assessment.Candidates.View',
    Create: 'Assessment.Candidates.Create',
    Edit: 'Assessment.Candidates.Edit',
    Delete: 'Assessment.Candidates.Delete',
  },

  Groups: {
    Default: 'Assessment.Groups',
    View: 'Assessment.Groups.View',
    Create: 'Assessment.Groups.Create',
    Edit: 'Assessment.Groups.Edit',
    Delete: 'Assessment.Groups.Delete',
  },

  Assignments: {
    Default: 'Assessment.Assignments',
    View: 'Assessment.Assignments.View',
    Create: 'Assessment.Assignments.Create',
    Revoke: 'Assessment.Assignments.Revoke',
    SendEmail: 'Assessment.Assignments.SendEmail',
  },

  Attempts: {
    Default: 'Assessment.Attempts',
    View: 'Assessment.Attempts.View',
    ForceSubmit: 'Assessment.Attempts.ForceSubmit',
    Delete: 'Assessment.Attempts.Delete',
  },

  Review: {
    Default: 'Assessment.Review',
    ViewQueue: 'Assessment.Review.ViewQueue',
    Grade: 'Assessment.Review.Grade',
    ViewIntegritySignals: 'Assessment.Review.ViewIntegritySignals',
  },

  Results: {
    Default: 'Assessment.Results',
    View: 'Assessment.Results.View',
    Export: 'Assessment.Results.Export',
    ViewItemAnalysis: 'Assessment.Results.ViewItemAnalysis',
  },

  Catalog: {
    Default: 'Assessment.Catalog',
    View: 'Assessment.Catalog.View',
    Manage: 'Assessment.Catalog.Manage',
  },

  IdentityManagement: {
    Default: 'Assessment.IdentityManagement',
    Users: {
      Default: 'Assessment.IdentityManagement.Users',
      View: 'Assessment.IdentityManagement.Users.View',
      Create: 'Assessment.IdentityManagement.Users.Create',
      Edit: 'Assessment.IdentityManagement.Users.Edit',
      Delete: 'Assessment.IdentityManagement.Users.Delete',
      ManageRoles: 'Assessment.IdentityManagement.Users.ManageRoles',
    },
  },

  Administration: {
    Default: 'Assessment.Administration',
    ManageSettings: 'Assessment.Administration.ManageSettings',
  },
} as const;
