import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';
import { ChartConfiguration, ChartOptions } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { finalize } from 'rxjs';

import {
  DashboardTransaction,
  ReportFixedDateRange,
  ReportTransactionTypeFilter
} from '../../../core/models/dashboard';
import { DashboardService } from '../../../core/services/dashboard.service';

/**
 * Supported date filter modes.
 */
type ReportDateFilterMode = 'all' | 'upTo' | 'range' | 'fixed';

/**
 * Displays report visualizations for expenses and monthly cash flow.
 */
@Component({
  selector: 'app-reports-tab',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    BaseChartDirective
  ],
  templateUrl: './reports-tab.component.html',
  styleUrl: './reports-tab.component.scss'
})
export class ReportsTabComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  /** Raw transactions source for report filtering. */
  private allTransactions: DashboardTransaction[] = [];

  /** Indicates report loading state. */
  public isLoading = true;

  /** Error message for reports API failures. */
  public message = '';

  /** All available categories for filter selection. */
  public categoryOptions: string[] = [];

  /** Date filter mode options for the form. */
  public readonly dateFilterModes: Array<{ value: ReportDateFilterMode; label: string }> = [
    { value: 'all', label: 'All time' },
    { value: 'upTo', label: 'Up to date' },
    { value: 'range', label: 'Date range' },
    { value: 'fixed', label: 'Fixed period' }
  ];

  /** Fixed period options for quick date filtering. */
  public readonly fixedDateRanges: Array<{ value: ReportFixedDateRange; label: string }> = [
    { value: 'day', label: 'Last day' },
    { value: 'week', label: 'Last week' },
    { value: 'month', label: 'Last month' },
    { value: 'sixMonths', label: 'Last six months' },
    { value: 'year', label: 'Last year' }
  ];

  /** Transaction type options for reports. */
  public readonly typeOptions: Array<{ value: ReportTransactionTypeFilter; label: string }> = [
    { value: 'all', label: 'Incomes + expenses' },
    { value: 'Income', label: 'Only incomes' },
    { value: 'Expense', label: 'Only expenses' }
  ];

  /** Reports filter form. */
  public readonly reportFiltersForm = this.formBuilder.nonNullable.group({
    dateMode: ['all' as ReportDateFilterMode],
    upToDate: [''],
    fromDate: [''],
    toDate: [''],
    fixedRange: ['month' as ReportFixedDateRange],
    category: ['all'],
    type: ['all' as ReportTransactionTypeFilter]
  });

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
    this.reportFiltersForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.runInAngular(() => {
          this.applyFiltersAndRebuildCharts();
        });
      });

    this.dashboardService.transactionsChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadTransactionsForReports();
      });

    this.loadTransactionsForReports();
  }

  /**
   * Resets all reports filters to defaults.
   */
  public resetFilters(): void {
    this.reportFiltersForm.reset({
      dateMode: 'all',
      upToDate: '',
      fromDate: '',
      toDate: '',
      fixedRange: 'month',
      category: 'all',
      type: 'all'
    });
  }

  private loadTransactionsForReports(): void {
    this.runInAngular(() => {
      this.isLoading = true;
      this.message = '';
    });

    this.dashboardService
      .getTransactions()
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.isLoading = false;
          });
        })
      )
      .subscribe({
        next: (transactions: DashboardTransaction[]) => {
          this.runInAngular(() => {
            this.allTransactions = transactions;
            this.categoryOptions = [...new Set(
              transactions
                .map((transaction: DashboardTransaction) => transaction.category.trim())
                .filter((category: string) => category.length > 0)
            )].sort((left: string, right: string) => left.localeCompare(right));

            this.applyFiltersAndRebuildCharts();
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

            if (error instanceof HttpErrorResponse && error.status === 403) {
              this.message = 'Enable authenticator-based 2FA in your profile to access reports.';
              void this.router.navigate(['/profile']);
              return;
            }

            this.message = 'Unable to load reports.';
          });
        }
      });
  }

  private applyFiltersAndRebuildCharts(): void {
    const filters = this.reportFiltersForm.getRawValue();
    const now = new Date();

    let filteredTransactions = [...this.allTransactions];

    if (filters.type !== 'all') {
      filteredTransactions = filteredTransactions.filter((transaction) => transaction.type === filters.type);
    }

    if (filters.category !== 'all') {
      filteredTransactions = filteredTransactions.filter((transaction) => transaction.category === filters.category);
    }

    filteredTransactions = filteredTransactions.filter((transaction) =>
      this.matchesDateFilter(transaction.date, filters.dateMode, filters.upToDate, filters.fromDate, filters.toDate, filters.fixedRange, now));

    const expenseTotals = new Map<string, number>();
    const monthlyTotals = new Map<string, { income: number; expense: number }>();

    for (const transaction of filteredTransactions) {
      if (transaction.type === 'Expense') {
        const currentExpenseAmount = expenseTotals.get(transaction.category) ?? 0;
        expenseTotals.set(transaction.category, currentExpenseAmount + Number(transaction.amount));
      }

      const transactionDate = new Date(transaction.date);
      const monthKey = `${transactionDate.getUTCFullYear()}-${String(transactionDate.getUTCMonth() + 1).padStart(2, '0')}`;
      const currentMonthTotal = monthlyTotals.get(monthKey) ?? { income: 0, expense: 0 };

      if (transaction.type === 'Income') {
        currentMonthTotal.income += Number(transaction.amount);
      } else {
        currentMonthTotal.expense += Number(transaction.amount);
      }

      monthlyTotals.set(monthKey, currentMonthTotal);
    }

    const sortedExpenses = [...expenseTotals.entries()]
      .map(([category, totalAmount]) => ({ category, totalAmount }))
      .sort((left, right) => right.totalAmount - left.totalAmount);

    const sortedMonths = [...monthlyTotals.entries()]
      .sort(([left], [right]) => left.localeCompare(right));

    this.pieChartData = {
      labels: sortedExpenses.map((item) => item.category),
      datasets: [
        {
          data: sortedExpenses.map((item) => item.totalAmount),
          backgroundColor: ['#ef4444', '#f97316', '#f59e0b', '#8b5cf6', '#06b6d4', '#22c55e', '#ec4899']
        }
      ]
    };

    this.barChartData = {
      labels: sortedMonths.map(([month]) => month),
      datasets: [
        {
          label: 'Income',
          data: sortedMonths.map(([, totals]) => totals.income),
          backgroundColor: '#16a34a'
        },
        {
          label: 'Expense',
          data: sortedMonths.map(([, totals]) => totals.expense),
          backgroundColor: '#dc2626'
        }
      ]
    };
  }

  private matchesDateFilter(
    transactionDateIso: string,
    mode: ReportDateFilterMode,
    upToDate: string,
    fromDate: string,
    toDate: string,
    fixedRange: ReportFixedDateRange,
    now: Date
  ): boolean {
    const transactionDate = new Date(transactionDateIso);

    if (mode === 'upTo') {
      if (!upToDate) {
        return true;
      }

      const upperBoundary = new Date(upToDate);
      upperBoundary.setHours(23, 59, 59, 999);
      return transactionDate <= upperBoundary;
    }

    if (mode === 'range') {
      const hasFrom = fromDate.length > 0;
      const hasTo = toDate.length > 0;

      if (!hasFrom && !hasTo) {
        return true;
      }

      const lowerBoundary = hasFrom ? new Date(fromDate) : null;
      const upperBoundary = hasTo ? new Date(toDate) : null;
      if (upperBoundary) {
        upperBoundary.setHours(23, 59, 59, 999);
      }

      if (lowerBoundary && transactionDate < lowerBoundary) {
        return false;
      }

      if (upperBoundary && transactionDate > upperBoundary) {
        return false;
      }

      return true;
    }

    if (mode === 'fixed') {
      const start = new Date(now);
      switch (fixedRange) {
        case 'day':
          start.setDate(start.getDate() - 1);
          break;
        case 'week':
          start.setDate(start.getDate() - 7);
          break;
        case 'month':
          start.setMonth(start.getMonth() - 1);
          break;
        case 'sixMonths':
          start.setMonth(start.getMonth() - 6);
          break;
        case 'year':
          start.setFullYear(start.getFullYear() - 1);
          break;
      }

      return transactionDate >= start && transactionDate <= now;
    }

    return true;
  }

  private runInAngular(action: () => void): void {
    this.ngZone.run(() => {
      action();
      this.changeDetectorRef.detectChanges();
    });
  }
}
