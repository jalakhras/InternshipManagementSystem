import {
  Component,
  ComponentRef,
  ViewContainerRef,
  computed,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RichTextComponent } from '../../shared/ui/rich-text.component';
import { MediaFieldComponent } from '../../shared/ui/media-field.component';

import { QuestionService } from '../../core/api/question.service';
import {
  CreateUpdateQuestionDto,
  QuestionDifficulty,
  QuestionTypeDescriptor,
} from '../../core/api/assessment.models';
import { TranslateService } from '../../core/translate.service';
import { PAYLOAD_EDITORS } from './payload/payload-editor';

/**
 * Writing a question.
 *
 * Thirteen types have to feel like one screen. They do that by sharing everything
 * except one slot: the prompt, marks, competency, difficulty, explanation and
 * per-question timer are the same for all of them, and only the middle changes.
 *
 * The type-specific editor is loaded into that slot at runtime from a registry, so
 * adding a type is one component and one line — the same promise the server makes
 * with IQuestionGrader. A type with no editor still saves through a raw JSON
 * field, because the server deliberately accepts types this build does not know.
 */
@Component({
  selector: 'astro-question-form',
  standalone: true,
  imports: [FormsModule, RichTextComponent, MediaFieldComponent],
  templateUrl: './question-form.component.html',
  styleUrl: './question-form.component.scss',
})
export class QuestionFormComponent {
  private readonly questions = inject(QuestionService);

  readonly t = inject(TranslateService).t;

  readonly examId = input.required<string>();
  readonly questionId = input<string>();

  /** The question already fetched, so the form is not reloaded over the author's edits. */
  private loadedId?: string;

  /** Only the chosen type, so the mounting effect ignores every other form change. */
  private readonly selectedTypeId = computed(() => this.form().type);

  /** The question's marks, watched separately so the rubric can follow them. */
  private readonly questionScore = computed(() => this.form().score);

  /** The mounted editor, kept so its inputs can be updated without rebuilding it. */
  private editorRef?: ComponentRef<{ payloadChange: { subscribe(fn: (value: string) => void): unknown } }>;

  private readonly editorHost = viewChild('editorHost', { read: ViewContainerRef });

  readonly types = signal<QuestionTypeDescriptor[]>([]);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  /** Type is chosen first: it decides the shape of everything below it. */
  readonly showTypePicker = signal(true);

  readonly form = signal<CreateUpdateQuestionDto>({
    examId: '',
    text: '',
    type: '',
    payload: '{}',
    difficulty: QuestionDifficulty.Medium,
    score: 1,
    displayOrder: 0,
    isActive: true,
  });

  readonly selectedType = computed(() =>
    this.types().find(t => t.type === this.form().type),
  );

  /** True when this build ships no editor for the chosen type; the raw field appears instead. */
  readonly hasEditor = computed(() => !!PAYLOAD_EDITORS[this.form().type]);

  readonly difficulties = [
    { value: QuestionDifficulty.Easy, labelKey: '::Question:Difficulty:Easy' },
    { value: QuestionDifficulty.Medium, labelKey: '::Question:Difficulty:Medium' },
    { value: QuestionDifficulty.Hard, labelKey: '::Question:Difficulty:Hard' },
  ];

  readonly QuestionDifficulty = QuestionDifficulty;

  constructor() {
    this.questions.getTypes().subscribe({
      next: types => this.types.set(types),
      error: err => this.error.set(this.reason(err)),
    });

    effect(() => {
      this.patch('examId', this.examId());
    });

    // Mounts the editor for the chosen type.
    //
    // The type is read through a computed, not off the form. Reading form().type
    // directly tracks the whole form object, and every patch replaces it — so a
    // single keystroke tore the editor down and built a new one, losing focus and
    // flashing the panel. A computed emits only when the type itself changes.
    effect(() => {
      const type = this.selectedTypeId();
      const host = this.editorHost();

      if (!host) {
        return;
      }

      host.clear();

      const load = PAYLOAD_EDITORS[type];
      if (!load) {
        return;
      }

      void load().then(componentType => {
        const ref = host.createComponent(componentType);

        ref.setInput('payload', this.form().payload);

        // Some editors want context the frame owns: the choice editor needs the
        // type to know how many answers may be correct, and the rubric editor
        // needs the question's marks to flag a rubric that does not add up.
        ref.setInput('type', type);
        ref.setInput('questionScore', this.questionScore());

        ref.instance.payloadChange.subscribe((payload: string) => this.patch('payload', payload));

        this.editorRef = ref as typeof this.editorRef;
      });
    });

    // Marks reach the mounted editor without rebuilding it. They used to arrive
    // by rebuilding — which is why changing them worked and typing anything else
    // destroyed the editor mid-edit.
    effect(() => {
      const score = this.questionScore();

      this.editorRef?.setInput('questionScore', score);
    });

    // Not read directly in the constructor: withComponentInputBinding() sets a
    // routed component's inputs after construction, so this was always undefined
    // and an existing question opened as a blank new one.
    effect(() => {
      const id = this.questionId();

      if (!id || id === this.loadedId) {
        return;
      }

      this.loadedId = id;
      this.load(id);
    });
  }

  private load(id: string): void {
    this.questions.get(id).subscribe({
      next: question => {
        this.form.set({
          examId: question.examId,
          questionGroupId: question.questionGroupId,
          text: question.text,
          type: question.type,
          payload: question.payload,
          topicId: question.topicId,
          difficulty: question.difficulty,
          score: question.score,
          explanation: question.explanation,
          timeLimitInSeconds: question.timeLimitInSeconds,
          mediaBlobName: question.mediaBlobName,
          mediaType: question.mediaType,
          displayOrder: question.displayOrder,
          isActive: question.isActive,
        });
        this.showTypePicker.set(false);
      },
      error: err => this.error.set(this.reason(err)),
    });
  }

  chooseType(type: string): void {
    this.patch('type', type);
    // A fresh payload: carrying options across from a numeric question would
    // produce something no grader can read.
    this.patch('payload', '{}');
    this.showTypePicker.set(false);
  }

  patch<K extends keyof CreateUpdateQuestionDto>(key: K, value: CreateUpdateQuestionDto[K]): void {
    this.form.update(f => ({ ...f, [key]: value }));
  }

  setMedia(media: { blobName?: string; mediaType?: string }): void {
    this.patch('mediaBlobName', media.blobName);
    this.patch('mediaType', media.mediaType);
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);

    const id = this.questionId();
    const request = id
      ? this.questions.update(id, this.form())
      : this.questions.create(this.form());

    request.subscribe({
      next: () => this.saving.set(false),
      error: err => {
        // The server refuses a payload no grader could read, and its message is
        // already a localised sentence — pass it through rather than inventing one.
        this.error.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  private reason(err: unknown): string {
    const e = err as { error?: { error?: { message?: string } }; message?: string };
    return e?.error?.error?.message ?? e?.message ?? this.t('::UnknownError');
  }
}
