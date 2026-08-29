import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DirectionService } from './core/direction.service';

/**
 * Root component.
 *
 * Deliberately thin: the shell lives on a route so the exam screens can opt out
 * of it entirely. DirectionService is injected here so `dir` and `lang` are on
 * the document before the first route renders — set any later and the first
 * paint is mirrored the wrong way.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class AppComponent {
  private readonly direction = inject(DirectionService);
}
