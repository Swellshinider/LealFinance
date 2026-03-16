import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../services/auth.service';

/**
 * Application header with authentication-aware navigation actions.
 */
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  public constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  /**
   * Returns whether a valid user session exists.
   */
  public isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  /**
   * Ends the current session and redirects to login.
   */
  public logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
