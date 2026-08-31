import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Subject, catchError, debounceTime, map, of, switchMap } from 'rxjs';

import {
  AssignmentRecipient,
  AssignmentResult,
  AssignmentService,
  ExamLinkDto,
} from '../../core/api/assignment.service';
import { CandidateService } from '../../core/api/candidate.service';
import { StructureService } from '../../core/api/structure.service';
import { ExamFormDto, ExamFormStatus } from '../../core/api/structure.models';
import { ExamService } from '../../core/api/exam.service';
import { CandidateDto, CandidateGroupDto } from '../../core/api/candidate.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { ModalDirective } from '../../shared/ui/modal.directive';

/**
 * Stands for "whichever paper comes next" in the picker.
 *
 * A sentinel rather than a second control, because naming a paper and rotating
 * through them are one decision — which paper does this sitting use — and two
 * controls would let somebody set both.
 */
const ROTATE = '__rotate__';

/**
 * Who a sitting goes to: a whole class, or one named person.
 *
 * One setting rather than two pickers, because the server refuses a request
 * carrying both and "some of each" is a request nobody can check before the
 * links are out. Holding the choice here is what makes both impossible to fill
 * in at once — there is no state in which the panel has a class and a person.
 */
type Audience = 'group' | 'person';

/**
 * Below this a search is not worth running.
 *
 * One letter matches most of a centre, which is a list nobody reads and a query
 * the database has to answer anyway.
 */
const MIN_SEARCH = 2;

/**
 * How many matches the person picker offers at a time.
 *
 * Short on purpose. This is a picker, not a roll: somebody looking for a named
 * student refines the term, and a list long enough to scroll is a sign the term
 * was too vague rather than something to page through.
 */
const SEARCH_LIMIT = 8;

/**
 * Sending an exam out, and watching what happened to the links.
 *
 * The last stretch between a roll of people and somebody sitting an exam. The
 * service for it was written weeks ago and had no controller, so none of it was
 * reachable.
 *
 * <h4>The links are shown once</h4>
 * A link is stored hashed and cannot be recovered — only the first characters
 * survive, which is enough to tell two apart and not enough to use. So the panel
 * that appears after sending is the single opportunity to copy them, and it says
 * so plainly rather than letting somebody close it and find out.
 *
 * <h4>An email that failed is not a link that failed</h4>
 * Creating the links never depends on the mail server. When a send fails the
 * link still exists, and the recipient row carries it with the failure beside
 * it, so a coordinator can pass it on another way instead of starting again.
 */
@Component({
  selector: 'astro-assignment',
  standalone: true,
  imports: [FormsModule, DatePipe, PageHeaderComponent, ModalDirective],
  templateUrl: './assignment.component.html',
  styleUrl: './assignment.component.scss',
})
export class AssignmentComponent {
  private readonly assignments = inject(AssignmentService);
  private readonly candidates = inject(CandidateService);
  private readonly exams = inject(ExamService);
  private readonly structure = inject(StructureService);

  readonly t = inject(TranslateService).t;

  readonly examId = input.required<string>();

  readonly examTitle = signal('');
  readonly groups = signal<CandidateGroupDto[]>([]);

  /**
   * The published papers for this exam, and which one this sitting uses.
   *
   * Empty is the normal case and means "draw a paper per candidate", which is
   * right for practice. Choosing one is what makes two people's scores
   * comparable, and it is decided here because a sitting is what a paper belongs
   * to — the morning group and the afternoon group are one class and two papers.
   */
  readonly forms = signal<ExamFormDto[]>([]);
  readonly formId = signal('');

  /**
   * Who this sitting would actually go to.
   *
   * Choosing a class and being told only its name meant pressing send on a list
   * nobody had seen — and a link, once sent, is a link somebody has. The names
   * load the moment a class is chosen, so the decision is made while looking at
   * the people it affects.
   */
  readonly recipients = signal<CandidateDto[]>([]);
  readonly loadingRecipients = signal(false);

  /** A link reissued from the table, held on screen until it has been copied. */
  readonly reissued = signal<AssignmentRecipient | null>(null);

  readonly ROTATE = ROTATE;

  readonly links = signal<ExamLinkDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(0);
  readonly pageSize = 20;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly busyId = signal<string | null>(null);

  // --- the send panel ---
  readonly sending = signal(false);
  readonly groupId = signal<string>('');

  /**
   * A class, or one person. Never both.
   *
   * A class was the only answer the screen had, so a coordinator who had added
   * one student could send them nothing until they had invented a class to put
   * them in — and on a new organisation, with no classes at all, the picker was
   * empty and the button could never enable.
   */
  readonly audience = signal<Audience>('group');

  readonly personQuery = signal('');
  readonly personResults = signal<CandidateDto[]>([]);
  readonly searching = signal(false);
  readonly searchFailed = signal(false);

  /** The one person this sitting goes to, once somebody has been picked out. */
  readonly person = signal<CandidateDto | null>(null);

  readonly expiresAt = signal(this.defaultExpiry());
  readonly maxAttempts = signal(1);
  readonly sendEmail = signal(true);
  readonly working = signal(false);
  readonly result = signal<AssignmentResult | null>(null);
  readonly copied = signal<string | null>(null);

  readonly canSend = permissionSignal(P.Assignments.Create);
  readonly canCreate = permissionSignal(P.Assignments.Create);

  /**
   * Whether this account may read the candidate roll.
   *
   * Not the permission that sends an exam, and deliberately not widened into
   * one. Searching by name, address or reference over every candidate in the
   * organisation is reading the roll — that is what the endpoint behind it is,
   * `Candidates.View` is what guards it, and the shipped Coordinator role holds
   * both this and `Assignments.Create` precisely because sending needs both.
   *
   * What was wrong was never the guard. It was that a coordinator on a custom
   * role without it typed a name, waited, and read "the search could not run" —
   * a sentence describing a network fault, for a permission decision that will
   * not change however many times they try. So the panel asks first and says
   * which permission is missing, and the search is not run at all.
   */
  readonly canSeeCandidates = permissionSignal(P.Candidates.View);
  readonly canRevoke = permissionSignal(P.Assignments.Revoke);
  readonly canSendEmail = permissionSignal(P.Assignments.SendEmail);

  readonly totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));
  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.links().length === 0);

  /**
   * Whether there is somebody to send to at all.
   *
   * Reads whichever side the audience is set to and ignores the other, so the
   * button can never be enabled by a leftover from the side nobody is looking
   * at.
   */
  readonly canConfirm = computed(() =>
    this.audience() === 'person' ? !!this.person() : !!this.groupId(),
  );

  /** Announced to a screen reader; the list itself is only visible. */
  readonly searchStatus = computed(() => {
    // Announced whatever has been typed, because there is no search to wait for
    // and a reader who hears nothing back assumes the box swallowed the name.
    if (!this.canSeeCandidates()) {
      return this.t('::Assignment:Person:NeedsCandidates');
    }

    if (this.searching() || this.personQuery().trim().length < MIN_SEARCH) {
      return '';
    }

    return this.searchFailed()
      ? this.t('::Assignment:Person:Failed')
      : this.t('::Assignment:Person:Results', this.personResults().length.toString());
  });

  private loadedId?: string;

  /** Keystrokes on the person search, before they have been slowed down. */
  private readonly typed = new Subject<string>();

  constructor() {
    // Searched on the server, one page of matches at a time. A centre with six
    // hundred students is ordinary, and loading all of them to filter in the
    // browser is what already broke the roll editor.
    //
    // `switchMap` rather than `mergeMap` because a slow answer to "lay" must not
    // land after the answer to "layla" and put the wrong names under the cursor.
    this.typed
      .pipe(
        // No `distinctUntilChanged` here. Typing a letter and deleting it inside
        // the debounce window lands the same term twice, and dropping the second
        // one would leave the box saying "loading" with nothing on the way.
        debounceTime(250),
        switchMap(term => {
          const trimmed = term.trim();

          if (trimmed.length < MIN_SEARCH) {
            return of<CandidateDto[] | null>([]);
          }

          return this.candidates
            .getList({ filter: trimmed, skipCount: 0, maxResultCount: SEARCH_LIMIT })
            .pipe(
              map(page => page.items),
              // Swallowed inside the inner observable on purpose: an error that
              // escapes into the outer pipe kills the stream, and the search box
              // would go quietly dead for the rest of the panel's life.
              catchError(() => of<CandidateDto[] | null>(null)),
            );
        }),
        takeUntilDestroyed(),
      )
      .subscribe(found => {
        this.searching.set(false);
        this.searchFailed.set(found === null);
        this.personResults.set(found ?? []);
      });

    effect(() => {
      const id = this.examId();

      if (!id || id === this.loadedId) {
        return;
      }

      this.loadedId = id;

      this.exams.get(id).subscribe({
        next: exam => this.examTitle.set(exam.title),
        error: () => this.examTitle.set(''),
      });

      this.candidates.getGroups().subscribe({
        next: groups => this.groups.set(groups),
        error: () => undefined,
      });

      // Only published papers are offered. A draft has not been reviewed and a
      // retired one was taken out of rotation on purpose; the server refuses
      // both, and offering them here would be an error somebody has to hit
      // before they learn it.
      this.structure.getForms(id).subscribe({
        next: forms => this.forms.set(forms.filter(f => f.status === ExamFormStatus.Published)),
        error: () => undefined,
      });

      this.load();
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.assignments
      .getLinks(this.examId(), {
        skipCount: this.page() * this.pageSize,
        maxResultCount: this.pageSize,
      })
      .subscribe({
        next: result => {
          this.links.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: err => {
          this.error.set(this.reason(err));
          this.loading.set(false);
        },
      });
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  /**
   * What a link is doing right now.
   * <p>
   * Read in this order deliberately. Revoked beats everything — a killed link is
   * not "in progress" whatever else is true of it. Spent beats open, because a
   * link with no attempts left is not usable however long it has to run.
   * </p>
   */
  linkState(link: ExamLinkDto): 'revoked' | 'spent' | 'expired' | 'started' | 'sent' | 'ready' {
    if (link.isRevoked) {
      return 'revoked';
    }

    if (link.attemptsUsed >= link.maxAttempts) {
      return 'spent';
    }

    if (new Date(link.expiresAt) < new Date()) {
      return 'expired';
    }

    if (link.firstOpenedAt) {
      return 'started';
    }

    return link.emailSentAt ? 'sent' : 'ready';
  }

  stateKey(link: ExamLinkDto): string {
    return `::Link:State:${this.linkState(link)}`;
  }

  // -------------------------------------------------------------------- send

  /**
   * Switches between sending to a class and sending to one person.
   *
   * Clearing the other side is the whole point. The server rejects a request
   * naming both, and a screen that lets somebody fill in two and then tells them
   * off has already wasted the minute they spent picking the second one.
   */
  setAudience(audience: Audience): void {
    if (audience === this.audience()) {
      return;
    }

    this.audience.set(audience);

    if (audience === 'group') {
      this.clearPerson();
    } else {
      this.setGroup('');
    }
  }

  /**
   * Loads the people a chosen class would send to.
   *
   * Five hundred is far past any real class, and the count beside the name is
   * the number that matters — the list is there so somebody recognises the
   * names, not so they can audit a cohort.
   */
  setGroup(groupId: string): void {
    this.groupId.set(groupId);
    this.recipients.set([]);

    // Same permission, same silence: without Candidates.View this request is
    // refused, the names stay empty, and the panel used to read "this class has
    // nobody in it yet" — which is a different and alarming claim, and the one
    // thing that would stop somebody sending.
    if (!groupId || !this.canSeeCandidates()) {
      return;
    }

    this.loadingRecipients.set(true);

    this.candidates.getList({ groupId, skipCount: 0, maxResultCount: 500 }).subscribe({
      next: page => {
        this.recipients.set(page.items);
        this.loadingRecipients.set(false);
      },
      error: () => {
        // The names are a courtesy; the count on the picker is still right, and
        // failing the whole panel over them would stop somebody sending an exam.
        this.loadingRecipients.set(false);
      },
    });
  }

  /**
   * Looks somebody up by name, address or reference.
   *
   * The spinner goes on here rather than in the subscription so that the quarter
   * second of debounce reads as the search it is, instead of as a box that
   * ignored the last thing typed.
   */
  searchPeople(term: string): void {
    this.personQuery.set(term);
    this.searchFailed.set(false);

    // A 403 on every keystroke is a spinner that resolves into a failure the
    // panel has already explained, and a line in somebody's log for each letter
    // typed. The answer is known before the request.
    if (!this.canSeeCandidates()) {
      this.searching.set(false);
      this.personResults.set([]);

      return;
    }

    this.searching.set(term.trim().length >= MIN_SEARCH);

    if (term.trim().length < MIN_SEARCH) {
      this.personResults.set([]);
    }

    this.typed.next(term);
  }

  choosePerson(candidate: CandidateDto): void {
    this.person.set(candidate);
    this.personQuery.set('');
    this.personResults.set([]);
    this.searching.set(false);
    this.searchFailed.set(false);
  }

  clearPerson(): void {
    this.person.set(null);
    this.searchPeople('');
  }

  /**
   * Issues a fresh link for one person, and shows it.
   *
   * The token is stored hashed, so an existing link cannot be recovered — only
   * replaced. The old address stops working, which is why this asks first.
   */
  reissue(link: ExamLinkDto, sendEmail = false): void {
    this.busyId.set(link.id);
    this.actionError.set(null);

    this.assignments.reissue(link.id, sendEmail).subscribe({
      next: recipient => {
        this.reissued.set(recipient);
        this.busyId.set(null);
        this.load();
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.busyId.set(null);
      },
    });
  }

  closeReissued(): void {
    this.reissued.set(null);
  }

  // --- extending a deadline ---

  /**
   * The link whose deadline is being moved, and the date being proposed.
   *
   * Reissuing deliberately leaves the deadline alone — a lost address and a
   * missed deadline are different problems. Which meant that until this existed,
   * a coordinator helping somebody who missed Friday had only the reissue
   * button: they pressed it, read out a fresh address, and it was expired before
   * the candidate typed it.
   */
  readonly extending = signal<ExamLinkDto | null>(null);
  readonly extendTo = signal('');

  askExtend(link: ExamLinkDto): void {
    this.actionError.set(null);
    this.extending.set(link);

    // Prefilled a week out from today rather than from the old deadline, which
    // may be months gone. A week is the answer often enough to be worth typing
    // over rather than typing from scratch.
    const week = new Date();
    week.setDate(week.getDate() + 7);

    this.extendTo.set(week.toISOString().slice(0, 16));
  }

  cancelExtend(): void {
    this.extending.set(null);
    this.extendTo.set('');
  }

  confirmExtend(): void {
    const link = this.extending();
    const when = this.extendTo();

    if (!link || !when) {
      return;
    }

    this.busyId.set(link.id);
    this.actionError.set(null);

    this.assignments.extend(link.id, new Date(when).toISOString()).subscribe({
      next: () => {
        this.busyId.set(null);
        this.cancelExtend();
        this.load();
      },
      error: err => {
        // The server refuses a date in the past and a date earlier than the one
        // already set, and says which. Shown as it comes rather than replaced
        // with a general failure: "you cannot move a deadline backwards" is
        // something a coordinator can act on.
        this.actionError.set(this.reason(err));
        this.busyId.set(null);
      },
    });
  }

  copyReissued(): void {
    const recipient = this.reissued();

    if (recipient) {
      void navigator.clipboard?.writeText(recipient.url);
      this.copied.set(recipient.candidateId);
    }
  }

  openSend(): void {
    this.sending.set(true);
    this.result.set(null);
    this.audience.set('group');
    this.groupId.set('');
    this.recipients.set([]);
    this.clearPerson();
    this.expiresAt.set(this.defaultExpiry());
    this.maxAttempts.set(1);
    this.sendEmail.set(true);
  }

  closeSend(): void {
    this.sending.set(false);

    if (this.result()) {
      this.load();
    }
  }

  send(): void {
    this.working.set(true);
    this.actionError.set(null);

    // Exactly one of the two targets is ever set, because the audience decides
    // which one is even read. The server rejects a request carrying both, and
    // this is where that stays true rather than being checked afterwards.
    const person = this.audience() === 'person';

    this.assignments
      .create({
        examId: this.examId(),
        examFormId: this.formId() === ROTATE ? undefined : this.formId() || undefined,
        rotateForms: this.formId() === ROTATE,
        candidateId: person ? this.person()?.id : undefined,
        candidateGroupId: person ? undefined : this.groupId() || undefined,
        expiresAt: new Date(this.expiresAt()).toISOString(),
        maxAttempts: this.maxAttempts(),
        sendEmail: this.sendEmail(),
      })
      .subscribe({
        next: result => {
          this.result.set(result);
          this.working.set(false);
        },
        error: err => {
          this.working.set(false);
          this.actionError.set(this.reason(err));
        },
      });
  }

  /**
   * Copies one link.
   * <p>
   * The clipboard can be refused — a page without focus, a browser that asks
   * first — so a failure is silent rather than alarming, and the link stays
   * visible on screen to be selected by hand.
   * </p>
   */
  copy(recipient: { candidateId: string; url: string }): void {
    void navigator.clipboard
      ?.writeText(recipient.url)
      .then(() => {
        this.copied.set(recipient.candidateId);
        setTimeout(() => this.copied.set(null), 2000);
      })
      .catch(() => undefined);
  }

  copyAll(): void {
    const result = this.result();

    if (!result) {
      return;
    }

    // One line per person, name and link. What gets pasted into a message or a
    // spreadsheet, which is what a coordinator does next.
    const text = result.recipients.map(r => `${r.candidateName}\t${r.url}`).join('\n');

    void navigator.clipboard?.writeText(text).then(() => {
      this.copied.set('all');
      setTimeout(() => this.copied.set(null), 2000);
    }).catch(() => undefined);
  }

  // ------------------------------------------------------------------ revoke

  revoke(link: ExamLinkDto): void {
    this.busyId.set(link.id);
    this.actionError.set(null);

    this.assignments.revoke(link.id).subscribe({
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

  /** A fortnight, which is long enough not to rush anybody and short enough to matter. */
  private defaultExpiry(): string {
    const when = new Date();
    when.setDate(when.getDate() + 14);

    return when.toISOString().slice(0, 16);
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
