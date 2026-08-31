import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, catchError, debounceTime, forkJoin, of, switchMap } from 'rxjs';

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
import { PagerComponent } from '../../shared/ui/pager.component';
import { failureReason } from '../../core/failure';

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
 * The roll used to be edited as a whole list — every candidate in the tenant in
 * one dialog, ticked or not, and Save sent the ticked ones as the new class.
 * That reads well and is untrue of any browser: it fetched 500 people and
 * searched them in the page, so at a centre with more than 500 there were people
 * who were not on the screen, could not be found by typing, could not be ticked,
 * and therefore could not be put into any class at all. Raising the number is
 * not the fix — ABP refuses a page over 1000, and a dialog of a thousand
 * checkboxes is not a way to find Fatima.
 *
 * So the dialog is now two questions rather than one list. "In this class" is
 * the members, paged, usually tens of rows. "Add somebody" is a search against
 * the server, which has always matched name, email and reference — the dialog
 * simply never sent it. And Save sends what changed, not what the roll should
 * be, because a client that cannot hold the roll must not claim to.
 *
 * The classes themselves still page in the browser, because the endpoint behind
 * them returns all of them at once and has no skip/take to ask for — so that
 * paging is presentation, and honest about being that.
 */
@Component({
  selector: 'astro-group-list',
  standalone: true,
  imports: [
    FormsModule,
    DatePipe,
    PageHeaderComponent,
    DataStateComponent,
    ModalDirective,
    PagerComponent,
  ],
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

  readonly page = signal(0);
  readonly pageSize = GROUPS_PAGE_SIZE;

  /** The page of classes on screen. Sliced here because the endpoint sends all of them. */
  readonly visibleGroups = computed(() =>
    this.groups().slice(this.page() * this.pageSize, (this.page() + 1) * this.pageSize),
  );

  // ---------------------------------------------------------- the roll editor
  readonly rollFor = signal<CandidateGroupDto | null>(null);

  /** The first read of the members. Its failure is the one that closes the editor. */
  readonly rollLoading = signal(false);

  // "In this class": the members, a page at a time.
  readonly members = signal<CandidateDto[]>([]);
  readonly memberTotal = signal(0);
  readonly memberPage = signal(0);
  readonly rollPageSize = ROLL_PAGE_SIZE;

  // "Add somebody": a search of everybody, run by the server.
  readonly rollFilter = signal('');
  readonly matches = signal<CandidateDto[]>([]);
  readonly matchTotal = signal(0);
  readonly matchPage = signal(0);
  readonly searching = signal(false);

  /** The term the rows on screen actually answer, so the empty state cannot lie. */
  readonly searchedTerm = signal('');

  /**
   * Which of the people on screen are already in this class, as the server says.
   *
   * Read rather than inferred: `groupNames` on a candidate would have to be
   * matched by name, and two classes may share one.
   */
  private readonly inClass = signal<Set<string>>(new Set());

  /**
   * What Save will send.
   *
   * Two sets of intentions rather than one set of ticks. A single "these are the
   * members" set cannot tell "unticked" from "never loaded", and that ambiguity
   * *is* the 500 bug: the screen held a page and the protocol read its silence
   * about everybody else as a removal. "Add these, remove those" says nothing
   * about anyone it does not name, so a page is enough to edit a roll of any
   * length — and two coordinators no longer overwrite each other, because their
   * two changes commute.
   */
  readonly pendingAdd = signal<Set<string>>(new Set());
  readonly pendingRemove = signal<Set<string>>(new Set());

  readonly rollDirty = computed(() => this.pendingAdd().size > 0 || this.pendingRemove().size > 0);

  /** How many will be in the class once this is saved. */
  readonly rollCount = computed(
    () => this.memberTotal() + this.pendingAdd().size - this.pendingRemove().size,
  );

  /** Nothing matched what was typed, as distinct from nothing typed yet. */
  readonly noMatches = computed(
    () => !this.searching() && !!this.searchedTerm() && this.matchTotal() === 0,
  );

  /** Keystrokes on their way to the server. */
  private readonly typed = new Subject<void>();

  constructor() {
    this.load();

    this.catalog.getCategories().subscribe({
      next: categories => this.categories.set(categories),
    });

    // Debounced, because the alternative is a request per keystroke against a
    // table of every person in the centre. Enter pushes into the same subject,
    // so somebody who types and presses it does not wait out the delay twice.
    this.typed
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        switchMap(() => {
          const term = this.rollFilter().trim();
          const group = this.rollFor();

          if (!term || !group) {
            return of(null);
          }

          // Two reads of the same page under the same filter: everyone who
          // matches, and which of them this class already holds. The second is
          // a subset of the first in the same name order, so anybody in the
          // first twenty of the one is in the first twenty of the other — the
          // pair answers "is this person already in?" exactly, not nearly.
          const skipCount = this.matchPage() * this.rollPageSize;

          return forkJoin({
            all: this.candidates.getList({
              filter: term,
              skipCount,
              maxResultCount: this.rollPageSize,
            }),
            here: this.candidates.getList({
              groupId: group.id,
              filter: term,
              skipCount,
              maxResultCount: this.rollPageSize,
            }),
          }).pipe(
            // Swallowed into the stream rather than thrown out of it: an error
            // reaching switchMap's consumer completes the subscription, and the
            // search box would then be dead for the rest of the session.
            catchError(err => {
              this.actionError.set(this.reason(err));

              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe(result => {
        this.searching.set(false);

        if (!result) {
          return;
        }

        this.matches.set(result.all.items);
        this.matchTotal.set(result.all.totalCount);
        this.inClass.set(new Set(result.here.items.map(p => p.id)));
        this.searchedTerm.set(this.rollFilter().trim());
      });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.candidates.getGroups().subscribe({
      next: groups => {
        this.groups.set(groups);

        // Deleting the last class on the last page would otherwise leave the
        // reader on a page that no longer exists, looking at nothing.
        const lastPage = Math.max(0, Math.ceil(groups.length / this.pageSize) - 1);

        if (this.page() > lastPage) {
          this.page.set(lastPage);
        }

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
    this.searchedTerm.set('');
    this.matches.set([]);
    this.matchTotal.set(0);
    this.matchPage.set(0);
    this.inClass.set(new Set());
    this.pendingAdd.set(new Set());
    this.pendingRemove.set(new Set());
    this.memberPage.set(0);
    this.rollLoading.set(true);
    this.actionError.set(null);

    // Only the members. The dialog used to open on everybody as well — one read
    // of 500 people, filtered in the browser — and that is the whole of item
    // 6.2: past 500 a person was not on the screen and no amount of typing put
    // them there. Everybody is now a search, asked for when somebody asks.
    this.loadMembers(group.id, /* opening */ true);
  }

  /**
   * The page of members on screen.
   *
   * While opening, a failure closes the editor rather than reporting into it.
   * That rule was written for a whole-list save, where a failed read of the roll
   * became an authoritative empty list and deleted classes; under a change-based
   * save it cannot do that any more. It stays because it is still true that an
   * editor should not open on state it failed to read — a coordinator looking at
   * an empty "in this class" that is empty only because a request failed will
   * add people who are already there and wonder why the count is wrong.
   */
  private loadMembers(groupId: string, opening = false): void {
    this.rollLoading.set(true);

    this.candidates
      .getList({
        groupId,
        skipCount: this.memberPage() * this.rollPageSize,
        maxResultCount: this.rollPageSize,
      })
      .subscribe({
        next: page => {
          this.members.set(page.items);
          this.memberTotal.set(page.totalCount);
          this.rollLoading.set(false);
        },
        error: err => {
          this.actionError.set(this.reason(err));
          this.rollLoading.set(false);

          if (opening) {
            // Closed, not merely reported. The message would otherwise render
            // behind the dialog's own scrim, where nobody sees it.
            this.rollFor.set(null);
          }
        },
      });
  }

  goToMemberPage(page: number): void {
    this.memberPage.set(page);

    const group = this.rollFor();

    if (group) {
      this.loadMembers(group.id);
    }
  }

  /** Every keystroke; the debounce upstream decides which ones become requests. */
  rollFilterChanged(term: string): void {
    this.rollFilter.set(term);
    this.matchPage.set(0);

    if (!term.trim()) {
      // Cleared means cleared. Leaving the last result on screen under an empty
      // box is a list that answers a question nobody is asking any more.
      this.matches.set([]);
      this.matchTotal.set(0);
      this.searchedTerm.set('');
      this.searching.set(false);

      return;
    }

    this.searching.set(true);
    this.typed.next();
  }

  /** Enter, for somebody who does not want to wait out the debounce. */
  applyRollFilter(): void {
    if (!this.rollFilter().trim()) {
      return;
    }

    this.searching.set(true);
    this.typed.next();
  }

  goToMatchPage(page: number): void {
    this.matchPage.set(page);
    this.searching.set(true);
    this.typed.next();
  }

  goToPage(page: number): void {
    this.page.set(page);
  }

  closeRoll(): void {
    this.rollFor.set(null);
    this.members.set([]);
    this.memberTotal.set(0);
    this.matches.set([]);
    this.matchTotal.set(0);
    this.inClass.set(new Set());
    this.pendingAdd.set(new Set());
    this.pendingRemove.set(new Set());
  }

  /** Whether this person is in the class as it stands, before anything unsaved. */
  isCurrently(id: string): boolean {
    return this.inClass().has(id);
  }

  /** Whether the box is ticked: the class as it stands, plus what is unsaved. */
  willBeIn(id: string, currently: boolean): boolean {
    if (this.pendingAdd().has(id)) {
      return true;
    }

    if (this.pendingRemove().has(id)) {
      return false;
    }

    return currently;
  }

  /**
   * Ticks or unticks one person.
   *
   * Recorded against what the server says is true now, so the two sets only ever
   * hold real changes: unticking somebody who is not in the class cancels a
   * pending addition rather than becoming a pending removal of nobody.
   */
  toggleMember(id: string, currently: boolean): void {
    // New Sets each time: mutating in place changes nothing a signal can see,
    // and the checkbox would not move.
    const add = new Set(this.pendingAdd());
    const remove = new Set(this.pendingRemove());

    if (this.willBeIn(id, currently)) {
      add.delete(id);

      if (currently) {
        remove.add(id);
      }
    } else {
      remove.delete(id);

      if (!currently) {
        add.add(id);
      }
    }

    this.pendingAdd.set(add);
    this.pendingRemove.set(remove);
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

    if (!this.rollDirty()) {
      // Nothing to say. Sending an empty change would be a write that means
      // "no change", which is exactly the sentence that used to be dangerous.
      this.closeRoll();

      return;
    }

    // Removing everybody is a real thing — a course that ended — and it is also
    // the shape a mistake takes, so it is asked about. It can no longer arrive
    // by accident: a failed read leaves both sets empty, which saves nothing at
    // all, and emptying a class of twelve now takes twelve deliberate unticks.
    if (this.rollCount() === 0 && this.memberTotal() > 0 && !this.confirmedEmptying) {
      this.askingToEmpty.set(true);

      return;
    }

    this.confirmedEmptying = false;
    this.askingToEmpty.set(false);

    this.saving.set(true);
    this.actionError.set(null);

    this.candidates
      .changeGroupMembers(group.id, [...this.pendingAdd()], [...this.pendingRemove()])
      .subscribe({
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
    return failureReason(err, this.t);
  }
}

/** Matches the candidates list, so the two screens turn pages at the same rate. */
const GROUPS_PAGE_SIZE = 20;
const ROLL_PAGE_SIZE = 20;

/**
 * Long enough that a typed name is one request rather than eight, short enough
 * that the list has moved by the time the reader looks up from the keyboard.
 */
const SEARCH_DEBOUNCE_MS = 250;

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
