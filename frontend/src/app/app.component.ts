import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { AuthService } from './core/services/auth.service';

/**
 * Root application shell with navigation and routed content.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink],
  templateUrl: './app.component.html',
  styleUrl: './app.scss'
})
export class AppComponent {
  public constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  /**
   * Returns whether a user is authenticated.
   */
  public isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  /**
   * Clears local session and redirects to login.
   */
  public logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}