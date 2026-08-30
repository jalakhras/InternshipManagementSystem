import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { CandidateService } from '../../core/api/candidate.service';
import {
  CandidateDto,
  CandidateGroupDto,
  CreateUpdateCandidateGroupDto,
} from '../../core/api/candidate.models';
import { CatalogService } from '../../core/api/catalog.service';
import { CategoryDto } from '../../core/api/catalog.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { DataStateComponent } from '../../shared/ui/data-state.component';
import { ModalDirective } from '../../shared/ui/modal.directive';

/**
 * Classes: one group of people moving through a level together.
 *
 * The sidebar has linked here since the first week and the route did not exist,
 * so the link fell through to the dashboard. The service behind it was complete.
 *
 * A class carries a level rather than only a name, and that is the part worth
 * insisting on: it is what makes a cohort part of the curriculum instead of a
 * list of names beside it. A class at A1 is offered A1 papers, and its results
 * mean something set against the other A1 classes. A class with no level is
 * still allowed — a standing group of contractors is not a course — but the
 * screen says what is lost.
 *
 * The roll is edited as a whole list rather than one person at a time, because
 * the coordinator is reading from a register: they know who is in the class, not
 * which two changed since last week.
 */
@Component({
  selector: 'astro-group-list',
  standalone: true,
  imports: [FormsModule, DatePipe, PageHeaderComponent, DataStateComponent, ModalDirective],
  templateUrl: './group-list.component.html',
  styleUrl: './group-list.component.scss',
})
export class GroupListComponent {
  private readonly candidates = inject(CandidateService);
  private readonly catalog = inject(CatalogService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  readonly groups = signal<CandidateGroupDto[]>([]);
  readonly categories = signal<CategoryDto[]>([]);

  readonly canManage = permissionSignal(P.Groups.Edit);
  readonly canDelete = permissionSignal(P.Groups.Delete);

  readonly saving = signal(false);
  readonly pendingDelete = signal<CandidateGroupDto | null>(null);

  readonly draft = signal<GroupDraft>(emptyDraft());

  /** Levels under the chosen domain. A level belongs to one ladder. */
  readonly levels = computed(() =>
    this.categories().find(c => c.id === this.draft().categoryId)?.levels ?? [],
  );

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.groups().length === 0);

  // --- the roll editor ---
  readonly rollFor = signal<CandidateGroupDto | null>(null);
  readonly people = signal<CandidateDto[]>([]);
  readonly chosen = signal<Set<string>>(new Set());
  readonly rollFilter = signal('');
  readonly rollLoading = signal(false);

  readonly visiblePeople = computed(() => {
    const term = this.rollFilter().trim().toLowerCase();

    if (!term) {
      return this.people();
    }

    return this.people().filter(
      person =>
        person.fullName.toLowerCase().includes(term) || person.email.toLowerCase().includes(term),
    );
  });

  constructor() {
    this.load();

    this.catalog.getCategories().subscribe({
      next: categories => this.categories.set(categories),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.candidates.getGroups().subscribe({
      next: groups => {
        this.groups.set(groups);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  newGroup(): void {
    this.draft.set({ ...emptyDraft(), open: true });
  }

  edit(group: CandidateGroupDto): void {
    this.draft.set({
      open: true,
      id: group.id,
      name: group.name,
      description: group.description ?? '',
      categoryId: group.categoryId ?? '',
      levelId: group.levelId ?? '',
      startsOn: asDateInput(group.startsOn),
      endsOn: asDateInput(group.endsOn),
    });
  }

  cancel(): void {
    this.draft.set(emptyDraft());
  }

  setCategory(categoryId: string): void {
    // The level belongs to the domain being replaced, so it cannot survive it.
    this.draft.update(d => ({ ...d, categoryId, levelId: '' }));
  }

  patch<K extends keyof GroupDraft>(key: K, value: GroupDraft[K]): void {
    this.draft.update(d => ({ ...d, [key]: value }));
  }

  save(): void {
    const draft = this.draft();
    const name = draft.name.trim();

    if (!name) {
      return;
    }

    const body: CreateUpdateCandidateGroupDto = {
      name,
      description: draft.description.trim() || undefined,
      categoryId: draft.categoryId || undefined,
      levelId: draft.levelId || undefined,
      startsOn: draft.startsOn || null,
      endsOn: draft.endsOn || null,
    };

    this.saving.set(true);
    this.actionError.set(null);

    const request = draft.id
      ? this.candidates.updateGroup(draft.id, body)
      : this.candidates.createGroup(body);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.draft.set(emptyDraft());
        this.load();
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  askDelete(group: CandidateGroupDto): void {
    this.pendingDelete.set(group);
  }

  cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  confirmDelete(): void {
    const group = this.pendingDelete();

    if (!group) {
      return;
    }

    this.saving.set(true);

    this.candidates.deleteGroup(group.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.pendingDelete.set(null);
        this.load();
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
        this.pendingDelete.set(null);
      },
    });
  }

  // ------------------------------------------------------------------ the roll

  openRoll(group: CandidateGroupDto): void {
    this.rollFor.set(group);
    this.rollFilter.set('');
    this.rollLoading.set(true);
    this.actionError.set(null);

    // Everyone, then the ones already in this class ticked. A page of two
    // hundred is a scroll; a page that only shows members is a screen where
    // adding somebody is impossible.
    //
    // Both together, and the editor opens only if both arrived. They used to be
    // two independent subscriptions and the second had no error handler at all:
    // when it failed, `chosen` stayed empty, the other request had already
    // cleared the loading flag, and the dialog rendered a complete and healthy
    // roll with nothing ticked and no error anywhere. The screen's own sentence
    // — "what you leave ticked is the class" — then made that emptiness
    // authoritative, and pressing Save sent `candidateIds: []`, which the server
    // reads as "remove everybody". A class of twelve became a class of none,
    // reported as a successful save, with no undo.
    //
    // The rule this encodes: a whole-list editor must never open on state it
    // failed to read.
    forkJoin({
      everyone: this.candidates.getList({ skipCount: 0, maxResultCount: 500 }),
      members: this.candidates.getList({ groupId: group.id, skipCount: 0, maxResultCount: 500 }),
    }).subscribe({
      next: ({ everyone, members }) => {
        this.people.set(everyone.items);
        this.chosen.set(new Set(members.items.map(p => p.id)));
        this.rollLoading.set(false);
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.rollLoading.set(false);

        // Closed, not merely reported. Leaving the editor open with a message
        // above it keeps the data loss one click away — and the message renders
        // behind the dialog's own scrim, where nobody sees it.
        this.rollFor.set(null);
      },
    });
  }

  closeRoll(): void {
    this.rollFor.set(null);
    this.chosen.set(new Set());
  }

  toggleMember(id: string): void {
    // A new Set each time: mutating in place changes nothing a signal can see,
    // and the checkbox would not move.
    const next = new Set(this.chosen());

    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }

    this.chosen.set(next);
  }

  isMember(id: string): boolean {
    return this.chosen().has(id);
  }

  /** Asked before a class is emptied, and only then. */
  readonly askingToEmpty = signal(false);

  private confirmedEmptying = false;

  confirmEmptying(): void {
    this.confirmedEmptying = true;
    this.saveRoll();
  }

  cancelEmptying(): void {
    this.askingToEmpty.set(false);
  }

  saveRoll(): void {
    const group = this.rollFor();

    if (!group) {
      return;
    }

    this.saving.set(true);
    this.actionError.set(null);

    // Emptying a class on purpose is allowed and is sometimes exactly right —
    // a course that ended. It is only the *unmeant* empty list the server
    // refuses, so this says which of the two this is. The coordinator is asked
    // first, because removing everybody from a class is not an ordinary save.
    const emptying = this.chosen().size === 0;

    if (emptying && !this.confirmedEmptying) {
      this.saving.set(false);
      this.askingToEmpty.set(true);
      return;
    }

    this.confirmedEmptying = false;
    this.askingToEmpty.set(false);

    this.candidates.setGroupMembers(group.id, [...this.chosen()], emptying).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeRoll();
        this.load();
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}

interface GroupDraft {
  open: boolean;
  id: string | null;
  name: string;
  description: string;
  categoryId: string;
  levelId: string;
  startsOn: string;
  endsOn: string;
}

const emptyDraft = (): GroupDraft => ({
  open: false,
  id: null,
  name: '',
  description: '',
  categoryId: '',
  levelId: '',
  startsOn: '',
  endsOn: '',
});

/** An ISO instant trimmed to what a date input accepts. */
function asDateInput(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : '';
}
