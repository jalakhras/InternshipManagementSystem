import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { ChoiceOption, ChoicePayload, newId, readPayload, writePayload } from './payload.models';

/**
 * Options for single choice, multiple answers and true/false.
 *
 * One editor for three types because the payload is the same shape; what differs
 * is how many options may be correct, which the frame passes in as `type`.
 *
 * The warnings here mirror the server's `QuestionPayloadValidator` exactly. That
 * duplication is deliberate: the server is the authority and will refuse a bad
 * save, but discovering a mistake at save time — after writing four options — is
 * a worse experience than seeing it while typing.
 */
@Component({
  selector: 'astro-choice-editor',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="options">
      @for (option of options(); track option.id; let i = $index) {
        <div class="option" [class.option--correct]="option.isCorrect">
          <label class="option__mark">
            <input
              [type]="isSingleAnswer() ? 'radio' : 'checkbox'"
              name="correct"
              [checked]="option.isCorrect"
              (change)="toggleCorrect(option.id)"
              [attr.aria-label]="t('::Question:MarkCorrect')" />
          </label>

          <input
            type="text"
            class="form-control option__text"
            [ngModel]="option.text"
            (ngModelChange)="setText(option.id, $event)"
            [placeholder]="t('::Question:OptionPlaceholder') + ' ' + (i + 1)"
            [attr.aria-label]="t('::Question:OptionPlaceholder') + ' ' + (i + 1)" />

          @if (weighted()) {
            <!-- The weight is what the option is worth, not a rank. Shown as a
                 number rather than a slider because an author setting 0.6 means
                 0.6, and a slider makes them aim at it. -->
            <input
              type="number"
              class="form-control option__weight astro-numeric"
              step="0.1"
              min="-1"
              max="1"
              [ngModel]="option.weight ?? 0"
              (ngModelChange)="setWeight(option.id, $event)"
              [attr.aria-label]="t('::Question:Weight') + ' ' + (i + 1)" />

            <span class="option__band">{{ t(bandKey(option)) }}</span>
          }

          <button
            type="button"
            class="option__remove"
            [disabled]="options().length <= 2"
            [attr.aria-label]="t('::Question:RemoveOption')"
            (click)="removeOption(option.id)">
            <i class="bi bi-x-lg" aria-hidden="true"></i>
          </button>
        </div>
      }
    </div>

    <div class="actions">
      <button type="button" class="btn btn-sm btn-outline-secondary" (click)="addOption()">
        <i class="bi bi-plus-lg" aria-hidden="true"></i>
        {{ t('::Question:AddOption') }}
      </button>

      <!-- Offered on every choice type: a single-choice question with one best
           answer and one defensible one is the case that prompted this. -->
      <label class="partial">
        <input type="checkbox" [checked]="weighted()" (change)="toggleWeighted()" />
        <span>
          <strong>{{ t('::Question:Weighted') }}</strong>
          <small>{{ t('::Question:Weighted:Hint') }}</small>
        </span>
      </label>

      @if (!isSingleAnswer() && !weighted()) {
        <label class="partial">
          <input
            type="checkbox"
            [checked]="allowPartialCredit()"
            (change)="togglePartialCredit()" />
          <span>
            <strong>{{ t('::Question:PartialCredit') }}</strong>
            <small>{{ t('::Question:PartialCredit:Hint') }}</small>
          </span>
        </label>
      }
    </div>

    <!-- Same checks the server runs, surfaced while typing rather than at save. -->
    @for (warning of warnings(); track warning) {
      <p class="warning" role="status">
        <i class="bi bi-exclamation-triangle" aria-hidden="true"></i>
        {{ t('::' + warning) }}
      </p>
    }
  `,
  styleUrl: './choice-editor.component.scss',
})
export class ChoiceEditorComponent {
  readonly t = inject(TranslateService).t;

  readonly payload = input<string>('');
  readonly type = input<string>('single-choice');
  readonly payloadChange = output<string>();

  readonly options = signal<ChoiceOption[]>([]);
  readonly allowPartialCredit = signal(false);
  readonly weighted = signal(false);

  /** Single choice and true/false accept exactly one correct option. */
  readonly isSingleAnswer = computed(() => this.type() !== 'multi-select');

  readonly warnings = computed<string[]>(() => {
    const options = this.options();
    const correct = options.filter(o => o.isCorrect).length;
    const found: string[] = [];

    if (options.length < 2) {
      found.push('IMS:Question:NeedsTwoOptions');
    }

    if (correct === 0) {
      found.push('IMS:Question:NoCorrectOption');
    }

    if (this.isSingleAnswer() && correct > 1) {
      found.push('IMS:Question:SingleChoiceHasManyCorrect');
    }

    // Every option correct means selecting everything is right, which measures
    // nothing — and is the exact shape the old scoring bug rewarded.
    if (!this.isSingleAnswer() && options.length > 0 && correct === options.length) {
      found.push('IMS:Question:AllOptionsCorrect');
    }

    if (new Set(options.map(o => o.id)).size !== options.length) {
      found.push('IMS:Question:DuplicateOptionId');
    }

    if (this.weighted()) {
      if (options.some(o => o.weight === undefined || o.weight === null)) {
        found.push('IMS:Question:WeightMissing');
      }

      if (options.some(o => o.weight !== undefined && (o.weight < -1 || o.weight > 1))) {
        found.push('IMS:Question:WeightOutOfRange');
      }

      if (options.some(o => o.isCorrect !== (o.weight === 1))) {
        found.push('IMS:Question:WeightConflictsWithCorrectFlag');
      }

      // Weighted mode switches off the all-or-nothing rule, so without something
      // priced below zero, selecting everything is never worse than choosing well.
      if (!this.isSingleAnswer() && options.every(o => (o.weight ?? 0) >= 0)) {
        found.push('IMS:Question:AllWeightsPositive');
      }
    }

    return found;
  });

  constructor() {
    // Reads the incoming payload once per change of the input rather than on every
    // keystroke, so typing does not fight the parse.
    effect(() => {
      const parsed = readPayload<ChoicePayload>(this.payload(), {
        options: [],
        allowPartialCredit: false,
      });

      this.options.set(
        parsed.options.length > 0
          ? parsed.options
          : // A new question opens with two empty options rather than none: the
            // minimum a choice question can be, so the author starts by typing
            // rather than by pressing add.
            [
              { id: newId('o'), text: '', isCorrect: false },
              { id: newId('o'), text: '', isCorrect: false },
            ],
      );

      this.allowPartialCredit.set(parsed.allowPartialCredit ?? false);
      this.weighted.set(parsed.weighted === true);
    });
  }

  addOption(): void {
    this.options.update(list => [...list, { id: newId('o'), text: '', isCorrect: false }]);
    this.emit();
  }

  removeOption(id: string): void {
    this.options.update(list => list.filter(o => o.id !== id));
    this.emit();
  }

  setWeight(id: string, value: number | string): void {
    const weight = Number(value);

    this.options.update(list =>
      list.map(o =>
        o.id === id
          // The correct flag follows the weight rather than being set separately:
          // the server refuses a question where the two disagree, and two controls
          // for one fact is how they come to disagree.
          ? { ...o, weight, isCorrect: weight === 1 }
          : o,
      ),
    );

    this.emit();
  }

  /** The band an option falls in, for the label beside its weight. */
  bandKey(option: ChoiceOption): string {
    const weight = option.weight ?? 0;

    if (weight === 1) {
      return '::Question:Weight:Best';
    }

    if (weight > 0) {
      return '::Question:Weight:Acceptable';
    }

    return weight < 0 ? '::Question:Weight:Penalised' : '::Question:Weight:Neutral';
  }

  toggleWeighted(): void {
    const on = !this.weighted();

    this.weighted.set(on);

    if (on) {
      // Seeded from what the author has already said: the option they marked
      // correct becomes the best answer, the rest start at nothing. Leaving the
      // weights blank would show four validation warnings the moment the toggle
      // is pressed.
      this.options.update(list => list.map(o => ({ ...o, weight: o.isCorrect ? 1 : 0 })));
    }

    this.emit();
  }

  setText(id: string, text: string): void {
    this.options.update(list => list.map(o => (o.id === id ? { ...o, text } : o)));
    this.emit();
  }

  toggleCorrect(id: string): void {
    this.options.update(list =>
      list.map(o => {
        if (o.id === id) {
          return { ...o, isCorrect: this.isSingleAnswer() ? true : !o.isCorrect };
        }

        // Single choice clears the others, so the author cannot create the
        // "nobody can pass this" state by accident.
        return this.isSingleAnswer() ? { ...o, isCorrect: false } : o;
      }),
    );

    this.emit();
  }

  togglePartialCredit(): void {
    this.allowPartialCredit.update(v => !v);
    this.emit();
  }

  private emit(): void {
    const weighted = this.weighted();

    this.payloadChange.emit(
      writePayload({
        // Weights are stripped when weighting is off, and the flag is omitted
        // rather than written as false. A question that does not use this must
        // serialise exactly as it did before the feature existed — otherwise
        // every save rewrites every payload in the bank for no reason.
        options: this.options().map(o => (weighted ? o : { ...o, weight: undefined })),
        allowPartialCredit: this.allowPartialCredit(),
        ...(weighted ? { weighted: true } : {}),
      }),
    );
  }
}
