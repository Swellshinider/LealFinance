import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';

import { RecurringTabComponent } from './tabs/recurring-tab.component';
import { ReportsTabComponent } from './tabs/reports-tab.component';
import { SummaryTabComponent } from './tabs/summary-tab.component';
import { TransactionsTabComponent } from './tabs/transactions-tab.component';

/**
 * Displays protected dashboard data.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [MatCardModule, MatTabsModule, SummaryTabComponent, TransactionsTabComponent, ReportsTabComponent, RecurringTabComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {}
