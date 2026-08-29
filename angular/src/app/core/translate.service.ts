import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LocalizationService } from '@abp/ng.core';

/**
 * Translation that actually re-renders when the language changes.
 *
 * ABP ships two pipes and neither is reactive: `abpLocalization` is a pure pipe,
 * so with a constant key it evaluates once and never again; `abpAsyncLocalization`
 * takes the config once and completes. In ABP's own themes this is hidden because
 * switching language reloads the page. This app does not reload — the switch
 * should not throw away whatever someone was doing — so the direction flipped
 * while every string stayed in the previous language.
 *
 * The fix is a signal the language change bumps. Reading it inside `t()` makes any
 * template expression that calls `t()` depend on it, so Angular re-evaluates that
 * expression and only that expression. No page reload, no component recreation, no
 * lost form state, and nothing to remember at each call site beyond using `t`.
 */
@Injectable({ providedIn: 'root' })
export class TranslateService {
  private readonly localization = inject(LocalizationService);

  /**
   * Bumped on every language change. Its value is meaningless; being read is the
   * whole point — it is what ties a template expression to the current language.
   */
  private readonly revision = signal(0);

  constructor() {
    this.localization.languageChange$
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.revision.update(n => n + 1));
  }

  /**
   * Resolves a localisation key in the current language.
   *
   * Bound as a method in templates: `{{ t('::Nav:Exams') }}`. It is an ordinary
   * function call rather than a pipe precisely so it can take a signal dependency,
   * which a pure pipe cannot.
   */
  readonly t = (key: string, ...params: string[]): string => {
    this.revision();
    return this.localization.instant(key, ...params);
  };
}
