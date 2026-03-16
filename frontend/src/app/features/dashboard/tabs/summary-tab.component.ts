import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { DashboardService, DashboardSummaryResponse, DashboardTransaction } from '../../../core/services/dashboard.service';

/**
 * Displays the dashboard summary tab with balance and recent transactions.
 */
@Component({
  selector: 'app-summary-tab',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatProgressSpinnerModule],
  templateUrl: './summary-tab.component.html',
  styleUrl: './summary-tab.component.scss'
})
export class SummaryTabComponent implements OnInit {
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  /** Indicates summary data loading state. */
  public isLoading = true;

  /** Error or status message for summary loading. */
  public message = '';

  /** Current balance value. */
  public totalCurrentBalance = 0;

  /** Latest five transactions. */
  public recentTransactions: DashboardTransaction[] = [];

  public constructor(
    private readonly dashboardService: DashboardService,
    private readonly router: Router
  ) {}

  /**
   * Loads summary data from the protected endpoint.
   */
  public ngOnInit(): void {
    this.dashboardService
      .getSummary()
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.isLoading = false;
          });
        })
      )
      .subscribe({
        next: (response: DashboardSummaryResponse) => {
          this.runInAngular(() => {
            this.totalCurrentBalance = response.totalCurrentBalance;
            this.recentTransactions = response.recentTransactions;
            this.message = '';
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

            this.message = 'Unable to load dashboard summary.';
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
