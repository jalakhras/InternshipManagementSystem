import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { TranslateService } from '../../../core/translate.service';
import { TakerQuestion } from '../take.models';

/**
 * Putting steps back in order.
 *
 * Arrows, not drag and drop. Dragging is unusable with a keyboard, awkward with
 * a screen reader, and on a phone it fights the page scroll — which under a
 * clock means a candidate loses their place while trying to move an item. Two
 * buttons per row work everywhere and need no explanation.
 *
 * The items arrive shuffled by the server for this attempt. The order shown is
 * the answer, so there is nothing else to fill in.
 */
@Component({
  selector: 'astro-ordering-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <ol class="items">
      @for (item of items(); track item.id; let i = $index) {
        <li class="item">
          <span class="item__position astro-numeric">{{ i + 1 }}</span>
          <span class="item__text">{{ item.text }}</span>

          <button
            type="button"
            class="item__move"
            [disabled]="i === 0"
            [attr.aria-label]="t('::Take:MoveUp') + ': ' + item.text"
            (click)="move(i, -1)">
            <i class="bi bi-arrow-up" aria-hidden="true"></i>
          </button>

          <button
            type="button"
            class="item__move"
            [disabled]="i === items().length - 1"
            [attr.aria-label]="t('::Take:MoveDown') + ': ' + item.text"
            (click)="move(i, 1)">
            <i class="bi bi-arrow-down" aria-hidden="true"></i>
          </button>
        </li>
      }
    </ol>
  `,
  styles: `
    :host { display: block; }

    .items {
      display: grid;
      gap: var(--astro-space-2);
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .item {
      display: grid;
      grid-template-columns: 2.5rem 1fr 3rem 3rem;
      align-items: center;
      gap: var(--astro-space-2);
      min-block-size: 3.25rem;
      padding: var(--astro-space-2) var(--astro-space-3);
      border: 1px solid var(--border-subtle);
      border-radius: var(--astro-radius-md);
      background: var(--surface-raised);
    }

    .item__position {
      display: grid;
      place-items: center;
      inline-size: 2rem;
      block-size: 2rem;
      border-radius: 50%;
      background: var(--astro-brand-50);
      color: var(--astro-brand-700);
      font-weight: 600;
    }

    .item__text { line-height: var(--astro-leading-body); }

    .item__move {
      display: grid;
      place-items: center;
      inline-size: 2.75rem;
      block-size: 2.75rem;
      border: 1px solid var(--border-subtle);
      border-radius: var(--astro-radius-sm);
      background: var(--surface-raised);
      color: var(--text-secondary);
      cursor: pointer;

      &:hover:not(:disabled) { border-color: var(--astro-brand-600); color: var(--astro-brand-700); }
      &:disabled { opacity: .35; cursor: default; }
    }
  `,
})
export class OrderingAnswerComponent {
  readonly t = inject(TranslateService).t;

  readonly question = input.required<TakerQuestion>();
  readonly response = input<string | undefined>();
  readonly responseChange = output<string>();

  readonly items = signal<{ id: string; text: string }[]>([]);

  private seeded = false;

  constructor() {
    effect(() => {
      const question = this.question();
      const saved = this.response();

      if (this.seeded) {
        return;
      }

      this.seeded = true;

      const shown = (question.display['items'] as { id: string; text: string }[] | undefined) ?? [];
      const stored = this.parse(saved);

      // A resumed attempt comes back to the order the candidate left, not to the
      // shuffle they were first given.
      this.items.set(
        stored.length === shown.length
          ? stored.map(id => shown.find(item => item.id === id)).filter((x): x is { id: string; text: string } => !!x)
          : shown,
      );
    });
  }

  move(index: number, direction: -1 | 1): void {
    this.items.update(list => {
      const next = [...list];
      const target = index + direction;

      [next[index], next[target]] = [next[target], next[index]];

      return next;
    });

    this.responseChange.emit(JSON.stringify(this.items().map(item => item.id)));
  }

  private parse(saved: string | undefined): string[] {
    if (!saved) {
      return [];
    }

    try {
      const parsed: unknown = JSON.parse(saved);

      return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : [];
    } catch {
      return [];
    }
  }
}
