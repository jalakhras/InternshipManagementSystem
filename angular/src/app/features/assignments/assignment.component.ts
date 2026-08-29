import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';

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
  readonly expiresAt = signal(this.defaultExpiry());
  readonly maxAttempts = signal(1);
  readonly sendEmail = signal(true);
  readonly working = signal(false);
  readonly result = signal<AssignmentResult | null>(null);
  readonly copied = signal<string | null>(null);

  readonly canSend = permissionSignal(P.Assignments.Create);
  readonly canCreate = permissionSignal(P.Assignments.Create);
  readonly canRevoke = permissionSignal(P.Assignments.Revoke);

  readonly totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));
  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.links().length === 0);

  private loadedId?: string;

  constructor() {
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
   * Loads the people a chosen class would send to.
   *
   * Five hundred is far past any real class, and the count beside the name is
   * the number that matters — the list is there so somebody recognises the
   * names, not so they can audit a cohort.
   */
  setGroup(groupId: string): void {
    this.groupId.set(groupId);
    this.recipients.set([]);

    if (!groupId) {
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
   * Issues a fresh link for one person, and shows it.
   *
   * The token is stored hashed, so an existing link cannot be recovered — only
   * replaced. The old address stops working, which is why this asks first.
   */
  reissue(link: ExamLinkDto): void {
    this.busyId.set(link.id);
    this.actionError.set(null);

    this.assignments.reissue(link.id).subscribe({
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
    this.groupId.set('');
    this.recipients.set([]);
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

    this.assignments
      .create({
        examId: this.examId(),
        examFormId: this.formId() === ROTATE ? undefined : this.formId() || undefined,
        rotateForms: this.formId() === ROTATE,
        candidateGroupId: this.groupId() || undefined,
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
