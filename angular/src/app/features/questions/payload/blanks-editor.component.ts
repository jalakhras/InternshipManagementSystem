import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { BlankSpec, FillInTheBlankPayload, newId, readPayload, writePayload } from './payload.models';

/**
 * Blanks, and every spelling that counts as filling one.
 *
 * The competing design was a marker in the prompt — `The capital is [[Paris]]` —
 * and it was rejected on the owner's rule that no input may require a syntax an
 * author has to learn. A teacher who mistypes the brackets gets a question that
 * silently has no blanks in it, and nothing tells them.
 *
 * So the blanks are a numbered list beside the prompt instead. The author writes
 * the sentence with underscores wherever they like, and lists what each gap
 * accepts. Nothing to memorise, and nothing that can be typed slightly wrong.
 *
 * The accepted answers are a list rather than one string for the reason every
 * language teacher already knows: marking "color" wrong because the key said
 * "colour" tests a spelling convention rather than the thing the question asked.
 */
@Component({
  selector: 'astro-blanks-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <p class="lede">{{ t('::Question:Blanks:Lede') }}</p>

    <div class="blanks">
      @for (blank of blanks(); track blank.id; let i = $index) {
        <div class="blank">
          <span class="blank__number astro-numeric">{{ i + 1 }}</span>

          <div class="blank__body">
            <input
              type="text"
              class="form-control"
              [ngModel]="blank.acceptedAnswers.join(' | ')"
              (ngModelChange)="setAnswers(blank.id, $event)"
              [placeholder]="t('::Question:Blanks:Placeholder')"
              [attr.aria-label]="t('::Question:Blanks:Answers') + ' ' + (i + 1)" />

            <small class="blank__hint">{{ t('::Question:Blanks:Answers:Hint') }}</small>
          </div>

          <button
            type="button"
            class="blank__remove"
            [disabled]="blanks().length <= 1"
            [attr.aria-label]="t('::Question:Blanks:Remove')"
            (click)="removeBlank(blank.id)">
            <i class="bi bi-x-lg" aria-hidden="true"></i>
          </button>
        </div>
      }
    </div>

    <div class="actions">
      <button type="button" class="btn btn-sm btn-outline-secondary" (click)="addBlank()">
        <i class="bi bi-plus-lg" aria-hidden="true"></i>
        {{ t('::Question:Blanks:Add') }}
      </button>

      <label class="partial">
        <input type="checkbox" [checked]="caseSensitive()" (change)="toggleCase()" />
        <span>
          <strong>{{ t('::Question:Blanks:CaseSensitive') }}</strong>
          <small>{{ t('::Question:Blanks:CaseSensitive:Hint') }}</small>
        </span>
      </label>

      <label class="partial">
        <input type="checkbox" [checked]="allowPartialCredit()" (change)="togglePartial()" />
        <span>
          <strong>{{ t('::Question:PartialCredit') }}</strong>
          <small>{{ t('::Question:Blanks:PartialCredit:Hint') }}</small>
        </span>
      </label>
    </div>

    @for (warning of warnings(); track warning) {
      <p class="warning" role="status">
        <i class="bi bi-exclamation-triangle" aria-hidden="true"></i>
        {{ t('::' + warning) }}
      </p>
    }
  `,
  styles: `
    :host { display: block; }

    .lede { margin-block: 0 var(--astro-space-3); color: var(--text-muted); font-size: .875rem; }

    .blanks { display: grid; gap: var(--astro-space-3); }

    .blank {
      display: grid;
      grid-template-columns: 2rem 1fr 2rem;
      align-items: start;
      gap: var(--astro-space-2);
    }

    .blank__number {
      display: grid;
      place-items: center;
      inline-size: 2rem;
      block-size: 2rem;
      border-radius: 50%;
      background: var(--surface-sunken);
      color: var(--text-secondary);
      font-size: .8125rem;
      font-weight: 600;
    }

    .blank__body { display: grid; gap: var(--astro-space-1); }
    .blank__hint { color: var(--text-muted); font-size: .8125rem; }

    .blank__remove {
      display: grid;
      place-items: center;
      inline-size: 2rem;
      block-size: 2rem;
      border: 0;
      border-radius: var(--astro-radius-sm);
      background: transparent;
      color: var(--text-muted);
      cursor: pointer;

      &:hover:not(:disabled) { color: var(--astro-fail-600); }
      &:disabled { opacity: .3; cursor: default; }
    }

    .actions {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-start;
      gap: var(--astro-space-4);
      margin-block-start: var(--astro-space-4);
    }

    .partial {
      display: flex;
      gap: var(--astro-space-2);
      cursor: pointer;

      span { display: grid; }
      small { color: var(--text-muted); }
    }

    .warning {
      display: flex;
      gap: var(--astro-space-2);
      margin-block-start: var(--astro-space-3);
      color: var(--astro-pending-600);
      font-size: .875rem;
    }
  `,
})
export class BlanksEditorComponent {
  readonly t = inject(TranslateService).t;

  readonly payload = input<string>('');
  readonly payloadChange = output<string>();

  readonly blanks = signal<BlankSpec[]>([]);
  readonly caseSensitive = signal(false);
  readonly allowPartialCredit = signal(true);

  readonly warnings = computed<string[]>(() => {
    const blanks = this.blanks();
    const found: string[] = [];

    if (blanks.length === 0) {
      found.push('IMS:Question:NeedsOneBlank');
    }

    // A blank accepting nothing marks every candidate wrong, and it reads as a
    // hard question rather than a broken one.
    if (blanks.some(b => b.acceptedAnswers.length === 0)) {
      found.push('IMS:Question:BlankHasNoAnswer');
    }

    return found;
  });

  constructor() {
    effect(() => {
      const parsed = readPayload<FillInTheBlankPayload>(this.payload(), {
        blanks: [],
        caseSensitive: false,
        allowPartialCredit: true,
      });

      this.blanks.set(parsed.blanks.length > 0 ? parsed.blanks : [this.blankSpec()]);
      this.caseSensitive.set(parsed.caseSensitive === true);
      this.allowPartialCredit.set(parsed.allowPartialCredit ?? true);
    });
  }

  addBlank(): void {
    this.blanks.update(list => [...list, this.blankSpec()]);
    this.emit();
  }

  removeBlank(id: string): void {
    this.blanks.update(list => list.filter(b => b.id !== id));
    this.emit();
  }

  /**
   * Splits on a vertical bar, which is shown in the field's own hint rather than
   * assumed. One separator, chosen because it does not appear in ordinary prose
   * the way a comma does — "Paris, France" is one answer, not two.
   */
  setAnswers(id: string, raw: string): void {
    const acceptedAnswers = raw
      .split('|')
      .map(part => part.trim())
      .filter(part => part.length > 0);

    this.blanks.update(list => list.map(b => (b.id === id ? { ...b, acceptedAnswers } : b)));
    this.emit();
  }

  toggleCase(): void {
    this.caseSensitive.update(v => !v);
    this.emit();
  }

  togglePartial(): void {
    this.allowPartialCredit.update(v => !v);
    this.emit();
  }

  private blankSpec(): BlankSpec {
    return { id: newId('b'), acceptedAnswers: [] };
  }

  private emit(): void {
    this.payloadChange.emit(
      writePayload({
        blanks: this.blanks(),
        caseSensitive: this.caseSensitive(),
        allowPartialCredit: this.allowPartialCredit(),
      }),
    );
  }
}
