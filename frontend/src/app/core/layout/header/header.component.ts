import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService, UserProfile } from '../../services/auth.service';

/**
 * Application header with authentication-aware navigation actions.
 */
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule, MatMenuModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit {
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  /** Cached authenticated profile data. */
  public userProfile: UserProfile | null = null;

  /** Fallback avatar image used when profile photo is missing. */
  public readonly defaultAvatarUrl = '/default-avatar.svg';

  public constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  /**
   * Loads profile data when a session exists.
   */
  public ngOnInit(): void {
    this.authService.userProfile$.subscribe((profile: UserProfile | null) => {
      this.runInAngular(() => {
        this.userProfile = profile;
      });
    });

    if (this.isAuthenticated()) {
      this.authService.getProfile().subscribe({
        error: () => {
          this.runInAngular(() => {
            this.userProfile = null;
          });
        }
      });
    }
  }

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

  /**
   * Navigates to the profile page.
   */
  public goToProfile(): void {
    void this.router.navigate(['/profile']);
  }

  /**
   * Returns the current avatar image source.
   */
  public getProfileAvatarUrl(): string {
    const profilePhotoUrl = this.userProfile?.profilePhotoUrl?.trim();
    return profilePhotoUrl ? profilePhotoUrl : this.defaultAvatarUrl;
  }

  private runInAngular(action: () => void): void {
    this.ngZone.run(() => {
      action();
      this.changeDetectorRef.detectChanges();
    });
  }
}
