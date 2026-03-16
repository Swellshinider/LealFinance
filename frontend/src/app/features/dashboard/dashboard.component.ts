import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { finalize, timeout } from 'rxjs';

interface DashboardSummaryResponse {
  message: string;
  generatedAtUtc: string;
}

/**
 * Displays protected dashboard data.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [MatCardModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly requestTimeoutMs = 15000;

  /** Protected backend status message. */
  public message = 'Loading dashboard data...';

  /** Generated timestamp from backend. */
  public generatedAtUtc = '';

  /** Indicates summary request is still loading. */
  public isLoading = true;

  public constructor(
    private readonly httpClient: HttpClient,
    private readonly router: Router
  ) {}

  /**
   * Loads dashboard summary data from the protected API.
   */
  public ngOnInit(): void {
    this.httpClient
      .get<DashboardSummaryResponse>('http://localhost:5216/api/dashboard/summary')
      .pipe(
        timeout(this.requestTimeoutMs),
        finalize(() => {
          this.runInAngular(() => {
            this.isLoading = false;
          });
        })
      )
      .subscribe({
        next: (response: DashboardSummaryResponse) => {
          this.runInAngular(() => {
            this.message = response.message;
            this.generatedAtUtc = response.generatedAtUtc;
          });
        },
        error: (error: unknown) => {
          this.runInAngular(() => {
            if (error instanceof HttpErrorResponse && error.status === 401) {
              this.message = 'Your session has expired. Please log in again.';
              void this.router.navigate(['/login']);
              return;
            }

            if (error instanceof HttpErrorResponse && error.status === 0) {
              this.message = 'Cannot reach the server right now.';
              return;
            }

            this.message = 'Unable to load dashboard data.';
          });
        }
      });
  }

  private runInAngular(action: () => void): void {
    this.ngZone.run(() => {
      action();
      this.changeDetectorRef.detectChanges();
    });
  }
}
