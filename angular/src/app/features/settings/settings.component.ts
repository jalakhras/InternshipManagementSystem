import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { SettingsService, TenantSettings } from '../../core/api/settings.service';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { MediaFieldComponent } from '../../shared/ui/media-field.component';
import { DataStateComponent } from '../../shared/ui/data-state.component';
import { failureReason } from '../../core/failure';

/**
 * What this organisation changes about the platform for itself.
 *
 * The settings were defined on the server and nothing read or wrote them, so
 * every organisation on the deployment saw the same name, the same default
 * language and the same colours — which is the difference between a product
 * several organisations use and one built for somebody else that the rest
 * tolerate.
 *
 * The name and the mark are first because they are the ones a candidate sees.
 * Somebody opening a placement-test link has no relationship with us and no
 * reason to trust a name they have never heard of.
 *
 * One accent colour rather than a palette. A tenant who can set every colour can
 * set an unreadable one, and the contrast of the rest of the system is not
 * theirs to break.
 */
@Component({
  selector: 'astro-settings',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent, MediaFieldComponent, DataStateComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent {
  private readonly settings = inject(SettingsService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = signal<TenantSettings>(blank());

  readonly canManage = permissionSignal(P.Administration.ManageSettings);

  /** Read-only for everybody else, rather than hidden: knowing the rules is not a privilege. */
  readonly readOnly = computed(() => !this.canManage());

  readonly languages = [
    { culture: 'ar', labelKey: '::Settings:Language:Arabic' },
    { culture: 'en', labelKey: '::Settings:Language:English' },
  ];

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.settings.load().subscribe({
      next: settings => {
        this.form.set({ ...settings });
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  patch<K extends keyof TenantSettings>(key: K, value: TenantSettings[K]): void {
    this.form.update(f => ({ ...f, [key]: value }));
    this.saved.set(false);
  }

  /** The file picker reports the blob and its kind together. Only the blob is stored. */
  setLogo(media: { blobName?: string; mediaType?: string }): void {
    this.patch('logoBlobName', media.blobName ?? null);
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);

    this.settings.update(this.form()).subscribe({
      next: settings => {
        this.form.set({ ...settings });
        this.saving.set(false);
        this.saved.set(true);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  private reason(err: unknown): string {
    return failureReason(err, this.t);
  }
}

const blank = (): TenantSettings => ({
  organizationName: null,
  logoBlobName: null,
  supportEmail: null,
  brandColor: null,
  defaultLanguage: 'ar',
  timeZone: 'Asia/Riyadh',
  defaultPassingPercentage: 60,
  showResultToCandidate: true,
  collectIntegritySignals: true,
});
