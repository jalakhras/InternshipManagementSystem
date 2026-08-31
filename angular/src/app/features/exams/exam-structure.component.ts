import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { StructureService } from '../../core/api/structure.service';
import {
  CreateUpdateExamSectionDto,
  ExamSectionDto,
} from '../../core/api/structure.models';
import { QuestionService } from '../../core/api/question.service';
import { QuestionGroupDto } from '../../core/api/assessment.models';
import { ExamService } from '../../core/api/exam.service';
import { CatalogService } from '../../core/api/catalog.service';
import { TopicDto } from '../../core/api/catalog.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { MediaFieldComponent } from '../../shared/ui/media-field.component';
import { DataStateComponent } from '../../shared/ui/data-state.component';
import { ModalDirective } from '../../shared/ui/modal.directive';
import { failureReason } from '../../core/failure';

/**
 * How an exam is laid out: its parts, and the passages inside it.
 *
 * Both halves existed on the server with no screen, which meant an English exam
 * could not actually be an English exam. It needs two things this page provides:
 *
 *  · Sections — grammar, listening, reading, writing. A section can carry its
 *    own time limit and its own floor, so somebody who fails listening fails the
 *    exam however well the rest went. Without them, four different kinds of
 *    question produce one undifferentiated number.
 *
 *  · Passages — a reading text, an audio clip or a video with several questions
 *    hanging off it. The taker sees it once, beside each of its questions,
 *    rather than repeated in six prompts.
 *
 * Nothing here asks for markup or a format. A passage is typed into a box; a
 * clip is chosen from the file picker.
 */
@Component({
  selector: 'astro-exam-structure',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent, MediaFieldComponent, DataStateComponent, ModalDirective],
  templateUrl: './exam-structure.component.html',
  styleUrl: './exam-structure.component.scss',
})
export class ExamStructureComponent {
  private readonly structure = inject(StructureService);
  private readonly questions = inject(QuestionService);
  private readonly exams = inject(ExamService);
  private readonly catalog = inject(CatalogService);

  readonly t = inject(TranslateService).t;

  readonly examId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly saving = signal(false);

  /** What the confirmation dialog is asking about, or null when it is closed. */
  readonly pendingDelete = signal<PendingDelete | null>(null);

  readonly examTitle = signal('');
  readonly sections = signal<ExamSectionDto[]>([]);
  readonly groups = signal<QuestionGroupDto[]>([]);
  readonly topics = signal<TopicDto[]>([]);

  readonly canManage = permissionSignal(P.Exams.Edit);

  readonly sectionDraft = signal<SectionDraft>(emptySection());
  readonly groupDraft = signal<GroupDraft>(emptyGroup());

  readonly noSections = computed(() => !this.loading() && this.sections().length === 0);
  readonly noGroups = computed(() => !this.loading() && this.groups().length === 0);

  private loadedId?: string;

  constructor() {
    effect(() => {
      const id = this.examId();

      if (!id || id === this.loadedId) {
        return;
      }

      this.loadedId = id;

      this.exams.get(id).subscribe({
        next: exam => {
          this.examTitle.set(exam.title);

          // Topics from this exam's own domain only. Offering every topic in the
          // organisation is how a listening section ends up tagged "welding".
          this.catalog.getCategories().subscribe({
            next: categories =>
              this.topics.set(categories.find(c => c.id === exam.categoryId)?.topics ?? []),
          });
        },
        error: () => this.examTitle.set(''),
      });

      this.load();
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.structure.getSections(this.examId()).subscribe({
      next: sections => {
        this.sections.set(sections);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });

    this.questions.getGroups(this.examId()).subscribe({
      next: groups => this.groups.set(groups),
      error: () => undefined,
    });
  }

  // ---------------------------------------------------------------- sections

  newSection(): void {
    const next = this.sections().length;

    this.sectionDraft.set({ ...emptySection(), open: true, displayOrder: next });
  }

  editSection(section: ExamSectionDto): void {
    this.sectionDraft.set({
      open: true,
      id: section.id,
      name: section.name,
      instructions: section.instructions ?? '',
      topicId: section.topicId ?? '',
      timeLimitInMinutes: section.timeLimitInMinutes ?? null,
      minimumPercentage: section.minimumPercentage ?? null,
      questionsPerForm: section.questionsPerForm ?? null,
      isQualifying: section.isQualifying,
      displayOrder: section.displayOrder,
    });
  }

  cancelSection(): void {
    this.sectionDraft.set(emptySection());
  }

  patchSection<K extends keyof SectionDraft>(key: K, value: SectionDraft[K]): void {
    this.sectionDraft.update(d => ({ ...d, [key]: value }));
  }

  saveSection(): void {
    const draft = this.sectionDraft();
    const name = draft.name.trim();

    if (!name) {
      return;
    }

    const body: CreateUpdateExamSectionDto = {
      examId: this.examId(),
      name,
      instructions: draft.instructions.trim() || null,
      topicId: draft.topicId || null,
      timeLimitInMinutes: draft.timeLimitInMinutes,
      minimumPercentage: draft.minimumPercentage,
      questionsPerForm: draft.questionsPerForm,
      isQualifying: draft.isQualifying,
      displayOrder: draft.displayOrder,
    };

    this.run(
      draft.id ? this.structure.updateSection(draft.id, body) : this.structure.createSection(body),
      () => this.sectionDraft.set(emptySection()),
    );
  }

  /**
   * Asked, not done. Deleting a section takes a part out of the exam's shape —
   * its clock, its floor and its qualifying flag go with it — and the row it
   * sits on is one pixel from Edit. Every other list in this product confirms
   * before it deletes; this one did not.
   */
  askDeleteSection(section: ExamSectionDto): void {
    this.pendingDelete.set({ kind: 'section', id: section.id, name: section.name });
  }

  askDeleteGroup(group: QuestionGroupDto): void {
    this.pendingDelete.set({
      kind: 'passage',
      id: group.id,
      name: group.instructions || this.t('::Stimulus:Untitled'),
    });
  }

  cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  confirmDelete(): void {
    const pending = this.pendingDelete();

    if (!pending) {
      return;
    }

    const request =
      pending.kind === 'section'
        ? this.structure.deleteSection(pending.id)
        : this.questions.deleteGroup(pending.id);

    this.run(request, () => this.pendingDelete.set(null));
  }

  // ---------------------------------------------------------------- passages

  newGroup(): void {
    this.groupDraft.set({ ...emptyGroup(), open: true, displayOrder: this.groups().length });
  }

  editGroup(group: QuestionGroupDto): void {
    this.groupDraft.set({
      open: true,
      id: group.id,
      instructions: group.instructions ?? '',
      stimulusText: group.stimulusText ?? '',
      stimulusBlobName: group.stimulusBlobName ?? '',
      stimulusMediaType: group.stimulusMediaType ?? '',
      displayOrder: group.displayOrder,
    });
  }

  cancelGroup(): void {
    this.groupDraft.set(emptyGroup());
  }

  patchGroup<K extends keyof GroupDraft>(key: K, value: GroupDraft[K]): void {
    this.groupDraft.update(d => ({ ...d, [key]: value }));
  }

  /** The file picker reports both at once, so they cannot disagree. */
  setStimulusMedia(media: { blobName?: string; mediaType?: string }): void {
    this.groupDraft.update(d => ({
      ...d,
      stimulusBlobName: media.blobName ?? '',
      stimulusMediaType: media.mediaType ?? '',
    }));
  }

  saveGroup(): void {
    const draft = this.groupDraft();

    // A passage with neither text nor a clip is an empty box the taker will be
    // shown. Refused here rather than saved and wondered about later.
    if (!draft.stimulusText.trim() && !draft.stimulusBlobName) {
      this.actionError.set(this.t('::Stimulus:NeedsContent'));
      return;
    }

    const body = {
      examId: this.examId(),
      instructions: draft.instructions.trim() || null,
      stimulusText: draft.stimulusText.trim() || null,
      stimulusBlobName: draft.stimulusBlobName || null,
      stimulusMediaType: draft.stimulusMediaType || null,
      displayOrder: draft.displayOrder,
    };

    this.run(
      draft.id ? this.questions.updateGroup(draft.id, body) : this.questions.createGroup(body),
      () => this.groupDraft.set(emptyGroup()),
    );
  }

  // ------------------------------------------------------------------ plumbing

  private run<T>(request: Observable<T>, done: () => void): void {
    this.saving.set(true);
    this.actionError.set(null);

    request.subscribe({
      next: () => {
        done();
        this.saving.set(false);
        this.load();
      },
      error: (err: unknown) => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  private reason(err: unknown): string {
    return failureReason(err, this.t);
  }
}

interface SectionDraft {
  open: boolean;
  id: string | null;
  name: string;
  instructions: string;
  topicId: string;
  timeLimitInMinutes: number | null;
  minimumPercentage: number | null;
  questionsPerForm: number | null;
  isQualifying: boolean;
  displayOrder: number;
}

interface GroupDraft {
  open: boolean;
  id: string | null;
  instructions: string;
  stimulusText: string;
  stimulusBlobName: string;
  stimulusMediaType: string;
  displayOrder: number;
}

const emptySection = (): SectionDraft => ({
  open: false,
  id: null,
  name: '',
  instructions: '',
  topicId: '',
  timeLimitInMinutes: null,
  minimumPercentage: null,
  questionsPerForm: null,
  isQualifying: false,
  displayOrder: 0,
});

const emptyGroup = (): GroupDraft => ({
  open: false,
  id: null,
  instructions: '',
  stimulusText: '',
  stimulusBlobName: '',
  stimulusMediaType: '',
  displayOrder: 0,
});

interface PendingDelete {
  readonly kind: 'section' | 'passage';
  readonly id: string;
  readonly name: string;
}
