import { Component, Signal, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import {
  AuthService,
  ConfigStateService,
  LanguageInfo,
  SessionStateService,
} from '@abp/ng.core';
import { DirectionService, ThemePreference } from '../core/direction.service';
import { TranslateService } from '../core/translate.service';
import { permissionSignal } from '../core/permission.signal';
import { NAVIGATION, NavItem, NavSection } from '../core/navigation';
import { SettingsService } from '../core/api/settings.service';
import { BrandService } from '../core/brand.service';
import { MediaService } from '../core/media.service';

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
  private readonly session = inject(SessionStateService);
  private readonly config = inject(ConfigStateService);
  private readonly auth = inject(AuthService);
  private readonly settings = inject(SettingsService);
  private readonly media = inject(MediaService);
  private readonly brand = inject(BrandService);

  readonly dir = inject(DirectionService);

  /** Bound so templates can call it directly; see TranslateService for why. */
  readonly t = inject(TranslateService).t;

  constructor() {
    // Once, here, because every screen inside the shell renders under this name.
    // A failure costs the branding and nothing else: the product name stands in.
    this.settings.load().subscribe({ error: () => undefined });
  }

  /** Collapsed on small screens by default; the content matters more than the menu. */
  readonly sidebarOpen = signal(false);

  /**
   * The organisation's own name, falling back to the product's.
   *
   * A language centre's staff should see their centre's name here. They did not
   * choose this platform by name and have no particular attachment to ours.
   */
  readonly organizationName = computed(
    () => this.settings.current()?.organizationName?.trim() || this.t('::AppName'),
  );

  /** Their mark, when they have uploaded one. The drawn astrolabe stands in until then. */
  readonly logoUrl = computed(() => this.media.objectUrl(this.settings.current()?.logoBlobName)());

  /**
   * Their colour, applied to the whole application rather than to one badge.
   * <p>
   * An effect and not a computed: this writes to the document, and it has to run
   * again when the settings arrive — which is after the shell is built, and
   * again if somebody changes the colour on the settings screen without
   * reloading.
   * </p>
   */
  private readonly paint = effect(() => this.brand.apply(this.settings.current()?.brandColor));

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
   * Every policy the sidebar asks about, each as a signal.
   *
   * Built once here rather than looked up per render. The lookup itself is what
   * had to change: `computed()` over `getGrantedPolicy()` has no dependencies —
   * that call is a plain method and NAVIGATION is a constant — so in a zoneless
   * application it evaluated during construction and never again. A user whose
   * configuration had not landed yet saw a sidebar with nothing but Dashboard
   * in it, permanently, until they reloaded.
   */
  private readonly granted = new Map<string, Signal<boolean>>(
    NAVIGATION
      .flatMap(section => section.items)
      .map(item => item.permission)
      .filter((policy): policy is string => !!policy)
      .map(policy => [policy, permissionSignal(policy)] as const),
  );

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
    if (!item.permission) {
      return true;
    }

    // Reading the signal is what ties `sections` to the configuration, so the
    // menu fills in the moment the answer arrives.
    return this.granted.get(item.permission)?.() ?? false;
  }
}

interface VisibleSection {
  readonly labelKey: string;
  readonly items: readonly NavItem[];
}
