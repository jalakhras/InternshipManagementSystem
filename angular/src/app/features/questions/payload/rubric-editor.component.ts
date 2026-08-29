import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { RubricCriterion, RubricPayload, newId, readPayload, writePayload } from './payload.models';

/**
 * A marking rubric, for the types a person has to mark: written answers, uploaded
 * files and spoken recordings.
 *
 * The rubric is what makes those types defensible. Two reviewers scoring the same
 * answer out of ten will disagree; against named criteria they mostly will not,
 * and a candidate who disputes a mark can be shown which criterion cost them.
 *
 * It is optional — a question with no criteria is still markable — but the total
 * is shown against the question's marks so a mismatch is visible while it is
 * cheap to fix.
 */
@Component({
  selector: 'astro-rubric-editor',
  standalone: true,
  imports: [FormsModule],
  template: `
    <p class="lede">{{ t('::Question:Rubric:Lede') }}</p>

    @if (criteria().length === 0) {
      <p class="empty">{{ t('::Question:Rubric:None') }}</p>
    } @else {
      <div class="criteria">
        @for (criterion of criteria(); track criterion.id) {
          <div class="criterion">
            <input
              type="text"
              class="form-control criterion__name"
              [ngModel]="criterion.name"
              (ngModelChange)="setName(criterion.id, $event)"
              [placeholder]="t('::Question:Rubric:CriterionName')"
              [attr.aria-label]="t('::Question:Rubric:CriterionName')" />

            <input
              type="number"
              min="0"
              step="0.5"
              class="form-control criterion__score astro-numeric"
              [ngModel]="criterion.maxScore"
              (ngModelChange)="setScore(criterion.id, +$event)"
              [attr.aria-label]="t('::Question:Rubric:CriterionScore')" />

            <button
              type="button"
              class="criterion__remove"
              [attr.aria-label]="t('::Question:Rubric:RemoveCriterion')"
              (click)="remove(criterion.id)">
              <i class="bi bi-x-lg" aria-hidden="true"></i>
            </button>
          </div>
        }
      </div>

      <p class="total" [class.total--mismatch]="mismatched()">
        {{ t('::Question:Rubric:Total') }}
        <span class="astro-numeric">{{ total() }}</span>
        @if (mismatched()) {
          <span class="total__note">{{ t('::Question:Rubric:Mismatch') }}</span>
        }
      </p>
    }

    <button type="button" class="btn btn-sm btn-outline-secondary" (click)="add()">
      <i class="bi bi-plus-lg" aria-hidden="true"></i>
      {{ t('::Question:Rubric:AddCriterion') }}
    </button>

    <div class="field">
      <label class="form-label" for="guidance">{{ t('::Question:Rubric:Guidance') }}</label>
      <textarea
        id="guidance"
        class="form-control"
        rows="2"
        [ngModel]="guidance()"
        (ngModelChange)="setGuidance($event)"></textarea>
      <!-- Written for the marker. The candidate never sees it, which is what makes
           it usable for saying things like "award full marks only if they mention risk". -->
      <p class="hint">{{ t('::Question:Rubric:Guidance:Hint') }}</p>
    </div>
  `,
  styleUrl: './rubric-editor.component.scss',
})
export class RubricEditorComponent {
  readonly t = inject(TranslateService).t;

  readonly payload = input<string>('');

  /** The question's marks, so a rubric that does not add up is visible here. */
  readonly questionScore = input<number>(0);

  readonly payloadChange = output<string>();

  readonly criteria = signal<RubricCriterion[]>([]);
  readonly guidance = signal('');

  readonly total = computed(() =>
    Number(this.criteria().reduce((sum, c) => sum + (c.maxScore || 0), 0).toFixed(2)),
  );

  /**
   * True when the criteria do not add up to the question's marks.
   *
   * Not an error — a reviewer can still award within the question's total — but it
   * usually means a criterion was added and the marks not adjusted, and that is
   * far cheaper to notice now than in the review queue.
   */
  readonly mismatched = computed(
    () => this.criteria().length > 0 && this.questionScore() > 0 && this.total() !== this.questionScore(),
  );

  constructor() {
    effect(() => {
      const parsed = readPayload<RubricPayload>(this.payload(), { criteria: [] });
      this.criteria.set(parsed.criteria ?? []);
      this.guidance.set(parsed.reviewerGuidance ?? '');
    });
  }

  add(): void {
    this.criteria.update(list => [...list, { id: newId('c'), name: '', maxScore: 1 }]);
    this.emit();
  }

  remove(id: string): void {
    this.criteria.update(list => list.filter(c => c.id !== id));
    this.emit();
  }

  setName(id: string, name: string): void {
    this.criteria.update(list => list.map(c => (c.id === id ? { ...c, name } : c)));
    this.emit();
  }

  setScore(id: string, maxScore: number): void {
    this.criteria.update(list => list.map(c => (c.id === id ? { ...c, maxScore } : c)));
    this.emit();
  }

  setGuidance(value: string): void {
    this.guidance.set(value);
    this.emit();
  }

  private emit(): void {
    this.payloadChange.emit(
      writePayload({
        criteria: this.criteria(),
        reviewerGuidance: this.guidance() || undefined,
      }),
    );
  }
}
