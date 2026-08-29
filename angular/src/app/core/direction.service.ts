import { DOCUMENT, Injectable, effect, inject, signal } from '@angular/core';
import { SessionStateService } from '@abp/ng.core';

/** Languages the platform ships. Arabic is the default and reads right-to-left. */
export const RTL_LANGUAGES = new Set(['ar', 'fa', 'he', 'ur']);

export type ThemePreference = 'system' | 'light' | 'dark';

/**
 * Keeps `dir` and `lang` on the document in step with the active culture, and
 * carries the viewer's theme choice.
 *
 * Direction is set on `<html>` rather than on a container because the browser
 * needs it before layout: scrollbar side, text alignment, logical properties and
 * form control rendering all resolve from it. Setting it lower down leaves the
 * page half-mirrored.
 */
@Injectable({ providedIn: 'root' })
export class DirectionService {
  private readonly document = inject(DOCUMENT);
  private readonly session = inject(SessionStateService);

  /** Current direction, for components that genuinely need to branch on it. */
  readonly direction = signal<'rtl' | 'ltr'>('rtl');

  readonly language = signal<string>('ar');

  readonly theme = signal<ThemePreference>(this.readStoredTheme());

  constructor() {
    this.session.getLanguage$().subscribe(lang => this.applyLanguage(lang ?? 'ar'));

    effect(() => this.applyTheme(this.theme()));
  }

  setTheme(preference: ThemePreference): void {
    this.theme.set(preference);

    try {
      localStorage.setItem('astro.theme', preference);
    } catch {
      // Private windows and locked-down browsers throw here. A theme that fails
      // to persist is a small loss; a page that fails to render is not.
    }
  }

  private applyLanguage(lang: string): void {
    const base = lang.split('-')[0];
    const dir = RTL_LANGUAGES.has(base) ? 'rtl' : 'ltr';

    this.language.set(lang);
    this.direction.set(dir);

    const html = this.document.documentElement;
    html.setAttribute('lang', lang);
    html.setAttribute('dir', dir);
  }

  private applyTheme(preference: ThemePreference): void {
    const html = this.document.documentElement;

    // 'system' removes the stamp entirely so the tokens fall through to
    // prefers-color-scheme, which is what most visitors actually get.
    if (preference === 'system') {
      html.removeAttribute('data-theme');
    } else {
      html.setAttribute('data-theme', preference);
    }
  }

  private readStoredTheme(): ThemePreference {
    try {
      const stored = localStorage.getItem('astro.theme');
      return stored === 'light' || stored === 'dark' ? stored : 'system';
    } catch {
      return 'system';
    }
  }
}
