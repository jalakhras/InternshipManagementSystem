import { Component, computed, input } from '@angular/core';

export type StatusTone = 'pass' | 'pending' | 'fail' | 'neutral' | 'accent';

/**
 * A small labelled state marker.
 *
 * Every chip carries an icon as well as a colour. Colour alone excludes anyone
 * who cannot distinguish these hues — and in a product where the difference
 * between two of them is "passed" and "failed", that is not a detail.
 */
@Component({
  selector: 'astro-status-chip',
  standalone: true,
  template: `
    <span class="chip chip--{{ tone() }}">
      <i class="bi {{ icon() }}" aria-hidden="true"></i>
      <span>{{ label() }}</span>
    </span>
  `,
  styles: `
    .chip {
      display: inline-flex;
      align-items: center;
      gap: var(--astro-space-1);
      padding-inline: var(--astro-space-2);
      padding-block: 2px;
      border-radius: var(--astro-radius-full);
      font-size: var(--astro-text-xs);
      font-weight: var(--astro-weight-medium);
      white-space: nowrap;

      i { font-size: 0.85em; }
    }

    .chip--pass    { background: var(--status-pass-bg);    color: var(--status-pass-text); }
    .chip--pending { background: var(--status-pending-bg); color: var(--status-pending-text); }
    .chip--fail    { background: var(--status-fail-bg);    color: var(--status-fail-text); }
    .chip--neutral { background: var(--surface-sunken);    color: var(--text-secondary); }
    .chip--accent  { background: var(--accent-subtle);     color: var(--accent-subtle-text); }
  `,
})
export class StatusChipComponent {
  readonly label = input.required<string>();
  readonly tone = input<StatusTone>('neutral');

  /** Overrides the tone's default icon where a screen needs something specific. */
  readonly iconOverride = input<string>();

  readonly icon = computed(() => this.iconOverride() ?? DEFAULT_ICONS[this.tone()]);
}

const DEFAULT_ICONS: Record<StatusTone, string> = {
  pass: 'bi-check-circle-fill',
  pending: 'bi-hourglass-split',
  fail: 'bi-x-circle-fill',
  neutral: 'bi-circle',
  accent: 'bi-dot',
};
