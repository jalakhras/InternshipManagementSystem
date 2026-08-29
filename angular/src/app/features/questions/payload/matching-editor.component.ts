import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { MatchingPair, MatchingPayload, newId, readPayload, writePayload } from './payload.models';

/**
 * Pairs to be matched: a term on one side, its definition on the other.
 *
 * Two columns of plain text boxes and nothing else. The pairing is expressed by
 * being on the same row — an author types "Support" beside "A level price has
 * repeatedly failed to fall below", and that is the whole interaction. There is
 * no id to invent, no arrow to draw, and nothing to learn.
 *
 * The ids exist and matter — the grader reads them and the delivery shuffles by
 * them — but they are generated here and never shown. An author writing a
 * vocabulary exercise should not meet the word "id".
 */
@Component({
  selector: 'astro-matching-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <p class="lede">{{ t('::Question:Matching:Lede') }}</p>

    <div class="pairs">
      <div class="pairs__head">
        <span>{{ t('::Question:Matching:Left') }}</span>
        <span></span>
        <span>{{ t('::Question:Matching:Right') }}</span>
        <span></span>
      </div>

      @for (pair of pairs(); track pair.leftId; let i = $index) {
        <div class="pair">
          <input
            type="text"
            class="form-control"
            [ngModel]="pair.leftText"
            (ngModelChange)="setLeft(pair.leftId, $event)"
            [placeholder]="t('::Question:Matching:Left') + ' ' + (i + 1)"
            [attr.aria-label]="t('::Question:Matching:Left') + ' ' + (i + 1)" />

          <i class="bi bi-arrow-left-right pair__link astro-flip" aria-hidden="true"></i>

          <input
            type="text"
            class="form-control"
            [ngModel]="pair.rightText"
            (ngModelChange)="setRight(pair.leftId, $event)"
            [placeholder]="t('::Question:Matching:Right') + ' ' + (i + 1)"
            [attr.aria-label]="t('::Question:Matching:Right') + ' ' + (i + 1)" />

          <button
            type="button"
            class="pair__remove"
            [disabled]="pairs().length <= 2"
            [attr.aria-label]="t('::Question:Matching:RemovePair')"
            (click)="removePair(pair.leftId)">
            <i class="bi bi-x-lg" aria-hidden="true"></i>
          </button>
        </div>
      }
    </div>

    <div class="actions">
      <button type="button" class="btn btn-sm btn-outline-secondary" (click)="addPair()">
        <i class="bi bi-plus-lg" aria-hidden="true"></i>
        {{ t('::Question:Matching:AddPair') }}
      </button>

      <label class="partial">
        <input type="checkbox" [checked]="allowPartialCredit()" (change)="togglePartial()" />
        <span>
          <strong>{{ t('::Question:PartialCredit') }}</strong>
          <small>{{ t('::Question:Matching:PartialCredit:Hint') }}</small>
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

    .lede { margin-block: 0 var(--astro-space-3); color: var(--astro-ink-3); font-size: .875rem; }

    .pairs { display: grid; gap: var(--astro-space-2); }

    .pairs__head,
    .pair {
      display: grid;
      grid-template-columns: 1fr auto 1fr 2rem;
      align-items: center;
      gap: var(--astro-space-2);
    }

    .pairs__head {
      font-size: .8125rem;
      font-weight: 600;
      color: var(--astro-ink-3);
    }

    .pair__link { color: var(--astro-ink-3); }

    .pair__remove {
      display: grid;
      place-items: center;
      inline-size: 2rem;
      block-size: 2rem;
      border: 0;
      border-radius: var(--astro-radius-sm);
      background: transparent;
      color: var(--astro-ink-3);
      cursor: pointer;

      &:hover:not(:disabled) { color: var(--astro-fail-fg); }
      &:disabled { opacity: .3; cursor: default; }
    }

    .actions {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-start;
      gap: var(--astro-space-4);
      margin-block-start: var(--astro-space-3);
    }

    .partial {
      display: flex;
      gap: var(--astro-space-2);
      cursor: pointer;

      span { display: grid; }
      small { color: var(--astro-ink-3); }
    }

    .warning {
      display: flex;
      gap: var(--astro-space-2);
      margin-block-start: var(--astro-space-3);
      color: var(--astro-warn-fg);
      font-size: .875rem;
    }
  `,
})
export class MatchingEditorComponent {
  readonly t = inject(TranslateService).t;

  readonly payload = input<string>('');
  readonly payloadChange = output<string>();

  readonly pairs = signal<MatchingPair[]>([]);
  readonly allowPartialCredit = signal(true);

  readonly warnings = computed<string[]>(() => {
    const pairs = this.pairs();
    const found: string[] = [];

    if (pairs.length < 2) {
      found.push('IMS:Question:NeedsTwoPairs');
    }

    // A pair with one side blank cannot be matched to anything, and the taker is
    // left staring at an empty box wondering what they are missing.
    if (pairs.some(p => !p.leftText.trim() || !p.rightText.trim())) {
      found.push('IMS:Question:Matching:IncompletePair');
    }

    return found;
  });

  constructor() {
    effect(() => {
      const parsed = readPayload<MatchingPayload>(this.payload(), { pairs: [], allowPartialCredit: true });

      this.pairs.set(
        parsed.pairs.length > 0
          ? parsed.pairs
          : // Two empty rows, so an author starts by typing rather than by
            // pressing add. Two is the fewest a matching question can be.
            [this.blankPair(), this.blankPair()],
      );

      this.allowPartialCredit.set(parsed.allowPartialCredit ?? true);
    });
  }

  addPair(): void {
    this.pairs.update(list => [...list, this.blankPair()]);
    this.emit();
  }

  removePair(leftId: string): void {
    this.pairs.update(list => list.filter(p => p.leftId !== leftId));
    this.emit();
  }

  setLeft(leftId: string, leftText: string): void {
    this.pairs.update(list => list.map(p => (p.leftId === leftId ? { ...p, leftText } : p)));
    this.emit();
  }

  setRight(leftId: string, rightText: string): void {
    this.pairs.update(list => list.map(p => (p.leftId === leftId ? { ...p, rightText } : p)));
    this.emit();
  }

  togglePartial(): void {
    this.allowPartialCredit.update(v => !v);
    this.emit();
  }

  /** Ids are generated and never shown. The author's pairing is the row. */
  private blankPair(): MatchingPair {
    return { leftId: newId('l'), leftText: '', rightId: newId('r'), rightText: '' };
  }

  private emit(): void {
    this.payloadChange.emit(
      writePayload({ pairs: this.pairs(), allowPartialCredit: this.allowPartialCredit() }),
    );
  }
}
