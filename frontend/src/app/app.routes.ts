import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { MainLayoutComponent } from './core/layout/main-layout/main-layout.component';
import { LoginComponent } from './features/auth/login.component';
import { RegisterComponent } from './features/auth/register.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { LandingComponent } from './features/landing/landing.component';
import { TransactionsHistoryPageComponent } from './features/dashboard/transactions-history-page/transactions-history-page.component';
import { ProfileComponent } from './features/profile/profile.component';

export const routes: Routes = [
	{
		path: '',
		component: MainLayoutComponent,
		children: [
			{
				path: '',
				component: LandingComponent
			},
			{
				path: 'login',
				component: LoginComponent
			},
			{
				path: 'register',
				component: RegisterComponent
			},
			{
				path: 'dashboard',
				component: DashboardComponent,
				canActivate: [authGuard]
			},
			{
				path: 'dashboard/transactions-history',
				component: TransactionsHistoryPageComponent,
				canActivate: [authGuard]
			},
			{
				path: 'profile',
				component: ProfileComponent,
				canActivate: [authGuard]
			},
			{
				path: '**',
				redirectTo: ''
			}
		]
	},
];
