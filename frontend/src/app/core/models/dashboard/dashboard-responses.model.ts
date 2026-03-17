import { DashboardTransaction } from './dashboard-transaction.model';

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
 * Dashboard summary response payload.
 */
export interface DashboardSummaryResponse {
  /** Current account balance. */
  totalCurrentBalance: number;
  /** Latest transactions list. */
  recentTransactions: DashboardTransaction[];
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
