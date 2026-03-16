import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subject, timeout } from 'rxjs';

/**
 * Summary transaction card item.
 */
export interface DashboardTransaction {
  /** Transaction identifier. */
  id: number;
  /** Income or expense type. */
  type: 'Income' | 'Expense';
  /** Monetary value. */
  amount: number;
  /** Transaction category. */
  category: string;
  /** ISO timestamp. */
  date: string;
  /** Optional notes. */
  notes: string | null;
}

/**
 * Dashboard summary response payload.
 */
export interface DashboardSummaryResponse {
  /** Current account balance. */
  totalCurrentBalance: number;
  /** Latest transactions list. */
  recentTransactions: DashboardTransaction[];
}

/**
 * Upsert payload for transaction create and update operations.
 */
export interface TransactionUpsertRequest {
  /** Income or expense type. */
  type: 'Income' | 'Expense';
  /** Monetary value. */
  amount: number;
  /** Transaction category. */
  category: string;
  /** Transaction date as ISO string. */
  date: string;
  /** Optional notes. */
  notes: string | null;
}

/**
 * Reports pie chart data point.
 */
export interface ExpenseByCategoryPoint {
  /** Category label. */
  category: string;
  /** Summed expense value. */
  totalAmount: number;
}

/**
 * Reports bar chart data point.
 */
export interface MonthlyIncomeExpensePoint {
  /** Month label in yyyy-MM format. */
  month: string;
  /** Total income for month. */
  totalIncome: number;
  /** Total expenses for month. */
  totalExpense: number;
}

/**
 * Dashboard reports response payload.
 */
export interface DashboardReportsResponse {
  /** Expenses grouped by category. */
  expensesByCategory: ExpenseByCategoryPoint[];
  /** Monthly income versus expenses dataset. */
  monthlyIncomeVsExpense: MonthlyIncomeExpensePoint[];
}

/**
 * Supported report fixed date ranges.
 */
export type ReportFixedDateRange = 'day' | 'week' | 'month' | 'sixMonths' | 'year';

/**
 * Supported transaction type filters for reports.
 */
export type ReportTransactionTypeFilter = 'all' | 'Income' | 'Expense';

/**
 * Supported recurring schedule units.
 */
export type RecurringFrequencyUnit = 'Day' | 'Week' | 'Month' | 'Year';

/**
 * Recurring transaction schedule item.
 */
export interface RecurringTransaction {
  /** Schedule identifier. */
  id: number;
  /** Payment label template. */
  name: string;
  /** Income or expense type. */
  type: 'Income' | 'Expense';
  /** Monetary value. */
  amount: number;
  /** Transaction category. */
  category: string;
  /** Optional note template. */
  notes: string | null;
  /** Schedule start date (ISO). */
  startDate: string;
  /** Frequency unit. */
  frequencyUnit: RecurringFrequencyUnit;
  /** Frequency interval multiplier. */
  frequencyInterval: number;
  /** True for unlimited schedules. */
  isInfinite: boolean;
  /** Last payment sequence number for limited schedules. */
  maxOccurrences: number | null;
  /** First payment number to generate. */
  startPaymentNumber: number;
  /** Number of generated transactions. */
  generatedOccurrences: number;
  /** Next generation date (ISO). */
  nextOccurrenceDate: string | null;
  /** Active status. */
  isActive: boolean;
  /** Remaining payment count for limited schedules. */
  remainingPayments: number | null;
}

/**
 * Upsert payload for recurring transaction schedules.
 */
export interface RecurringTransactionUpsertRequest {
  /** Payment label template. */
  name: string;
  /** Income or expense type. */
  type: 'Income' | 'Expense';
  /** Monetary value. */
  amount: number;
  /** Transaction category. */
  category: string;
  /** Optional note template. */
  notes: string | null;
  /** Schedule start date as ISO string. */
  startDate: string;
  /** Frequency unit. */
  frequencyUnit: RecurringFrequencyUnit;
  /** Frequency interval multiplier. */
  frequencyInterval: number;
  /** True for unlimited schedules. */
  isInfinite: boolean;
  /** Last payment sequence number for limited schedules. */
  maxOccurrences: number | null;
  /** First payment number to generate. */
  startPaymentNumber: number;
}

/**
 * Protected dashboard API client.
 */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly apiBaseUrl = 'http://localhost:5216/api/dashboard';
  private readonly requestTimeoutMs = 15000;
  private readonly transactionsChangedSubject = new Subject<void>();
  private readonly recurringChangedSubject = new Subject<void>();

  /**
   * Emits when any transaction create, update, or delete operation succeeds.
   */
  public readonly transactionsChanged$ = this.transactionsChangedSubject.asObservable();

  /**
   * Emits when recurring schedules change.
   */
  public readonly recurringChanged$ = this.recurringChangedSubject.asObservable();

  public constructor(private readonly httpClient: HttpClient) {}

  /**
   * Gets the dashboard summary data.
   */
  public getSummary(): Observable<DashboardSummaryResponse> {
    return this.httpClient
      .get<DashboardSummaryResponse>(`${this.apiBaseUrl}/summary`)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Gets all user transactions.
   */
  public getTransactions(): Observable<DashboardTransaction[]> {
    return this.httpClient
      .get<DashboardTransaction[]>(`${this.apiBaseUrl}/transactions`)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Creates a new transaction.
   */
  public createTransaction(request: TransactionUpsertRequest): Observable<DashboardTransaction> {
    return this.httpClient
      .post<DashboardTransaction>(`${this.apiBaseUrl}/transactions`, request)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Updates an existing transaction.
   */
  public updateTransaction(transactionId: number, request: TransactionUpsertRequest): Observable<DashboardTransaction> {
    return this.httpClient
      .put<DashboardTransaction>(`${this.apiBaseUrl}/transactions/${transactionId}`, request)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Deletes a transaction.
   */
  public deleteTransaction(transactionId: number): Observable<{ message: string }> {
    return this.httpClient
      .delete<{ message: string }>(`${this.apiBaseUrl}/transactions/${transactionId}`)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Gets dashboard reports data.
   */
  public getReports(): Observable<DashboardReportsResponse> {
    return this.httpClient
      .get<DashboardReportsResponse>(`${this.apiBaseUrl}/reports`)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Gets recurring schedules for the authenticated user.
   */
  public getRecurringTransactions(): Observable<RecurringTransaction[]> {
    return this.httpClient
      .get<RecurringTransaction[]>(`${this.apiBaseUrl}/recurring`)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Creates a new recurring schedule.
   */
  public createRecurringTransaction(request: RecurringTransactionUpsertRequest): Observable<RecurringTransaction> {
    return this.httpClient
      .post<RecurringTransaction>(`${this.apiBaseUrl}/recurring`, request)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Updates an existing recurring schedule.
   */
  public updateRecurringTransaction(recurringId: number, request: RecurringTransactionUpsertRequest): Observable<RecurringTransaction> {
    return this.httpClient
      .put<RecurringTransaction>(`${this.apiBaseUrl}/recurring/${recurringId}`, request)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Cancels a recurring schedule.
   */
  public cancelRecurringTransaction(recurringId: number): Observable<{ message: string }> {
    return this.httpClient
      .delete<{ message: string }>(`${this.apiBaseUrl}/recurring/${recurringId}`)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Broadcasts a transaction data change event to all dashboard tabs.
   */
  public notifyTransactionsChanged(): void {
    this.transactionsChangedSubject.next();
  }

  /**
   * Broadcasts a recurring schedule change event.
   */
  public notifyRecurringChanged(): void {
    this.recurringChangedSubject.next();
  }
}
