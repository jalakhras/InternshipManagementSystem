import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { TakerQuestion } from '../take.models';

/**
 * A code answer.
 *
 * A code question was answered in the same plain box as an essay, and three
 * things the author had written were dropped on the way.
 *
 * The starter template is the clearest. An author writes the skeleton a
 * candidate is meant to complete — the signature, the imports, the line that
 * says where to begin — the server puts it in the projection and sends it, and
 * the client threw it away. So the author's work reached the candidate's
 * browser and never reached the candidate.
 *
 * The language went the same way. "Write it in Python" is not decoration when
 * the answer is marked by comparing text.
 *
 * And the box itself was wrong in Arabic, which is the language this product is
 * built for first. Code is left to right; a right-to-left box reorders it on
 * screen while the candidate types. The authoring form already knew this — the
 * author's two code boxes carry `astro-ltr` and a monospace face — so the
 * candidate's box was the only one in the product that did not.
 *
 * Tab is deliberately not captured. A textarea that swallows Tab is a keyboard
 * trap: somebody working without a mouse gets into the box and cannot get out
 * of it, mid-exam, with a clock running. Indentation by spaces costs a
 * candidate a keystroke; a trap costs them the paper.
 */
@Component({
  selector: 'astro-code-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="code">
      @if (language()) {
        <p class="code__language">
          <i class="bi bi-code-slash" aria-hidden="true"></i>
          {{ t('::Take:Code:Language', language()) }}
        </p>
      }

      <!-- What to write. The grader compares the candidate's text with what the
           author said the program should print, so a candidate who submits the
           program instead scores nothing — for reading the box the other way,
           not for being wrong. The author is told which of the two questions
           they have written; until now the candidate was not. -->
      <p class="code__asks">
        {{ expectsOutput() ? t('::Take:Code:WriteOutput') : t('::Take:Code:WriteCode') }}
      </p>

      <textarea
        class="form-control code__box astro-ltr"
        rows="12"
        spellcheck="false"
        autocomplete="off"
        autocapitalize="off"
        autocorrect="off"
        [ngModel]="value()"
        (ngModelChange)="set($event)"
        [attr.aria-label]="t('::Take:YourAnswer')"></textarea>

      @if (startedFromTemplate()) {
        <p class="code__note" role="status">{{ t('::Take:Code:StartedFromTemplate') }}</p>
      }
    </div>
  `,
  styles: `
    :host { display: block; }

    .code__language,
    .code__asks {
      margin: 0 0 var(--astro-space-2);
      color: var(--text-secondary);
      font-size: var(--astro-text-sm);
    }

    .code__language {
      display: flex;
      align-items: center;
      gap: var(--astro-space-2);
    }

    .code__box {
      font-family: var(--astro-font-mono);
      font-size: .9375rem;
      line-height: var(--astro-leading-body);
      tab-size: 2;
      min-block-size: 14rem;

      /* Direction, isolation and alignment come from .astro-ltr, which is the
         same class the authoring form puts on its two code boxes. Repeating
         them here is how the two drift apart. */
    }

    .code__note {
      margin: var(--astro-space-2) 0 0;
      color: var(--text-muted);
      font-size: var(--astro-text-sm);
    }
  `,
})
export class CodeAnswerComponent {
  readonly t = inject(TranslateService).t;

  readonly question = input.required<TakerQuestion>();
  readonly response = input<string | undefined>();
  readonly responseChange = output<string>();

  readonly value = signal('');

  /** Said only when the box was filled for them, so it explains what they see. */
  readonly startedFromTemplate = signal(false);

  readonly language = computed(() => (this.question().display['language'] as string | undefined) ?? '');

  /**
   * Whether the answer is the program's output rather than the program.
   *
   * A boolean, never the expected output itself: the candidate needs to know
   * which question they are being asked, not what the answer is.
   */
  readonly expectsOutput = computed(() => this.question().display['expectsOutput'] === true);

  private seeded = false;

  constructor() {
    effect(() => {
      const saved = this.response();
      const question = this.question();

      if (this.seeded) {
        return;
      }

      this.seeded = true;

      if (saved) {
        // A resumed attempt comes back to what the candidate wrote. Seeding the
        // template over the top would delete their work on a reload — which is
        // the one thing an autosaving exam must never do.
        this.value.set(saved);

        return;
      }

      const template = (question.display['starterTemplate'] as string | undefined) ?? '';

      // Shown, not saved. Emitting it would autosave the template as the
      // candidate's answer, and a question they have not touched would be
      // counted as answered — on the map, in the "answered" tally, and in what
      // they believe they still have left to do.
      this.value.set(template);
      this.startedFromTemplate.set(template.length > 0);
    });
  }

  set(value: string): void {
    this.value.set(value);
    this.responseChange.emit(value);
  }
}
