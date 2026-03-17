import { RecurringFrequencyUnit } from './recurring-transaction.model';

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
