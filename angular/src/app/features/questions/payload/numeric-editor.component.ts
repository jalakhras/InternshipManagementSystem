import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { NumericPayload, readPayload, writePayload } from './payload.models';

/**
 * A number accepted within a tolerance.
 *
 * The tolerance is the whole point of the type: position sizing, engineering and
 * chemistry all have right answers that are quantities, and demanding an exact
 * decimal match marks a correct answer wrong over a rounding step.
 */
@Component({
  selector: 'astro-numeric-editor',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="row">
      <div class="field">
        <label class="form-label" for="value">{{ t('::Question:CorrectValue') }}</label>
        <input
          id="value"
          type="number"
          step="any"
          class="form-control astro-numeric"
          [ngModel]="value()"
          (ngModelChange)="setValue(+$event)" />
      </div>

      <div class="field">
        <label class="form-label" for="tolerance">{{ t('::Question:Tolerance') }}</label>
        <input
          id="tolerance"
          type="number"
          step="any"
          min="0"
          class="form-control astro-numeric"
          [ngModel]="tolerance()"
          (ngModelChange)="setTolerance(+$event)" />
        <p class="hint">{{ t('::Question:Tolerance:Hint') }}</p>
      </div>

      <div class="field">
        <label class="form-label" for="unit">{{ t('::Question:Unit') }}</label>
        <input
          id="unit"
          type="text"
          class="form-control"
          [ngModel]="unit()"
          (ngModelChange)="setUnit($event)" />
        <p class="hint">{{ t('::Question:Unit:Hint') }}</p>
      </div>
    </div>

    <p class="preview">
      {{ t('::Question:Accepts') }}
      <span class="astro-numeric">{{ lowerBound() }} — {{ upperBound() }}</span>
      {{ unit() }}
    </p>

    @if (tolerance() < 0) {
      <p class="warning" role="status">
        <i class="bi bi-exclamation-triangle" aria-hidden="true"></i>
        {{ t('::IMS:Question:NegativeTolerance') }}
      </p>
    }
  `,
  styles: `
    :host { display: block; }
    .row { display: grid; gap: var(--astro-space-3); grid-template-columns: repeat(auto-fit, minmax(9rem, 1fr)); }
    .form-label { display: block; margin-block-end: var(--astro-space-1); font-size: var(--astro-text-sm); font-weight: var(--astro-weight-medium); }
    .form-control { background: var(--surface-page); border-color: var(--border-subtle); color: var(--text-primary); min-block-size: var(--astro-touch-min); }
    .form-control:focus { background: var(--surface-page); color: var(--text-primary); border-color: var(--accent); box-shadow: none; }
    .hint { margin: var(--astro-space-1) 0 0; font-size: var(--astro-text-xs); color: var(--text-muted); }

    /* Shows the accepted range as a sentence, so the author sees what they built
       rather than computing it from two fields. */
    .preview {
      margin: var(--astro-space-3) 0 0;
      padding: var(--astro-space-2) var(--astro-space-3);
      border-radius: var(--astro-radius-md);
      background: var(--accent-subtle);
      color: var(--accent-subtle-text);
      font-size: var(--astro-text-sm);
    }

    .warning {
      display: flex; align-items: flex-start; gap: var(--astro-space-2);
      margin: var(--astro-space-2) 0 0; padding: var(--astro-space-2) var(--astro-space-3);
      border-radius: var(--astro-radius-md);
      background: var(--status-fail-bg); color: var(--status-fail-text);
      font-size: var(--astro-text-sm);
    }
  `,
})
export class NumericEditorComponent {
  readonly t = inject(TranslateService).t;

  readonly payload = input<string>('');
  readonly payloadChange = output<string>();

  readonly value = signal(0);
  readonly tolerance = signal(0);
  readonly unit = signal('');

  constructor() {
    effect(() => {
      const parsed = readPayload<NumericPayload>(this.payload(), {
        correctValue: 0,
        tolerance: 0,
      });

      this.value.set(parsed.correctValue ?? 0);
      this.tolerance.set(parsed.tolerance ?? 0);
      this.unit.set(parsed.unit ?? '');
    });
  }

  lowerBound(): number {
    return Number((this.value() - Math.abs(this.tolerance())).toFixed(4));
  }

  upperBound(): number {
    return Number((this.value() + Math.abs(this.tolerance())).toFixed(4));
  }

  setValue(v: number): void { this.value.set(v); this.emit(); }
  setTolerance(v: number): void { this.tolerance.set(v); this.emit(); }
  setUnit(v: string): void { this.unit.set(v); this.emit(); }

  private emit(): void {
    this.payloadChange.emit(
      writePayload({
        correctValue: this.value(),
        tolerance: this.tolerance(),
        unit: this.unit() || undefined,
      }),
    );
  }
}
