import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, timeout } from 'rxjs';

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
 * Protected dashboard API client.
 */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly apiBaseUrl = 'http://localhost:5216/api/dashboard';
  private readonly requestTimeoutMs = 15000;

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
}
