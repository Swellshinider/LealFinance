import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subject, timeout } from 'rxjs';

import {
  DashboardReportsResponse,
  DashboardSummaryResponse,
  DashboardTransaction,
  RecurringTransaction,
  RecurringTransactionUpsertRequest,
  TransactionUpsertRequest
} from '../models/dashboard';
import { environment } from '../../../environments/environment';

/**
 * Protected dashboard API client.
 */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/dashboard`;
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
