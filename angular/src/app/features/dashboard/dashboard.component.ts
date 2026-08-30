import { Component, Signal, inject, computed } from '@angular/core';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { RouterLink } from '@angular/router';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';

/**
 * Landing screen.
 *
 * Written as an empty state on purpose: a new tenant's first view is this one,
 * and a dashboard of zeroes tells them nothing. What they need is the next
 * action, so the page is a short path to it until there is data worth charting.
 */
@Component({
  selector: 'astro-dashboard',
  standalone: true,
  imports: [RouterLink],
  template: `
    <header class="page-head">
      <h1>{{ t('::Dashboard:Title') }}</h1>
      <p class="lede">{{ t(leadKey()) }}</p>
    </header>

    <section class="starters" [attr.aria-label]="t('::Dashboard:GetStarted')">
      @for (step of visible(); track step.route) {
          <a class="starter" [routerLink]="step.route">
            <span class="starter__icon" aria-hidden="true">
              <i class="bi {{ step.icon }}"></i>
            </span>
            <span class="starter__body">
              <span class="starter__title">{{ t(step.titleKey) }}</span>
              <span class="starter__note">{{ t(step.noteKey) }}</span>
            </span>
            <i class="bi bi-chevron-right starter__go astro-flip" aria-hidden="true"></i>
          </a>
      }
    </section>
  `,
  styles: `
    .page-head { margin-block-end: var(--astro-space-6); }
    .lede { color: var(--text-secondary); font-size: var(--astro-text-lg); }

    .starters {
      display: grid;
      gap: var(--astro-space-3);
      max-inline-size: 44rem;
    }

    .starter {
      display: flex;
      align-items: center;
      gap: var(--astro-space-4);
      padding: var(--astro-space-4);
      background: var(--surface-raised);
      border: 1px solid var(--border-subtle);
      border-radius: var(--astro-radius-lg);
      color: inherit;
      text-decoration: none;
      transition: border-color var(--astro-duration-fast) var(--astro-ease),
                  box-shadow var(--astro-duration-fast) var(--astro-ease);

      &:hover {
        border-color: var(--accent);
        box-shadow: var(--astro-shadow-md);
      }
    }

    .starter__icon {
      display: grid;
      place-items: center;
      inline-size: 2.75rem;
      block-size: 2.75rem;
      flex: none;
      border-radius: var(--astro-radius-md);
      background: var(--accent-subtle);
      color: var(--accent-subtle-text);
      font-size: 1.25rem;
    }

    .starter__body { display: flex; flex-direction: column; flex: 1; }
    .starter__title { font-weight: var(--astro-weight-semibold); }
    .starter__note { font-size: var(--astro-text-sm); color: var(--text-secondary); }
    .starter__go { color: var(--text-muted); }
  `,
})
export class DashboardComponent {

  readonly t = inject(TranslateService).t;

  /**
   * Ordered as the work actually happens: define what you measure, write the
   * questions, add the people, then send it out.
   */
  readonly steps = [
    {
      route: '/catalog',
      icon: 'bi-tags',
      titleKey: '::Dashboard:Step:Catalog',
      noteKey: '::Dashboard:Step:CatalogNote',
      permission: P.Catalog.Manage,
    },
    {
      route: '/exams',
      icon: 'bi-file-earmark-text',
      titleKey: '::Dashboard:Step:Exam',
      noteKey: '::Dashboard:Step:ExamNote',
      permission: P.Exams.Create,
    },
    {
      route: '/candidates',
      icon: 'bi-people',
      titleKey: '::Dashboard:Step:Candidates',
      noteKey: '::Dashboard:Step:CandidatesNote',
      permission: P.Candidates.Create,
    },
    {
      route: '/assignments',
      icon: 'bi-send',
      titleKey: '::Dashboard:Step:Assign',
      noteKey: '::Dashboard:Step:AssignNote',
      permission: P.Assignments.Create,
    },

    // The two below are not steps in setting an exam up; they are what the
    // marker and the observer came here to do. Without them this screen showed
    // those two roles a heading, a promise of four steps, and nothing at all —
    // the first screen of the product, for two of the five roles the business
    // just defined.
    {
      route: '/review',
      icon: 'bi-check2-square',
      titleKey: '::Dashboard:Step:Review',
      noteKey: '::Dashboard:Step:ReviewNote',
      permission: P.Review.ViewQueue,
    },
    {
      route: '/results',
      icon: 'bi-bar-chart',
      titleKey: '::Dashboard:Step:Results',
      noteKey: '::Dashboard:Step:ResultsNote',
      permission: P.Results.View,
    },
  ];

  /** The cards this particular person can actually use. */
  readonly visible = computed(() =>
    this.steps.filter(step => !step.permission || this.can(step.permission)));

  /**
   * "Four steps between here and your first exam" is true for somebody setting
   * one up and false for everybody else. A marker has no steps to take here and
   * should not be told they are four away from something.
   */
  readonly leadKey = computed(() => {
    const shown = this.visible();

    if (shown.length === 0) {
      return '::Dashboard:Lede:Nothing';
    }

    return shown.some(step => step.permission === P.Exams.Create)
      ? '::Dashboard:Lede'
      : '::Dashboard:Lede:Work';
  });

  /**
   * One signal per policy these cards ask about.
   *
   * Built here, in the field initialiser, because `permissionSignal` injects and
   * injection is only legal during construction. Created lazily inside `can()`
   * it threw the first time a template called it — which is the failure mode the
   * signals were introduced to remove.
   */
  private readonly granted = new Map<string, Signal<boolean>>(
    this.steps.map(step => [step.permission, permissionSignal(step.permission)] as const),
  );

  /**
   * Whether the viewer holds a policy, as an answer that updates when it does.
   *
   * Read through a signal so a card appears when the configuration arrives,
   * rather than staying hidden from somebody who can use it. Read once at
   * construction it was `false` for everyone whose configuration had not landed.
   */
  can(policy: string): boolean {
    return this.granted.get(policy)?.() ?? false;
  }
}
