import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { ScalePayload, readPayload, writePayload } from './payload.models';

/**
 * A rating scale — how strongly, from one end to the other.
 *
 * There is no right answer here, which is the whole point of the type: it
 * collects an opinion, and a person reads it afterwards. Self-assessment before
 * a course, confidence beside a technical answer, a satisfaction question at the
 * end of a sitting.
 *
 * Two numbers and two labels. The preview underneath is the entire design
 * argument — an author sets 1 to 5 and immediately sees the five buttons a
 * candidate will see, so nobody has to imagine it or save to find out.
 */
@Component({
  selector: 'astro-scale-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <p class="lede">{{ t('::Question:Scale:Lede') }}</p>

    <div class="row-4">
      <div class="field">
        <label class="form-label" for="scaleMin">{{ t('::Question:Scale:From') }}</label>
        <input
          id="scaleMin"
          type="number"
          class="form-control astro-numeric"
          [ngModel]="min()"
          (ngModelChange)="setMin($event)" />
      </div>

      <div class="field">
        <label class="form-label" for="scaleMinLabel">{{ t('::Question:Scale:FromLabel') }}</label>
        <input
          id="scaleMinLabel"
          type="text"
          class="form-control"
          [ngModel]="minLabel()"
          (ngModelChange)="setMinLabel($event)"
          [placeholder]="t('::Question:Scale:FromLabel:Placeholder')" />
      </div>

      <div class="field">
        <label class="form-label" for="scaleMax">{{ t('::Question:Scale:To') }}</label>
        <input
          id="scaleMax"
          type="number"
          class="form-control astro-numeric"
          [ngModel]="max()"
          (ngModelChange)="setMax($event)" />
      </div>

      <div class="field">
        <label class="form-label" for="scaleMaxLabel">{{ t('::Question:Scale:ToLabel') }}</label>
        <input
          id="scaleMaxLabel"
          type="text"
          class="form-control"
          [ngModel]="maxLabel()"
          (ngModelChange)="setMaxLabel($event)"
          [placeholder]="t('::Question:Scale:ToLabel:Placeholder')" />
      </div>
    </div>

    <!-- What the candidate will see, built from the numbers above. An author
         should never have to save a question to find out what it looks like. -->
    @if (points().length > 0) {
      <div class="preview">
        <span class="preview__label">{{ minLabel() || min() }}</span>

        <div class="preview__points">
          @for (point of points(); track point) {
            <span class="preview__point astro-numeric">{{ point }}</span>
          }
        </div>

        <span class="preview__label">{{ maxLabel() || max() }}</span>
      </div>
    }

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

    .row-4 {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
      gap: var(--astro-space-3);
    }

    .preview {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--astro-space-3);
      margin-block-start: var(--astro-space-4);
      padding: var(--astro-space-3);
      border: 1px solid var(--border-subtle);
      border-radius: var(--astro-radius-md);
      background: var(--surface-sunken);
    }

    .preview__label { font-size: .875rem; color: var(--text-muted); }
    .preview__points { display: flex; gap: var(--astro-space-1); }

    .preview__point {
      display: grid;
      place-items: center;
      inline-size: 2.25rem;
      block-size: 2.25rem;
      border: 1px solid var(--border-subtle);
      border-radius: 50%;
      background: var(--surface-raised);
      color: var(--text-secondary);
      font-size: .875rem;
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
export class ScaleEditorComponent {
  readonly t = inject(TranslateService).t;

  readonly payload = input<string>('');
  readonly payloadChange = output<string>();

  readonly min = signal(1);
  readonly max = signal(5);
  readonly minLabel = signal('');
  readonly maxLabel = signal('');

  /**
   * The points a candidate would see. Capped, because a scale of one to five
   * hundred is a typo rather than a question, and rendering it would hang the
   * page while an author works out what they did.
   */
  readonly points = computed<number[]>(() => {
    const from = this.min();
    const to = this.max();

    if (to <= from || to - from > 20) {
      return [];
    }

    return Array.from({ length: to - from + 1 }, (_, i) => from + i);
  });

  readonly warnings = computed<string[]>(() => {
    const found: string[] = [];

    if (this.max() <= this.min()) {
      found.push('IMS:Question:ScaleRangeInvalid');
    }

    if (this.max() - this.min() > 20) {
      found.push('IMS:Question:Scale:TooManyPoints');
    }

    // Bare numbers make a taker guess which end is good. It is not an error —
    // some scales are self-evident — but it is usually an omission.
    if (!this.minLabel().trim() && !this.maxLabel().trim()) {
      found.push('IMS:Question:Scale:NoLabels');
    }

    return found;
  });

  constructor() {
    effect(() => {
      const parsed = readPayload<ScalePayload>(this.payload(), { min: 1, max: 5 });

      this.min.set(parsed.min ?? 1);
      this.max.set(parsed.max ?? 5);
      this.minLabel.set(parsed.minLabel ?? '');
      this.maxLabel.set(parsed.maxLabel ?? '');
    });
  }

  setMin(value: number | string): void {
    this.min.set(Math.round(Number(value) || 0));
    this.emit();
  }

  setMax(value: number | string): void {
    this.max.set(Math.round(Number(value) || 0));
    this.emit();
  }

  setMinLabel(value: string): void {
    this.minLabel.set(value);
    this.emit();
  }

  setMaxLabel(value: string): void {
    this.maxLabel.set(value);
    this.emit();
  }

  private emit(): void {
    this.payloadChange.emit(
      writePayload({
        min: this.min(),
        max: this.max(),
        minLabel: this.minLabel() || undefined,
        maxLabel: this.maxLabel() || undefined,
      }),
    );
  }
}
