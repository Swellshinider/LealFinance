import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import {
  DashboardService,
  DashboardTransaction,
  TransactionUpsertRequest
} from '../../../core/services/dashboard.service';

/**
 * Displays transactions CRUD operations inside the dashboard.
 */
@Component({
  selector: 'app-transactions-tab',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatAutocompleteModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatTableModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './transactions-tab.component.html',
  styleUrl: './transactions-tab.component.scss'
})
export class TransactionsTabComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  /** Table columns definition. */
  public readonly displayedColumns = ['date', 'type', 'category', 'amount', 'notes', 'actions'];

  /** Indicates data loading state. */
  public isLoading = true;

  /** Indicates submit state for create or update action. */
  public isSubmitting = false;

  /** Indicates delete operation in progress for a row. */
  public deletingTransactionId: number | null = null;

  /** Error or status message. */
  public message = '';

  /** Current rows shown in table. */
  public transactions: DashboardTransaction[] = [];

  /** Edited transaction identifier. Null means create mode. */
  public editingTransactionId: number | null = null;

  /** All existing user categories extracted from transaction history. */
  public categorySuggestions: string[] = [];

  /** Categories filtered by current input. */
  public filteredCategorySuggestions: string[] = [];

  /** Reactive form for transaction create and update. */
  public readonly transactionForm = this.formBuilder.nonNullable.group({
    type: ['Expense' as 'Income' | 'Expense', [Validators.required]],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    category: ['', [Validators.required, Validators.maxLength(100)]],
    date: [this.getTodayIsoDate(), [Validators.required]],
    notes: ['', [Validators.maxLength(500)]]
  });

  public constructor(
    private readonly dashboardService: DashboardService,
    private readonly router: Router
  ) {}

  /**
   * Loads transaction history.
   */
  public ngOnInit(): void {
    this.transactionForm.controls.category.valueChanges.subscribe((value: string) => {
      this.filterCategorySuggestions(value);
    });

    this.loadTransactions();
  }

  /**
   * Updates category autocomplete suggestions as the user types.
   */
  public onCategoryInput(): void {
    this.filterCategorySuggestions(this.transactionForm.controls.category.value);
  }

  /**
   * Submits the form to create or update a transaction.
   */
  public onSubmit(): void {
    if (this.isSubmitting) {
      return;
    }

    this.message = '';

    if (this.transactionForm.invalid) {
      this.transactionForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    const payload: TransactionUpsertRequest = {
      type: this.transactionForm.controls.type.value,
      amount: Number(this.transactionForm.controls.amount.value),
      category: this.transactionForm.controls.category.value.trim(),
      date: this.transactionForm.controls.date.value,
      notes: this.transactionForm.controls.notes.value.trim() || null
    };

    const saveRequest = this.editingTransactionId === null
      ? this.dashboardService.createTransaction(payload)
      : this.dashboardService.updateTransaction(this.editingTransactionId, payload);

    saveRequest
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.isSubmitting = false;
          });
        })
      )
      .subscribe({
        next: () => {
          this.runInAngular(() => {
            this.resetForm();
            this.loadTransactions();
          });
        },
        error: (error: unknown) => {
          this.handleError(error, 'Unable to save transaction.');
        }
      });
  }

  /**
   * Puts a row into edit mode and pre-fills the form.
   */
  public onEdit(transaction: DashboardTransaction): void {
    this.editingTransactionId = transaction.id;

    this.transactionForm.setValue({
      type: transaction.type,
      amount: transaction.amount,
      category: transaction.category,
      date: transaction.date.slice(0, 10),
      notes: transaction.notes ?? ''
    });
  }

  /**
   * Deletes a transaction row.
   */
  public onDelete(transaction: DashboardTransaction): void {
    if (this.deletingTransactionId !== null) {
      return;
    }

    const confirmed = window.confirm('Delete this transaction permanently?');
    if (!confirmed) {
      return;
    }

    this.deletingTransactionId = transaction.id;

    this.dashboardService
      .deleteTransaction(transaction.id)
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.deletingTransactionId = null;
          });
        })
      )
      .subscribe({
        next: () => {
          this.runInAngular(() => {
            this.transactions = this.transactions.filter((row) => row.id !== transaction.id);
            if (this.editingTransactionId === transaction.id) {
              this.resetForm();
            }
          });
        },
        error: (error: unknown) => {
          this.handleError(error, 'Unable to delete transaction.');
        }
      });
  }

  /**
   * Cancels edit mode and resets the form to defaults.
   */
  public cancelEdit(): void {
    this.resetForm();
  }

  private loadTransactions(): void {
    this.isLoading = true;

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
            this.transactions = response;
            this.categorySuggestions = [...new Set(
              response
                .map((transaction: DashboardTransaction) => transaction.category.trim())
                .filter((category: string) => category.length > 0)
            )].sort((left: string, right: string) => left.localeCompare(right));
            this.filterCategorySuggestions(this.transactionForm.controls.category.value);
          });
        },
        error: (error: unknown) => {
          this.handleError(error, 'Unable to load transaction history.');
        }
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

  private resetForm(): void {
    this.editingTransactionId = null;
    this.transactionForm.reset({
      type: 'Expense',
      amount: 0,
      category: '',
      date: this.getTodayIsoDate(),
      notes: ''
    });
  }

  private filterCategorySuggestions(value: string): void {
    const normalized = value.trim().toLowerCase();

    this.filteredCategorySuggestions = this.categorySuggestions.filter((category: string) =>
      normalized.length === 0 || category.toLowerCase().includes(normalized));
  }

  private getTodayIsoDate(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private runInAngular(action: () => void): void {
    this.ngZone.run(() => {
      action();
      this.changeDetectorRef.detectChanges();
    });
  }
}
