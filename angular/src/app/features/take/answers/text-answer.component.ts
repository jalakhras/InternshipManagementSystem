import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { TakerQuestion } from '../take.models';

/**
 * A written answer, and the fallback for anything this build has no input for.
 *
 * The fallback matters more than it looks. The server accepts question types
 * this client does not know and routes them to a person to mark, so a candidate
 * meeting one must still be able to answer it. Rendering nothing would strand
 * them on a question they can read and cannot respond to, with a clock running.
 */
@Component({
  selector: 'astro-text-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <textarea
      class="form-control answer"
      rows="8"
      [ngModel]="value()"
      (ngModelChange)="set($event)"
      [attr.aria-label]="t('::Take:YourAnswer')"></textarea>
  `,
  styles: `
    :host { display: block; }

    .answer {
      font-size: 1rem;
      line-height: var(--astro-leading-body);
      min-block-size: 12rem;
    }
  `,
})
export class TextAnswerComponent {
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
      this.value.set(saved ?? '');
    });
  }

  set(value: string): void {
    this.value.set(value);
    this.responseChange.emit(value);
  }
}
