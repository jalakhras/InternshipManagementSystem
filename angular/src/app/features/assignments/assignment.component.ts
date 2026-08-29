import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';

import { AssignmentService, AssignmentResult, ExamLinkDto } from '../../core/api/assignment.service';
import { CandidateService } from '../../core/api/candidate.service';
import { StructureService } from '../../core/api/structure.service';
import { ExamFormDto, ExamFormStatus } from '../../core/api/structure.models';
import { ExamService } from '../../core/api/exam.service';
import { CandidateGroupDto } from '../../core/api/candidate.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

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
  imports: [FormsModule, DatePipe, PageHeaderComponent],
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

  openSend(): void {
    this.sending.set(true);
    this.result.set(null);
    this.groupId.set('');
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
        examFormId: this.formId() || undefined,
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
