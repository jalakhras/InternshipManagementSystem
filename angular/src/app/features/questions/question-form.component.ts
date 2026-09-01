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
import { Router, RouterLink } from '@angular/router';
import { RichTextComponent } from '../../shared/ui/rich-text.component';
import { MediaFieldComponent } from '../../shared/ui/media-field.component';

import { CatalogService } from '../../core/api/catalog.service';
import { CategoryDto } from '../../core/api/catalog.models';
import { QuestionGroupDto } from '../../core/api/assessment.models';
import { QuestionService } from '../../core/api/question.service';
import {
  CreateUpdateQuestionDto,
  QuestionDifficulty,
  QuestionTypeDescriptor,
} from '../../core/api/assessment.models';
import { ExamSectionDto } from '../../core/api/structure.models';
import { TranslateService } from '../../core/translate.service';
import { PAYLOAD_EDITORS } from './payload/payload-editor';
import { QuestionSectionsService } from './question-sections.service';
import { failureReason } from '../../core/failure';

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
  imports: [FormsModule, RichTextComponent, MediaFieldComponent, RouterLink],
  templateUrl: './question-form.component.html',
  styleUrl: './question-form.component.scss',
})
export class QuestionFormComponent {
  private readonly questions = inject(QuestionService);
  private readonly catalog = inject(CatalogService);
  private readonly sectionsApi = inject(QuestionSectionsService);

  readonly t = inject(TranslateService).t;

  /**
   * The exam this question is being written into, or absent in the bank.
   *
   * In the bank the question is filed under a domain and a level instead, and
   * every exam at that level can draw it. That path had no screen, so the only
   * questions this product could produce were ones tied to a single paper.
   */
  readonly examId = input<string>();
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
  private readonly router = inject(Router);

  readonly saving = signal(false);

  /** Said out loud after a save, because silence reads as failure. */
  readonly saved = signal(false);

  /**
   * The server refused this text as a duplicate of another question in the exam.
   *
   * Held rather than acted on: the author is shown what the refusal was and
   * decides. Saving anyway is a decision somebody made, which is the whole
   * difference between this and having allowed the duplicate silently.
   */
  readonly duplicate = signal(false);
  readonly error = signal<string | null>(null);

  /** Type is chosen first: it decides the shape of everything below it. */
  readonly showTypePicker = signal(true);

  /** Domains, with their levels and topics, for the filing section. */
  readonly categories = signal<CategoryDto[]>([]);

  /**
   * The passages this exam has, so a question can join one.
   *
   * Only inside an exam: a passage belongs to a paper, and a bank question that
   * pointed at one would be undrawable by every other exam at its level.
   */
  readonly passages = signal<QuestionGroupDto[]>([]);

  /**
   * The parts of this exam a question can be filed into.
   *
   * Empty for an exam that was never split, and the picker stays hidden then:
   * a single-part paper has nowhere to file anything, and offering "unfiled" as
   * the only choice would be a control with one option.
   */
  readonly sections = signal<ExamSectionDto[]>([]);

  /**
   * Levels under the chosen domain. A level belongs to one ladder, so offering
   * every organisation's at once is how a beginners' safety item ends up at B2.
   */
  readonly levels = computed(() =>
    this.categories().find(c => c.id === this.form().categoryId)?.levels ?? [],
  );

  readonly topics = computed(() =>
    this.categories().find(c => c.id === this.form().categoryId)?.topics ?? [],
  );

  /**
   * A bank question has to say which domain it belongs to.
   *
   * Not a nicety: the drawing rule is domain plus level, so a bank question with
   * no domain can never be drawn by anything. It would save successfully and be
   * invisible forever, which is the worst kind of accepted input.
   */
  readonly needsCategory = computed(() => !this.examId() && !this.form().categoryId);

  readonly form = signal<CreateUpdateQuestionDto>({
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
      // Undefined rather than an empty string. The server reads "no exam" as
      // "bank question", and an empty string is a malformed id.
      this.patch('examId', this.examId() ?? undefined);
    });

    // The catalogue, for the filing pickers. A failure here costs the pickers and
    // nothing else — somebody fixing a typo in a prompt should not be stopped.
    this.catalog.getCategories().subscribe({
      next: categories => this.categories.set(categories),
    });

    effect(() => {
      const exam = this.examId();

      if (!exam) {
        return;
      }

      this.questions.getGroups(exam).subscribe({
        next: groups => this.passages.set(groups),
      });

      // The sections cost the picker if they fail and nothing else. The value
      // already on the form is what gets saved either way, so a section that
      // cannot be named is still a section that survives the edit.
      this.sectionsApi.getSections(exam).subscribe({
        next: sections => this.sections.set(sections),
      });
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

  /**
   * Fills the form from the stored question.
   *
   * Every field is copied deliberately, and a field left out of this list is not
   * merely unedited — the server assigns the whole question from what it is
   * sent, so an omission here erases the stored value on the next save. That is
   * what happened to the section: a question filed into "Listening" lost it the
   * moment anyone opened it to fix a typo.
   */
  private load(id: string): void {
    this.questions.get(id).subscribe({
      next: question => {
        this.form.set({
          examId: question.examId,
          categoryId: question.categoryId,
          levelId: question.levelId,
          examSectionId: question.examSectionId,
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
    this.saved.set(false);

    // A changed question is no longer the one that was refused.
    if (key === 'text') {
      this.duplicate.set(false);
    }


    this.form.update(f => ({ ...f, [key]: value }));
  }

  setMedia(media: { blobName?: string; mediaType?: string }): void {
    this.patch('mediaBlobName', media.blobName);
    this.patch('mediaType', media.mediaType);
  }

  /**
   * Changing the domain clears the level and the topic.
   *
   * Both belong to the domain that was just replaced. Leaving them would file the
   * question under a level from another ladder, which reads fine on the screen.
   */
  setCategory(categoryId: string): void {
    this.form.update(f => ({
      ...f,
      categoryId: categoryId || undefined,
      levelId: undefined,
      topicId: undefined,
    }));
  }

  /**
   * Save the question the server just refused as a duplicate.
   *
   * The same request with the author's answer attached. The flag lives on the
   * request rather than in the component's state so that it cannot leak into
   * the next question they write: it is true for this press and no other.
   */
  saveAnyway(): void {
    this.duplicate.set(false);
    this.save(true);
  }

  save(allowDuplicateText = false): void {
    if (this.needsCategory()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const id = this.questionId();
    const body = allowDuplicateText ? { ...this.form(), allowDuplicateText: true } : this.form();

    const request = id
      ? this.questions.update(id, body)
      : this.questions.create(body);

    request.subscribe({
      next: stored => {
        this.saving.set(false);
        this.saved.set(true);

        // Editing: they came from the list to change one thing, and it is
        // changed. Keeping them on a form they are finished with makes them
        // find their own way back.
        if (id) {
          this.router.navigate(['/exams', this.examId(), 'questions']);

          return;
        }

        // Creating: the form now holds a question that exists, and the address
        // has to say so.
        //
        // It did not, and the cost was a duplicate: after a successful create
        // the form still believed it was writing a new question, so a second
        // press wrote a second copy into the bank. That is not a rare mistake —
        // it is what a person does when a save takes a second and nothing on
        // the screen says it worked.
        //
        // `replaceUrl` because the address they were on no longer describes
        // anything: reloading it would offer to create the question again.
        this.router.navigate(
          ['/exams', this.examId(), 'questions', stored.id],
          { replaceUrl: true });
      },
      error: err => {
        // One refusal has an answer the author can give, so it is offered rather
        // than only reported: another question in this exam already reads the
        // same. Everything else is a refusal they can only fix by changing what
        // they wrote.
        if (this.codeOf(err) === 'IMS:Question:DuplicateText') {
          this.duplicate.set(true);
        }

        // The server refuses a payload no grader could read, and its message is
        // already a localised sentence — pass it through rather than inventing one.
        this.error.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  /**
   * The server's own error code, when it sent one.
   *
   * Read rather than matched against the message: a message is written for a
   * person and gets rewritten, and matching on it makes the behaviour depend on
   * the wording — and on which language the reader happens to be in.
   */
  private codeOf(err: unknown): string | undefined {
    return (err as { error?: { error?: { code?: string } } })?.error?.error?.code;
  }

  private reason(err: unknown): string {
    // The shared reader, not a local copy of the decision. This screen kept its
    // own, and its own still ended at HttpErrorResponse.message — an internal
    // URL and a status code, shown to whoever was trying to get their work
    // done. Nineteen screens were changed and these two were missed, which is
    // the ordinary way a sweep leaves something behind.
    return failureReason(err, this.t);
  }
}
