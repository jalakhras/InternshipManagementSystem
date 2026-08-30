import { InternshipManagementSystemPermissions as P } from './permissions';

/**
 * One item in the sidebar.
 *
 * `permission` is the policy that must be granted for the item to appear. Hiding
 * a link the person cannot use is not security — the server enforces that — but
 * showing it is a small cruelty: they click, they are refused, and they learn the
 * product is unreliable.
 */
export interface NavItem {
  /** Localisation key. Never a literal string: this menu ships in two languages. */
  readonly labelKey: string;
  readonly route: string;
  /** Bootstrap Icons class. SVG icons, never emoji — emoji render differently per OS. */
  readonly icon: string;
  readonly permission?: string;
}

export interface NavSection {
  readonly labelKey: string;
  readonly items: readonly NavItem[];
}

/**
 * The sidebar, grouped by the job being done rather than by the tables behind it.
 * A recruiter thinks "who am I assessing", not "which entity holds candidates".
 */
export const NAVIGATION: readonly NavSection[] = [
  {
    labelKey: '::Nav:Overview',
    items: [
      { labelKey: '::Nav:Dashboard', route: '/', icon: 'bi-speedometer2' },
    ],
  },
  {
    labelKey: '::Nav:Assessments',
    items: [
      { labelKey: '::Nav:Exams', route: '/exams', icon: 'bi-file-earmark-text', permission: P.Exams.View },
      { labelKey: '::Nav:QuestionBank', route: '/questions', icon: 'bi-collection', permission: P.Questions.View },
    ],
  },
  {
    labelKey: '::Nav:People',
    items: [
      { labelKey: '::Nav:Candidates', route: '/candidates', icon: 'bi-people', permission: P.Candidates.View },
      { labelKey: '::Nav:Groups', route: '/groups', icon: 'bi-diagram-3', permission: P.Groups.View },
      { labelKey: '::Nav:Assignments', route: '/assignments', icon: 'bi-send', permission: P.Assignments.View },
    ],
  },
  {
    labelKey: '::Nav:Results',
    items: [
      { labelKey: '::Monitor:Title', route: '/results/running', icon: 'bi-hourglass-split', permission: P.Attempts.View },
      { labelKey: '::Nav:ReviewQueue', route: '/review', icon: 'bi-pencil-square', permission: P.Review.ViewQueue },
      { labelKey: '::Nav:Results', route: '/results', icon: 'bi-bar-chart', permission: P.Results.View },
    ],
  },
  {
    labelKey: '::Nav:Configuration',
    items: [
      { labelKey: '::Nav:Catalog', route: '/catalog', icon: 'bi-tags', permission: P.Catalog.View },
      { labelKey: '::Nav:Users', route: '/users', icon: 'bi-person-badge', permission: P.IdentityManagement.Users.View },
      { labelKey: '::Nav:Roles', route: '/roles', icon: 'bi-shield-check', permission: 'AbpIdentity.Roles' },
      { labelKey: '::Nav:Tenants', route: '/organisations', icon: 'bi-buildings', permission: 'AbpTenantManagement.Tenants' },
      { labelKey: '::Nav:Settings', route: '/settings', icon: 'bi-sliders', permission: P.Administration.ManageSettings },
    ],
  },
];
