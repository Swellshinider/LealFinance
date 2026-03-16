import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { ChartConfiguration, ChartOptions } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { finalize } from 'rxjs';

import { DashboardReportsResponse, DashboardService } from '../../../core/services/dashboard.service';

/**
 * Displays report visualizations for expenses and monthly cash flow.
 */
@Component({
  selector: 'app-reports-tab',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatProgressSpinnerModule, BaseChartDirective],
  templateUrl: './reports-tab.component.html',
  styleUrl: './reports-tab.component.scss'
})
export class ReportsTabComponent implements OnInit {
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  /** Indicates report loading state. */
  public isLoading = true;

  /** Error message for reports API failures. */
  public message = '';

  /** Pie chart data for expenses by category. */
  public pieChartData: ChartConfiguration<'pie'>['data'] = {
    labels: [],
    datasets: [{ data: [] }]
  };

  /** Pie chart options. */
  public readonly pieChartOptions: ChartOptions<'pie'> = {
    responsive: true,
    plugins: {
      legend: {
        position: 'bottom'
      }
    }
  };

  /** Bar chart data for monthly income versus expense. */
  public barChartData: ChartConfiguration<'bar'>['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Income',
        data: [],
        backgroundColor: '#16a34a'
      },
      {
        label: 'Expense',
        data: [],
        backgroundColor: '#dc2626'
      }
    ]
  };

  /** Bar chart options. */
  public readonly barChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    scales: {
      y: {
        beginAtZero: true
      }
    }
  };

  public constructor(
    private readonly dashboardService: DashboardService,
    private readonly router: Router
  ) {}

  /**
   * Loads reports datasets and renders charts.
   */
  public ngOnInit(): void {
    this.dashboardService
      .getReports()
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.isLoading = false;
          });
        })
      )
      .subscribe({
        next: (response: DashboardReportsResponse) => {
          this.runInAngular(() => {
            this.pieChartData = {
              labels: response.expensesByCategory.map((item) => item.category),
              datasets: [
                {
                  data: response.expensesByCategory.map((item) => item.totalAmount),
                  backgroundColor: ['#ef4444', '#f97316', '#f59e0b', '#8b5cf6', '#06b6d4', '#22c55e', '#ec4899']
                }
              ]
            };

            this.barChartData = {
              labels: response.monthlyIncomeVsExpense.map((item) => item.month),
              datasets: [
                {
                  label: 'Income',
                  data: response.monthlyIncomeVsExpense.map((item) => item.totalIncome),
                  backgroundColor: '#16a34a'
                },
                {
                  label: 'Expense',
                  data: response.monthlyIncomeVsExpense.map((item) => item.totalExpense),
                  backgroundColor: '#dc2626'
                }
              ]
            };
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

            this.message = 'Unable to load reports.';
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
