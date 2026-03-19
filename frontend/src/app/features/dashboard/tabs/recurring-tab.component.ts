import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import {
  RecurringFrequencyUnit,
  RecurringTransaction,
  RecurringTransactionUpsertRequest
} from '../../../core/models/dashboard';
import { DashboardService } from '../../../core/services/dashboard.service';

/**
 * Displays recurring transaction schedule management.
 */
@Component({
  selector: 'app-recurring-tab',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatTableModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './recurring-tab.component.html',
  styleUrl: './recurring-tab.component.scss'
})
export class RecurringTabComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  /** Table columns definition. */
  public readonly displayedColumns = ['name', 'type', 'schedule', 'nextOccurrence', 'remaining', 'amount', 'status', 'actions'];

  /** Supported recurring frequency units. */
  public readonly frequencyUnits: Array<{ value: RecurringFrequencyUnit; label: string }> = [
    { value: 'Day', label: 'Day(s)' },
    { value: 'Week', label: 'Week(s)' },
    { value: 'Month', label: 'Month(s)' },
    { value: 'Year', label: 'Year(s)' }
  ];

  /** Current recurring rows. */
  public recurringItems: RecurringTransaction[] = [];

  /** Indicates recurring data loading state. */
  public isLoading = true;

  /** Indicates submit state for create or update action. */
  public isSubmitting = false;

  /** Active row being canceled. */
  public cancelingRecurringId: number | null = null;

  /** Error or status message. */
  public message = '';

  /** Edited recurring identifier. Null means create mode. */
  public editingRecurringId: number | null = null;

  /** Reactive form for recurring create and update. */
  public readonly recurringForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    type: ['Expense' as 'Income' | 'Expense', [Validators.required]],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    category: ['', [Validators.required, Validators.maxLength(100)]],
    startDate: [this.getTodayIsoDate(), [Validators.required]],
    frequencyInterval: [1, [Validators.required, Validators.min(1), Validators.max(3650)]],
    frequencyUnit: ['Month' as RecurringFrequencyUnit, [Validators.required]],
    isInfinite: [true],
    maxOccurrences: [12, [Validators.min(1), Validators.max(100000)]],
    startPaymentNumber: [1, [Validators.required, Validators.min(1), Validators.max(100000)]],
    notes: ['', [Validators.maxLength(500)]]
  });

  public constructor(
    private readonly dashboardService: DashboardService,
    private readonly router: Router
  ) {}

  /**
   * Loads recurring transactions.
   */
  public ngOnInit(): void {
    this.dashboardService.recurringChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadRecurring();
      });

    this.loadRecurring();
  }

  /**
   * Submits the form to create or update a recurring schedule.
   */
  public onSubmit(): void {
    if (this.isSubmitting) {
      return;
    }

    this.message = '';

    if (this.recurringForm.invalid) {
      this.recurringForm.markAllAsTouched();
      return;
    }

    const formValue = this.recurringForm.getRawValue();
    if (!formValue.isInfinite && formValue.startPaymentNumber > formValue.maxOccurrences) {
      this.message = 'Start payment number cannot be greater than the total payments.';
      return;
    }

    this.isSubmitting = true;

    const payload: RecurringTransactionUpsertRequest = {
      name: formValue.name.trim(),
      type: formValue.type,
      amount: Number(formValue.amount),
      category: formValue.category.trim(),
      startDate: formValue.startDate,
      frequencyInterval: Number(formValue.frequencyInterval),
      frequencyUnit: formValue.frequencyUnit,
      isInfinite: formValue.isInfinite,
      maxOccurrences: formValue.isInfinite ? null : Number(formValue.maxOccurrences),
      startPaymentNumber: Number(formValue.startPaymentNumber),
      notes: formValue.notes.trim() || null
    };

    const saveRequest = this.editingRecurringId === null
      ? this.dashboardService.createRecurringTransaction(payload)
      : this.dashboardService.updateRecurringTransaction(this.editingRecurringId, payload);

    saveRequest
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.isSubmitting = false;
          });
        })
      )
      .subscribe({
        next: (savedRecurring: RecurringTransaction) => {
          this.runInAngular(() => {
            this.upsertRecurring(savedRecurring);
            this.dashboardService.notifyRecurringChanged();
            this.resetForm();
          });
        },
        error: (error: unknown) => {
          this.handleError(error, 'Unable to save recurring schedule.');
        }
      });
  }

  /**
   * Puts a row into edit mode and pre-fills the form.
   */
  public onEdit(item: RecurringTransaction): void {
    this.editingRecurringId = item.id;

    this.recurringForm.setValue({
      name: item.name,
      type: item.type,
      amount: item.amount,
      category: item.category,
      startDate: item.startDate.slice(0, 10),
      frequencyInterval: item.frequencyInterval,
      frequencyUnit: item.frequencyUnit,
      isInfinite: item.isInfinite,
      maxOccurrences: item.maxOccurrences ?? 1,
      startPaymentNumber: item.startPaymentNumber,
      notes: item.notes ?? ''
    });
  }

  /**
   * Cancels an active recurring schedule.
   */
  public onCancelRecurring(item: RecurringTransaction): void {
    if (this.cancelingRecurringId !== null || !item.isActive) {
      return;
    }

    const confirmed = window.confirm('Cancel this recurring schedule? Existing generated transactions will remain unchanged.');
    if (!confirmed) {
      return;
    }

    this.cancelingRecurringId = item.id;

    this.dashboardService
      .cancelRecurringTransaction(item.id)
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.cancelingRecurringId = null;
          });
        })
      )
      .subscribe({
        next: () => {
          this.runInAngular(() => {
            this.recurringItems = this.sortRecurring(this.recurringItems.map((row) =>
              row.id === item.id
                ? { ...row, isActive: false, nextOccurrenceDate: null, remainingPayments: 0 }
                : row));
            this.dashboardService.notifyRecurringChanged();
            if (this.editingRecurringId === item.id) {
              this.resetForm();
            }
          });
        },
        error: (error: unknown) => {
          this.handleError(error, 'Unable to cancel recurring schedule.');
        }
      });
  }

  /**
   * Cancels edit mode and resets the form.
   */
  public cancelEdit(): void {
    this.resetForm();
  }

  /**
   * Gets a human-readable schedule label.
   */
  public getScheduleLabel(item: RecurringTransaction): string {
    const suffix = item.frequencyInterval > 1 ? 's' : '';
    return `Every ${item.frequencyInterval} ${item.frequencyUnit.toLowerCase()}${suffix}`;
  }

  private loadRecurring(): void {
    this.runInAngular(() => {
      this.isLoading = true;
      this.message = '';
    });

    this.dashboardService
      .getRecurringTransactions()
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.isLoading = false;
          });
        })
      )
      .subscribe({
        next: (response: RecurringTransaction[]) => {
          this.runInAngular(() => {
            this.recurringItems = this.sortRecurring(response);
          });
        },
        error: (error: unknown) => {
          this.handleError(error, 'Unable to load recurring schedules.');
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

      if (error instanceof HttpErrorResponse && error.status === 403) {
        this.message = 'Enable authenticator-based 2FA in your profile to access recurring schedules.';
        void this.router.navigate(['/profile']);
        return;
      }

      this.message = fallbackMessage;
    });
  }

  private resetForm(): void {
    this.editingRecurringId = null;
    this.recurringForm.reset({
      name: '',
      type: 'Expense',
      amount: 0,
      category: '',
      startDate: this.getTodayIsoDate(),
      frequencyInterval: 1,
      frequencyUnit: 'Month',
      isInfinite: true,
      maxOccurrences: 12,
      startPaymentNumber: 1,
      notes: ''
    });
  }

  private upsertRecurring(item: RecurringTransaction): void {
    const existingIndex = this.recurringItems.findIndex((row) => row.id === item.id);
    if (existingIndex >= 0) {
      this.recurringItems = this.recurringItems.map((row) => (row.id === item.id ? item : row));
    } else {
      this.recurringItems = [...this.recurringItems, item];
    }

    this.recurringItems = this.sortRecurring(this.recurringItems);
  }

  private sortRecurring(items: RecurringTransaction[]): RecurringTransaction[] {
    return [...items].sort((left, right) => {
      if (left.isActive !== right.isActive) {
        return left.isActive ? -1 : 1;
      }

      const leftDate = left.nextOccurrenceDate ? new Date(left.nextOccurrenceDate).getTime() : Number.MAX_SAFE_INTEGER;
      const rightDate = right.nextOccurrenceDate ? new Date(right.nextOccurrenceDate).getTime() : Number.MAX_SAFE_INTEGER;
      if (leftDate !== rightDate) {
        return leftDate - rightDate;
      }

      return right.id - left.id;
    });
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
