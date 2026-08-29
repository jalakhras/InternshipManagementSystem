import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { CodePayload, readPayload, writePayload } from './payload.models';

/**
 * A code question: what language, what the candidate starts from, and what the
 * program should print.
 *
 * The comparison is against printed output, not against execution semantics —
 * the platform does not run code, and saying so plainly in the form is better
 * than letting an author discover it from a result. If a tenant needs real
 * execution, a runner is integrated behind the same grader.
 *
 * Leaving the expected output empty is allowed and is sometimes right: a
 * question asking for an approach rather than a program has no single output,
 * and it goes to a human. The form says which of the two the author has built.
 */
@Component({
  selector: 'astro-code-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="field">
      <label class="form-label" for="codeLanguage">{{ t('::Question:Code:Language') }}</label>
      <select
        id="codeLanguage"
        class="form-select"
        [ngModel]="language()"
        (ngModelChange)="setLanguage($event)">
        <option value="">{{ t('::Question:Code:Language:Any') }}</option>
        @for (option of languages; track option) {
          <option [value]="option">{{ option }}</option>
        }
      </select>
      <p class="hint">{{ t('::Question:Code:Language:Hint') }}</p>
    </div>

    <div class="field">
      <label class="form-label" for="codeStarter">{{ t('::Question:Code:Starter') }}</label>
      <textarea
        id="codeStarter"
        class="form-control astro-ltr code"
        rows="6"
        spellcheck="false"
        [ngModel]="starterTemplate()"
        (ngModelChange)="setStarter($event)"></textarea>
      <p class="hint">{{ t('::Question:Code:Starter:Hint') }}</p>
    </div>

    <div class="field">
      <label class="form-label" for="codeExpected">{{ t('::Question:Code:Expected') }}</label>
      <textarea
        id="codeExpected"
        class="form-control astro-ltr code"
        rows="4"
        spellcheck="false"
        [ngModel]="expectedOutput()"
        (ngModelChange)="setExpected($event)"></textarea>
      <p class="hint">{{ t('::Question:Code:Expected:Hint') }}</p>
    </div>

    <!-- Which of the two questions this is, said while it is still being written
         rather than discovered when the review queue fills up. -->
    <p class="grading" role="status">
      <i class="bi" [class.bi-cpu]="isAutoGraded()" [class.bi-person]="!isAutoGraded()" aria-hidden="true"></i>
      {{ isAutoGraded() ? t('::Question:Code:WillBeAutoGraded') : t('::Question:Code:WillBeManual') }}
    </p>
  `,
  styles: `
    :host { display: block; }

    .field { margin-block-end: var(--astro-space-4); }
    .hint { margin-block: var(--astro-space-1) 0; font-size: .8125rem; color: var(--text-muted); }

    .code {
      font-family: var(--astro-font-mono);
      font-size: .875rem;
      tab-size: 2;
    }

    .grading {
      display: flex;
      align-items: center;
      gap: var(--astro-space-2);
      margin: 0;
      padding: var(--astro-space-2) var(--astro-space-3);
      border-radius: var(--astro-radius-sm);
      background: var(--surface-sunken);
      color: var(--text-secondary);
      font-size: .875rem;
    }
  `,
})
export class CodeEditorComponent {
  readonly t = inject(TranslateService).t;

  readonly payload = input<string>('');
  readonly payloadChange = output<string>();

  readonly language = signal('');
  readonly starterTemplate = signal('');
  readonly expectedOutput = signal('');

  /** Offered rather than typed, so a stored value is one the grader recognises. */
  readonly languages = [
    'C#', 'JavaScript', 'TypeScript', 'Python', 'Java', 'SQL', 'PHP', 'Go', 'Rust', 'C++',
  ];

  readonly isAutoGraded = computed(() => this.expectedOutput().trim().length > 0);

  constructor() {
    effect(() => {
      const parsed = readPayload<CodePayload>(this.payload(), {});

      this.language.set(parsed.language ?? '');
      this.starterTemplate.set(parsed.starterTemplate ?? '');
      this.expectedOutput.set(parsed.expectedOutput ?? '');
    });
  }

  setLanguage(value: string): void {
    this.language.set(value);
    this.emit();
  }

  setStarter(value: string): void {
    this.starterTemplate.set(value);
    this.emit();
  }

  setExpected(value: string): void {
    this.expectedOutput.set(value);
    this.emit();
  }

  private emit(): void {
    this.payloadChange.emit(
      writePayload({
        language: this.language() || undefined,
        starterTemplate: this.starterTemplate() || undefined,
        expectedOutput: this.expectedOutput() || undefined,
      }),
    );
  }
}
