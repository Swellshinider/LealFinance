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
