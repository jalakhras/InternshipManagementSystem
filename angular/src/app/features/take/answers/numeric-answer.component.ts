import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { TakerQuestion } from '../take.models';

/**
 * A number.
 *
 * The unit is shown beside the field and is not part of the answer, so a
 * candidate writing "1250" and one writing "1250 pips" are not marked
 * differently for a decision the question already made for them.
 *
 * `inputmode="decimal"` rather than `type="number"`: a numeric keyboard on a
 * phone, without the spinner arrows that turn a mistimed scroll into a changed
 * answer.
 */
@Component({
  selector: 'astro-numeric-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="numeric">
      <input
        type="text"
        inputmode="decimal"
        class="form-control numeric__field astro-numeric"
        autocomplete="off"
        [ngModel]="value()"
        (ngModelChange)="set($event)"
        [attr.aria-label]="t('::Take:YourAnswer')" />

      @if (unit()) {
        <span class="numeric__unit">{{ unit() }}</span>
      }
    </div>
  `,
  styles: `
    :host { display: block; }

    .numeric {
      display: flex;
      align-items: center;
      gap: var(--astro-space-2);
    }

    .numeric__field {
      max-inline-size: 16rem;
      font-size: 1.125rem;

      /* Digits read left to right whatever the page does around them. */
      direction: ltr;
      text-align: start;
    }

    .numeric__unit { color: var(--text-secondary); }
  `,
})
export class NumericAnswerComponent {
  readonly t = inject(TranslateService).t;

  readonly question = input.required<TakerQuestion>();
  readonly response = input<string | undefined>();
  readonly responseChange = output<string>();

  readonly value = signal('');

  private seeded = false;

  constructor() {
    effect(() => {
      const saved = this.response();

      if (this.seeded) {
        return;
      }

      this.seeded = true;
      this.value.set(saved ? saved.replace(/^"|"$/g, '') : '');
    });
  }

  unit(): string {
    return (this.question().display['unit'] as string | undefined) ?? '';
  }

  set(value: string): void {
    this.value.set(value);
    this.responseChange.emit(value);
  }
}
