import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';

/**
 * Public landing page that introduces LealFinance and guides users to key actions.
 */
@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatCardModule],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {
  public constructor(private readonly authService: AuthService) {}

  /**
   * Returns whether the current user has an active authenticated session.
   */
  public isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }
}
