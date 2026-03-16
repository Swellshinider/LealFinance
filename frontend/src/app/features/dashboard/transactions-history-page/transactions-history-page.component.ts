import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { PageEvent, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';

import { DashboardService, DashboardTransaction } from '../../../core/services/dashboard.service';

/**
 * Displays the complete transaction history with filters, pagination, and Excel export.
 */
@Component({
  selector: 'app-transactions-history-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatTableModule,
    MatPaginatorModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './transactions-history-page.component.html',
  styleUrl: './transactions-history-page.component.scss'
})
export class TransactionsHistoryPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  /** Table columns definition. */
  public readonly displayedColumns = ['date', 'type', 'category', 'amount', 'notes'];

  /** Available page size options. */
  public readonly pageSizeOptions = [5, 10, 25, 50, 100];

  /** Indicates data loading state. */
  public isLoading = true;

  /** Indicates export operation in progress. */
  public isExporting = false;

  /** User feedback message. */
  public message = '';

  /** Full transaction list. */
  public allTransactions: DashboardTransaction[] = [];

  /** Transactions after active filters are applied. */
  public filteredTransactions: DashboardTransaction[] = [];

  /** Transactions shown in current page. */
  public pagedTransactions: DashboardTransaction[] = [];

  /** Current page index (zero-based). */
  public pageIndex = 0;

  /** Current selected page size. */
  public pageSize = 10;

  /** Total filtered records count. */
  public totalFilteredCount = 0;

  /** Filter form for date range and pagination size. */
  public readonly filterForm = this.formBuilder.nonNullable.group({
    startDate: [''],
    endDate: [''],
    pageSize: [10]
  });

  public constructor(
    private readonly dashboardService: DashboardService,
    private readonly router: Router
  ) {}

  /**
   * Loads all transactions for the authenticated user.
   */
  public ngOnInit(): void {
    this.loadTransactions();
  }

  /**
   * Applies filters from the form and resets to the first page.
   */
  public onApplyFilters(): void {
    this.pageSize = Number(this.filterForm.controls.pageSize.value) || 10;
    this.pageIndex = 0;
    this.applyFiltersAndPagination();
  }

  /**
   * Clears filters and restores default page size.
   */
  public onResetFilters(): void {
    this.filterForm.reset({
      startDate: '',
      endDate: '',
      pageSize: 10
    });

    this.pageSize = 10;
    this.pageIndex = 0;
    this.applyFiltersAndPagination();
  }

  /**
   * Handles paginator page and size changes.
   */
  public onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.filterForm.controls.pageSize.setValue(event.pageSize);
    this.applyFiltersAndPagination(false);
  }

  /**
   * Exports filtered transaction data to an Excel file.
   */
  public async onExportToExcel(): Promise<void> {
    if (this.filteredTransactions.length === 0 || this.isExporting) {
      return;
    }

    this.runInAngular(() => {
      this.isExporting = true;
      this.message = '';
    });

    try {
      const xlsx = await import('xlsx');
      const exportRows = this.filteredTransactions.map((transaction: DashboardTransaction) => ({
        Date: new Date(transaction.date).toLocaleDateString(),
        Type: transaction.type,
        Category: transaction.category,
        Amount: transaction.type === 'Expense' ? -transaction.amount : transaction.amount,
        Notes: transaction.notes ?? ''
      }));

      const worksheet = xlsx.utils.json_to_sheet(exportRows);
      const workbook = xlsx.utils.book_new();
      xlsx.utils.book_append_sheet(workbook, worksheet, 'Transactions');
      xlsx.writeFile(workbook, `transactions-${new Date().toISOString().slice(0, 10)}.xlsx`);
    } catch {
      this.runInAngular(() => {
        this.message = 'Unable to export transactions to Excel.';
      });
    } finally {
      this.runInAngular(() => {
        this.isExporting = false;
      });
    }
  }

  private loadTransactions(): void {
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
        next: (response: DashboardTransaction[]) => {
          this.runInAngular(() => {
            this.allTransactions = this.sortTransactions(response);
            this.applyFiltersAndPagination();
          });
        },
        error: (error: unknown) => {
          this.handleError(error, 'Unable to load transaction history.');
        }
      });
  }

  private applyFiltersAndPagination(resetMessage = true): void {
    if (resetMessage) {
      this.message = '';
    }

    const startDateInput = this.filterForm.controls.startDate.value;
    const endDateInput = this.filterForm.controls.endDate.value;

    const startDate = startDateInput ? new Date(`${startDateInput}T00:00:00`) : null;
    const endDate = endDateInput ? new Date(`${endDateInput}T23:59:59.999`) : null;

    if (startDate !== null && endDate !== null && startDate.getTime() > endDate.getTime()) {
      this.filteredTransactions = [];
      this.pagedTransactions = [];
      this.totalFilteredCount = 0;
      this.message = 'Start date cannot be after end date.';
      return;
    }

    this.filteredTransactions = this.allTransactions.filter((transaction: DashboardTransaction) => {
      const transactionDate = new Date(transaction.date);

      if (startDate !== null && transactionDate.getTime() < startDate.getTime()) {
        return false;
      }

      if (endDate !== null && transactionDate.getTime() > endDate.getTime()) {
        return false;
      }

      return true;
    });

    this.totalFilteredCount = this.filteredTransactions.length;

    const maxPageIndex = Math.max(Math.ceil(this.totalFilteredCount / this.pageSize) - 1, 0);
    if (this.pageIndex > maxPageIndex) {
      this.pageIndex = 0;
    }

    const startIndex = this.pageIndex * this.pageSize;
    this.pagedTransactions = this.filteredTransactions.slice(startIndex, startIndex + this.pageSize);
  }

  private sortTransactions(transactions: DashboardTransaction[]): DashboardTransaction[] {
    return [...transactions].sort((left: DashboardTransaction, right: DashboardTransaction) => {
      const dateDifference = new Date(right.date).getTime() - new Date(left.date).getTime();
      if (dateDifference !== 0) {
        return dateDifference;
      }

      return right.id - left.id;
    });
  }

  private handleError(error: unknown, fallbackMessage: string): void {
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

      this.message = fallbackMessage;
    });
  }

  private runInAngular(action: () => void): void {
    this.ngZone.run(() => {
      action();
      this.changeDetectorRef.detectChanges();
    });
  }
}
