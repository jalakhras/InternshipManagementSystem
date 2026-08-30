import { Injectable, inject, signal } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable, tap } from 'rxjs';

/** What an organisation changes about the platform for itself. */
export interface TenantSettings {
  /** What this organisation calls itself. Shown to candidates as well as staff. */
  organizationName?: string | null;

  /** The organisation's mark, as a stored blob name. */
  logoBlobName?: string | null;

  /** One accent colour, as hex. Not a palette: the rest of the contrast is not theirs to break. */
  brandColor?: string | null;

  defaultLanguage?: string | null;
  timeZone?: string | null;

  defaultPassingPercentage: number;
  showResultToCandidate: boolean;
  collectIntegritySignals: boolean;
}

/**
 * The tenant's own settings, and the one place the shell reads its branding from.
 *
 * This service does keep state, unlike its siblings, and for a reason: the
 * organisation's name and mark are read by the shell on every render and by the
 * settings screen when it saves. Fetching them twice would show the old name in
 * the header until a reload — which is exactly the kind of thing that makes
 * somebody doubt the save worked.
 */
@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly rest = inject(RestService);

  private readonly base = '/api/assessment/settings';

  /** Null until the first load. The shell falls back to the product name. */
  readonly current = signal<TenantSettings | null>(null);

  load(): Observable<TenantSettings> {
    return this.rest
      .request<void, TenantSettings>({ method: 'GET', url: this.base })
      .pipe(tap(settings => this.current.set(settings)));
  }

  update(body: TenantSettings): Observable<TenantSettings> {
    return this.rest
      .request<TenantSettings, TenantSettings>({ method: 'PUT', url: this.base, body })
      .pipe(tap(settings => this.current.set(settings)));
  }
}
