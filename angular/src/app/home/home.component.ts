import { AuthService } from '@abp/ng.core';
import { Component, inject } from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/prefer-standalone
  standalone: false,
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent {
  private authService = inject(AuthService);

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  login() {
    this.authService.navigateToLogin();
  }
}
