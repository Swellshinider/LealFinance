import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

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
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  /** Protected backend status message. */
  public message = 'Loading dashboard data...';

  /** Generated timestamp from backend. */
  public generatedAtUtc = '';

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
      .subscribe({
        next: (response: DashboardSummaryResponse) => {
          this.message = response.message;
          this.generatedAtUtc = response.generatedAtUtc;
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.message = 'Your session has expired. Please log in again.';
            void this.router.navigate(['/login']);
            return;
          }

          if (error.status === 0) {
            this.message = 'Cannot reach the server right now.';
            return;
          }

          this.message = 'Unable to load dashboard data.';
        }
      });
  }
}
