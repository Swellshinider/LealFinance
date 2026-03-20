import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { MainLayoutComponent } from './core/layout/main-layout/main-layout.component';

export const routes: Routes = [
	{
		path: '',
		component: MainLayoutComponent,
		children: [
			{
				path: '',
				loadComponent: () => import('./features/landing/landing.component').then((m) => m.LandingComponent)
			},
			{
				path: 'login',
				loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent)
			},
			{
				path: 'register',
				loadComponent: () => import('./features/auth/register.component').then((m) => m.RegisterComponent)
			},
			{
				path: 'dashboard',
				loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
				canActivate: [authGuard]
			},
			{
				path: 'dashboard/transactions-history',
				loadComponent: () =>
					import('./features/dashboard/transactions-history-page/transactions-history-page.component').then(
						(m) => m.TransactionsHistoryPageComponent
					),
				canActivate: [authGuard]
			},
			{
				path: 'profile',
				loadComponent: () => import('./features/profile/profile.component').then((m) => m.ProfileComponent),
				canActivate: [authGuard]
			},
			{
				path: '**',
				redirectTo: ''
			}
		]
	},
];
