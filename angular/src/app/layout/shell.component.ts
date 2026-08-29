import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import {
  AuthService,
  ConfigStateService,
  LanguageInfo,
  PermissionService,
  SessionStateService,
} from '@abp/ng.core';
import { DirectionService, ThemePreference } from '../core/direction.service';
import { TranslateService } from '../core/translate.service';
import { NAVIGATION, NavItem, NavSection } from '../core/navigation';

/**
 * The application shell: top bar, sidebar, content region.
 *
 * Hand-built rather than themed. The LeptonX theme was removed because the
 * project had already committed to plain Bootstrap, and carrying a theme's
 * dependencies while overriding all of its output is the worst of both. What we
 * lose — a ready sidebar, a dynamic menu, a language switcher — is rebuilt here,
 * which is around two hundred lines and leaves us owning the RTL behaviour that
 * the theme never handled anyway.
 */
@Component({
  selector: 'astro-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  private readonly permission = inject(PermissionService);
  private readonly session = inject(SessionStateService);
  private readonly config = inject(ConfigStateService);
  private readonly auth = inject(AuthService);

  readonly dir = inject(DirectionService);

  /** Bound so templates can call it directly; see TranslateService for why. */
  readonly t = inject(TranslateService).t;

  /** Collapsed on small screens by default; the content matters more than the menu. */
  readonly sidebarOpen = signal(false);

  readonly userMenuOpen = signal(false);

  /**
   * The languages the server actually offers, rather than a list hard-coded here.
   * Adding a language becomes a server change, and this menu follows.
   */
  private readonly configuredLanguages = toSignal(
    // getDeep$ is untyped by design — it walks an arbitrary config path — so the
    // shape is asserted once, here, next to the path it describes.
    this.config.getDeep$('localization.languages') as Observable<LanguageInfo[]>,
    { initialValue: [] as LanguageInfo[] },
  );

  readonly languages = computed(() =>
    (this.configuredLanguages() ?? [])
      .filter((l): l is LanguageInfo & { cultureName: string } => !!l.cultureName)
      .map(l => ({
        culture: l.cultureName,
        display: l.displayName ?? l.cultureName,
      })),
  );

  readonly currentLanguage = computed(() => this.dir.language());

  /**
   * Sections with nothing the viewer may open are dropped entirely, rather than
   * left as an empty heading — a heading over nothing reads as a loading failure.
   */
  readonly sections = computed<readonly VisibleSection[]>(() =>
    NAVIGATION
      .map(section => ({
        labelKey: section.labelKey,
        items: section.items.filter(item => this.isVisible(item)),
      }))
      .filter(section => section.items.length > 0),
  );

  toggleSidebar(): void {
    this.sidebarOpen.update(open => !open);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  toggleUserMenu(): void {
    this.userMenuOpen.update(open => !open);
  }

  setLanguage(culture: string): void {
    // Delegated so the switch is in one place: it has to update the session, flip
    // the document direction and re-fetch the translations, and doing two of the
    // three is the bug this replaced.
    this.dir.setLanguage(culture);
  }

  cycleTheme(): void {
    const order: ThemePreference[] = ['system', 'light', 'dark'];
    const next = order[(order.indexOf(this.dir.theme()) + 1) % order.length];
    this.dir.setTheme(next);
  }

  logout(): void {
    this.auth.logout().subscribe();
  }

  private isVisible(item: NavItem): boolean {
    return !item.permission || this.permission.getGrantedPolicy(item.permission);
  }
}

interface VisibleSection {
  readonly labelKey: string;
  readonly items: readonly NavItem[];
}
