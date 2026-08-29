import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';

import { CandidateService } from '../../core/api/candidate.service';
import {
  CandidateDto,
  CandidateGroupDto,
  CandidateStatus,
  ImportCandidatesResult,
} from '../../core/api/candidate.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

/**
 * The people who sit exams.
 *
 * The screen is built around getting a roll <em>in</em> rather than around
 * typing one. A training centre's students are already in a spreadsheet, and the
 * first thing they will try is pasting it — so pasting is the primary action and
 * adding one person by hand is the secondary one.
 *
 * The import checks before it writes. Somebody pasting forty rows sees which
 * three are wrong, with line numbers, and then decides. Reporting the damage
 * afterwards would be the same information at the moment it stops being useful.
 */
@Component({
  selector: 'astro-candidate-list',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  templateUrl: './candidate-list.component.html',
  styleUrl: './candidate-list.component.scss',
})
export class CandidateListComponent {
  private readonly candidates = inject(CandidateService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<CandidateDto[]>([]);
  readonly totalCount = signal(0);
  readonly groups = signal<CandidateGroupDto[]>([]);

  readonly filter = signal('');
  readonly groupId = signal<string>('');
  readonly page = signal(0);

  readonly pageSize = 20;

  readonly canCreate = permissionSignal(P.Candidates.Create);
  readonly canEdit = permissionSignal(P.Candidates.Edit);
  readonly canDelete = permissionSignal(P.Candidates.Delete);

  readonly pendingDelete = signal<CandidateDto | null>(null);
  readonly busyId = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  // --- the import panel ---
  readonly importing = signal(false);
  readonly importText = signal('');
  readonly importGroupId = signal<string>('');
  readonly importChecking = signal(false);
  readonly importResult = signal<ImportCandidatesResult | null>(null);
  readonly importCommitted = signal(false);

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.items().length === 0);
  readonly isFiltered = computed(() => !!this.filter() || !!this.groupId());
  readonly totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));

  /** Whether the checked list is worth committing. Nothing to write is not an error. */
  readonly canCommitImport = computed(() => {
    const result = this.importResult();

    return !!result && !this.importCommitted() && (result.created > 0 || result.addedToGroup > 0);
  });

  constructor() {
    this.candidates.getGroups().subscribe({
      next: groups => this.groups.set(groups),
      error: () => {
        // A missing cohort list costs the filter, not the roll. Failing the whole
        // screen over a dropdown would hide the people.
      },
    });

    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.candidates
      .getList({
        filter: this.filter() || undefined,
        groupId: this.groupId() || undefined,
        skipCount: this.page() * this.pageSize,
        maxResultCount: this.pageSize,
      })
      .subscribe({
        next: result => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: err => {
          this.error.set(this.reason(err));
          this.loading.set(false);
        },
      });
  }

  applyFilter(): void {
    this.page.set(0);
    this.load();
  }

  setGroup(value: string): void {
    this.groupId.set(value);
    this.applyFilter();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  statusKey(candidate: CandidateDto): string {
    switch (candidate.status) {
      case CandidateStatus.Invited:
        return '::Candidate:Status:Invited';
      case CandidateStatus.InProgress:
        return '::Candidate:Status:InProgress';
      case CandidateStatus.Completed:
        return '::Candidate:Status:Completed';
      case CandidateStatus.Withdrawn:
        return '::Candidate:Status:Withdrawn';
      default:
        return '::Candidate:Status:Pending';
    }
  }

  // ------------------------------------------------------------------ import

  openImport(): void {
    this.importing.set(true);
    this.importText.set('');
    this.importGroupId.set('');
    this.importResult.set(null);
    this.importCommitted.set(false);
  }

  closeImport(): void {
    this.importing.set(false);

    if (this.importCommitted()) {
      this.load();
    }
  }

  /** Reads the paste and reports what would happen. Writes nothing. */
  checkImport(): void {
    this.importChecking.set(true);
    this.actionError.set(null);

    this.candidates
      .import({
        text: this.importText(),
        groupId: this.importGroupId() || undefined,
        dryRun: true,
      })
      .subscribe({
        next: result => {
          this.importResult.set(result);
          this.importChecking.set(false);
        },
        error: err => {
          this.importChecking.set(false);
          this.actionError.set(this.reason(err));
        },
      });
  }

  commitImport(): void {
    this.importChecking.set(true);

    this.candidates
      .import({
        text: this.importText(),
        groupId: this.importGroupId() || undefined,
      })
      .subscribe({
        next: result => {
          this.importResult.set(result);
          this.importCommitted.set(true);
          this.importChecking.set(false);
        },
        error: err => {
          this.importChecking.set(false);
          this.actionError.set(this.reason(err));
        },
      });
  }

  // ------------------------------------------------------------------ delete

  confirmDelete(): void {
    const candidate = this.pendingDelete();

    if (!candidate) {
      return;
    }

    this.pendingDelete.set(null);
    this.run(candidate.id, this.candidates.delete(candidate.id));
  }

  private run(id: string, action: Observable<unknown>): void {
    this.busyId.set(id);
    this.actionError.set(null);

    action.subscribe({
      next: () => {
        this.busyId.set(null);
        this.load();
      },
      error: err => {
        this.busyId.set(null);
        this.actionError.set(this.reason(err));
      },
    });
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
