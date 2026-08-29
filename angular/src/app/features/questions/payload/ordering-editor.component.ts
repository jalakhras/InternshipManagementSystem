import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { OrderingItem, OrderingPayload, newId, readPayload, writePayload } from './payload.models';

/**
 * Steps to be put in order.
 *
 * The author types the steps in the right order and that is the answer. No
 * position numbers to fill in, no "correct sequence" field to keep in step with
 * the list — the list <em>is</em> the sequence, and the arrows move a row within
 * it. Delivery shuffles them before a candidate ever sees them.
 *
 * Asking an author to type both a list and its ordering is asking them to keep
 * two things consistent by hand, which they will eventually fail to do, and the
 * failure is a question marked against a sequence nobody intended.
 */
@Component({
  selector: 'astro-ordering-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <p class="lede">{{ t('::Question:Ordering:Lede') }}</p>

    <div class="items">
      @for (item of items(); track item.id; let i = $index) {
        <div class="item">
          <span class="item__position astro-numeric">{{ i + 1 }}</span>

          <input
            type="text"
            class="form-control"
            [ngModel]="item.text"
            (ngModelChange)="setText(item.id, $event)"
            [placeholder]="t('::Question:Ordering:Step') + ' ' + (i + 1)"
            [attr.aria-label]="t('::Question:Ordering:Step') + ' ' + (i + 1)" />

          <button
            type="button"
            class="item__move"
            [disabled]="i === 0"
            [attr.aria-label]="t('::Question:Ordering:MoveUp')"
            (click)="move(i, -1)">
            <i class="bi bi-arrow-up" aria-hidden="true"></i>
          </button>

          <button
            type="button"
            class="item__move"
            [disabled]="i === items().length - 1"
            [attr.aria-label]="t('::Question:Ordering:MoveDown')"
            (click)="move(i, 1)">
            <i class="bi bi-arrow-down" aria-hidden="true"></i>
          </button>

          <button
            type="button"
            class="item__move item__move--danger"
            [disabled]="items().length <= 2"
            [attr.aria-label]="t('::Question:Ordering:RemoveStep')"
            (click)="removeItem(item.id)">
            <i class="bi bi-x-lg" aria-hidden="true"></i>
          </button>
        </div>
      }
    </div>

    <div class="actions">
      <button type="button" class="btn btn-sm btn-outline-secondary" (click)="addItem()">
        <i class="bi bi-plus-lg" aria-hidden="true"></i>
        {{ t('::Question:Ordering:AddStep') }}
      </button>

      <label class="partial">
        <input type="checkbox" [checked]="allowPartialCredit()" (change)="togglePartial()" />
        <span>
          <strong>{{ t('::Question:PartialCredit') }}</strong>
          <small>{{ t('::Question:Ordering:PartialCredit:Hint') }}</small>
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

    .items { display: grid; gap: var(--astro-space-2); }

    .item {
      display: grid;
      grid-template-columns: 2rem 1fr repeat(3, 2rem);
      align-items: center;
      gap: var(--astro-space-2);
    }

    .item__position {
      display: grid;
      place-items: center;
      inline-size: 2rem;
      block-size: 2rem;
      border-radius: 50%;
      background: var(--astro-surface-2);
      color: var(--astro-ink-2);
      font-size: .8125rem;
      font-weight: 600;
    }

    .item__move {
      display: grid;
      place-items: center;
      inline-size: 2rem;
      block-size: 2rem;
      border: 0;
      border-radius: var(--astro-radius-sm);
      background: transparent;
      color: var(--astro-ink-3);
      cursor: pointer;

      &:hover:not(:disabled) { color: var(--astro-ink-1); }
      &--danger:hover:not(:disabled) { color: var(--astro-fail-fg); }
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
export class OrderingEditorComponent {
  readonly t = inject(TranslateService).t;

  readonly payload = input<string>('');
  readonly payloadChange = output<string>();

  readonly items = signal<OrderingItem[]>([]);
  readonly allowPartialCredit = signal(true);

  readonly warnings = computed<string[]>(() => {
    const items = this.items();
    const found: string[] = [];

    if (items.length < 2) {
      found.push('IMS:Question:NeedsTwoItems');
    }

    if (items.some(i => !i.text.trim())) {
      found.push('IMS:Question:Ordering:EmptyStep');
    }

    return found;
  });

  constructor() {
    effect(() => {
      const parsed = readPayload<OrderingPayload>(this.payload(), { items: [], allowPartialCredit: true });

      this.items.set(
        parsed.items.length > 0
          ? // Sorted into the stored sequence, so the list shown is the answer.
            [...parsed.items].sort((a, b) => a.correctPosition - b.correctPosition)
          : [this.blankItem(), this.blankItem()],
      );

      this.allowPartialCredit.set(parsed.allowPartialCredit ?? true);
    });
  }

  addItem(): void {
    this.items.update(list => [...list, this.blankItem()]);
    this.emit();
  }

  removeItem(id: string): void {
    this.items.update(list => list.filter(i => i.id !== id));
    this.emit();
  }

  setText(id: string, text: string): void {
    this.items.update(list => list.map(i => (i.id === id ? { ...i, text } : i)));
    this.emit();
  }

  move(index: number, direction: -1 | 1): void {
    this.items.update(list => {
      const next = [...list];
      const target = index + direction;

      [next[index], next[target]] = [next[target], next[index]];

      return next;
    });

    this.emit();
  }

  togglePartial(): void {
    this.allowPartialCredit.update(v => !v);
    this.emit();
  }

  private blankItem(): OrderingItem {
    return { id: newId('i'), text: '', correctPosition: 0 };
  }

  /**
   * The list's own order becomes the stored sequence, numbered from one.
   * <p>
   * Written on every change rather than kept in step by the author. The server
   * refuses a sequence with gaps in it, and a hand-maintained position field is
   * exactly how gaps appear.
   * </p>
   */
  private emit(): void {
    const items = this.items().map((item, index) => ({ ...item, correctPosition: index + 1 }));

    this.payloadChange.emit(writePayload({ items, allowPartialCredit: this.allowPartialCredit() }));
  }
}
