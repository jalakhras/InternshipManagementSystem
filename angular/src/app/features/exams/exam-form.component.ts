import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { CatalogService } from '../../core/api/catalog.service';
import { SettingsService } from '../../core/api/settings.service';
import { CategoryDto, LevelDto } from '../../core/api/catalog.models';
import { ExamService } from '../../core/api/exam.service';
import {
  CreateUpdateExamDto,
  ExamDto,
  ExamMode,
  ExamStatus,
  PublishCheckDto,
} from '../../core/api/assessment.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { StatusChipComponent } from '../../shared/ui/status-chip.component';
import { ModalDirective } from '../../shared/ui/modal.directive';

/**
 * Creating and editing an exam.
 *
 * Grouped by the decision being made rather than by the shape of the DTO: what it
 * is, how long people get, and how the paper is assembled. The last group is where
 * the anti-leak behaviour lives, so it is stated in sentences rather than left as
 * four unexplained switches.
 */
@Component({
  selector: 'astro-exam-form',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent, StatusChipComponent, ModalDirective],
  templateUrl: './exam-form.component.html',
  styleUrl: './exam-form.component.scss',
})
export class ExamFormComponent {
  private readonly exams = inject(ExamService);
  private readonly catalog = inject(CatalogService);
  private readonly settings = inject(SettingsService);
  private readonly router = inject(Router);

  readonly t = inject(TranslateService).t;

  /** Bound from the route. Absent means this is a new exam. */
  readonly id = input<string>();

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly exam = signal<ExamDto | null>(null);

  /**
   * The catalogue, for the two pickers below.
   *
   * These fields existed on the DTO from the start and had nowhere to be set, so
   * every exam in the product was filed under no domain and no level. That is not
   * cosmetic: the rule behind the shared item bank is "same domain, same level",
   * and with both null it collapses to "this exam's own questions" — so an exam
   * that should have drawn from a bank of four hundred drew from its own dozen,
   * silently and with no error anywhere.
   */
  readonly categories = signal<CategoryDto[]>([]);
  readonly publishCheck = signal<PublishCheckDto | null>(null);
  readonly showPublishPanel = signal(false);

  /**
   * Levels offered for the chosen domain.
   *
   * Empty until a domain is chosen, because a level belongs to a ladder and
   * offering every organisation's levels at once is how somebody files a
   * beginners' safety test under B2.
   */
  readonly levels = computed<LevelDto[]>(() => {
    const chosen = this.categories().find(c => c.id === this.form().categoryId);

    return chosen?.levels ?? [];
  });

  readonly form = signal<CreateUpdateExamDto>({
    title: '',
    description: '',
    mode: ExamMode.Assessment,
    timeLimitInMinutes: 60,
    // Replaced with the organisation's own default once settings arrive. Sixty
    // is what a form shows before that lands, not a decision.
    passingPercentage: 60,
    shuffleQuestions: true,
    shuffleOptions: true,
    oneQuestionAtATime: true,
    allowBackNavigation: true,
    collectIntegritySignals: true,
    isScheduled: false,
  });

  // ------------------------------------------------------- the scheduled window

  /**
   * The organisation's time zone, for the sentence under the two date boxes.
   *
   * The window is wall-clock, not an instant: a coordinator typing 09:00 means
   * nine in the morning where they are, and the server converts into this zone
   * before it decides whether an exam is open. So the zone has to be on the
   * screen where the hour is typed — otherwise the only place it is stated is a
   * settings page the author may never have opened, and the failure it causes
   * (a cohort sitting in a room while the exam refuses to open) shows up hours
   * later with nothing on screen to explain it.
   */
  readonly timeZone = computed(() => this.settings.current()?.timeZone || null);

  /**
   * What a `datetime-local` input wants, from what the server sent.
   *
   * The server's value has no zone by design — it is a wall clock — so it
   * arrives as `2026-09-01T09:00:00`. The input takes exactly the first sixteen
   * characters of that, and slicing rather than parsing is deliberate: `new
   * Date()` would read the string as local time, shift it, and hand the author
   * back a different hour from the one they typed.
   */
  private toInput(value: string | undefined): string {
    return value ? value.slice(0, 16) : '';
  }

  readonly scheduledStart = computed(() => this.toInput(this.form().scheduledStartTime));
  readonly scheduledEnd = computed(() => this.toInput(this.form().scheduledEndTime));

  /**
   * The half-set or back-to-front window, said before Save rather than after.
   * <p>
   * The server refuses both, and its refusal is the same sentence — but an
   * author finds out on the way back from a request instead of while they are
   * still looking at the two boxes.
   * </p>
   */
  readonly scheduleProblem = computed<string | null>(() => {
    const form = this.form();

    if (!form.isScheduled) {
      return null;
    }

    if (!form.scheduledStartTime || !form.scheduledEndTime) {
      return 'IMS:Exam:ScheduleNeedsBothDates';
    }

    // String comparison, not Date: both are the same fixed-width wall-clock
    // shape, so they sort correctly and neither is dragged through a zone.
    return form.scheduledStartTime >= form.scheduledEndTime
      ? 'IMS:Exam:ScheduleEndsBeforeItStarts'
      : null;
  });

  setScheduled(on: boolean): void {
    this.form.update(f => ({ ...f, isScheduled: on }));
  }

  setScheduleStart(value: string): void {
    this.patch('scheduledStartTime', value || undefined);
  }

  setScheduleEnd(value: string): void {
    this.patch('scheduledEndTime', value || undefined);
  }

  readonly isNew = computed(() => !this.id());
  // Signals, not one-shot booleans. Read in a field initialiser the answer is
  // whatever the configuration happened to hold during construction, and a
  // component built before it lands captures false and keeps it — an author with
  // every permission looking at a form with no Save button.
  readonly canEdit = permissionSignal(P.Exams.Edit);
  readonly canPublish = permissionSignal(P.Exams.Publish);

  readonly isPublished = computed(() => this.exam()?.status === ExamStatus.Published);

  readonly ExamMode = ExamMode;

  /** The id already loaded, so navigating between two exams reloads but typing does not. */
  private loadedId?: string;

  constructor() {
    // "Applied to a new exam unless its author changes it" — what the setting's
    // own hint promises, and what nothing did. The pass mark was hardcoded at
    // sixty here, so an organisation that set seventy on its settings screen
    // watched every new exam come back at sixty and had to correct each one by
    // hand, or not notice.
    //
    // Only for a new exam. An exam being edited keeps the mark it was published
    // with; changing that under an author because a default moved would rewrite
    // the rule somebody already sat under.
    this.settings.load().subscribe({
      next: settings => {
        if (this.isNew() && settings.defaultPassingPercentage != null) {
          this.form.update(form => ({
            ...form,
            passingPercentage: settings.defaultPassingPercentage,
          }));
        }
      },
      error: () => undefined,
    });

    // Not read in the constructor. withComponentInputBinding() sets a routed
    // component's inputs after it is constructed, so `id` was always undefined
    // here: the exam was never fetched and the form opened as a new one, which
    // is why pressing Edit never entered edit mode.
    // Loaded once for the pickers. Failing to load them must not stop somebody
    // editing an exam's title, so there is no error branch: the pickers stay
    // empty and everything else on the form still works.
    this.catalog.getCategories().subscribe({
      next: categories => this.categories.set(categories),
    });

    effect(() => {
      const id = this.id();

      if (!id || id === this.loadedId) {
        return;
      }

      this.loadedId = id;
      this.loadExam(id);
    });
  }

  private loadExam(id: string): void {
    this.loading.set(true);

    this.exams.get(id).subscribe({
      next: exam => {
        this.exam.set(exam);
        this.form.set({
          title: exam.title,
          description: exam.description,
          categoryId: exam.categoryId,
          levelId: exam.levelId,
          mode: exam.mode,
          timeLimitInMinutes: exam.timeLimitInMinutes,
          passingPercentage: exam.passingPercentage,
          questionsPerForm: exam.questionsPerForm,
          shuffleQuestions: exam.shuffleQuestions,
          shuffleOptions: exam.shuffleOptions,
          oneQuestionAtATime: exam.oneQuestionAtATime,
          allowBackNavigation: exam.allowBackNavigation,
          collectIntegritySignals: exam.collectIntegritySignals,
          isScheduled: exam.isScheduled,
          scheduledStartTime: exam.scheduledStartTime,
          scheduledEndTime: exam.scheduledEndTime,
        });
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  patch<K extends keyof CreateUpdateExamDto>(key: K, value: CreateUpdateExamDto[K]): void {
    this.form.update(f => ({ ...f, [key]: value }));
  }

  /**
   * Changing the domain clears the level.
   *
   * A level belongs to one ladder. Keeping the old selection would leave an
   * English A1 exam filed under safety, which reads fine on the screen and puts
   * the paper in the wrong bank.
   */
  setCategory(categoryId: string): void {
    this.form.update(f => ({
      ...f,
      categoryId: categoryId || undefined,
      levelId: undefined,
    }));
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);

    const id = this.id();
    const request = id
      ? this.exams.update(id, this.form())
      : this.exams.create(this.form());

    request.subscribe({
      next: exam => {
        this.saving.set(false);
        this.exam.set(exam);

        if (!id) {
          // A new exam has no questions yet, so the next thing anyone does is add
          // them. Land on the saved exam rather than back on the list.
          this.router.navigate(['/exams', exam.id]);
        }
      },
      error: err => {
        this.error.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  /**
   * Asks what publishing would do before offering it.
   *
   * The whole list at once, rather than discovering the problems one refused click
   * at a time — publishing is the gate between a draft and something a real person
   * sits, and someone led through three refusals stops reading the fourth.
   */
  openPublishPanel(): void {
    const id = this.id();
    if (!id) {
      return;
    }

    this.showPublishPanel.set(true);
    this.publishCheck.set(null);

    this.exams.checkPublish(id).subscribe({
      next: check => this.publishCheck.set(check),
      error: err => this.error.set(this.reason(err)),
    });
  }

  publish(): void {
    const id = this.id();
    if (!id) {
      return;
    }

    this.saving.set(true);

    this.exams.publish(id).subscribe({
      next: exam => {
        this.exam.set(exam);
        this.showPublishPanel.set(false);
        this.saving.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  /**
   * The server's own message when there is one.
   *
   * Error codes are localised server-side, so the message that comes back is
   * already a sentence in the reader's language — passing it through beats
   * inventing a generic one here.
   */
  private reason(err: unknown): string {
    const e = err as { error?: { error?: { message?: string } }; message?: string };
    return e?.error?.error?.message ?? e?.message ?? this.t('::UnknownError');
  }
}
