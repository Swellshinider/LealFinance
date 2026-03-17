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
