import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';

import { TranslateService } from '../../core/translate.service';
import { TakeService } from './take.service';
import { AttemptResult } from './take.models';
import { takerFailure } from './taker-failure';

/**
 * What a candidate is told afterwards.
 *
 * Two outcomes, and the second is the one usually got wrong. When a person still
 * has answers to mark, there is no score yet — so this screen says that, rather
 * than showing a provisional number that will change. A candidate who reads 45%
 * and later receives 68% has been told something untrue, and no explanation
 * afterwards undoes it.
 *
 * When there is a score, it comes with a breakdown by competency. One percentage
 * tells nobody what to do next; four tell a student which class to join and a
 * candidate what to go and learn.
 */
@Component({
  selector: 'astro-take-result',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  templateUrl: './take-result.component.html',
  styleUrl: './take-result.component.scss',
})
export class TakeResultComponent {
  private readonly take = inject(TakeService);

  readonly t = inject(TranslateService).t;

  readonly token = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly result = signal<AttemptResult | null>(null);

  constructor() {
    this.take.getResult().subscribe({
      next: result => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: err => {
        const problem = err as { error?: { error?: { message?: string } }; message?: string };

        this.error.set(takerFailure(err, this.t));
        this.loading.set(false);
      },
    });
  }
}
