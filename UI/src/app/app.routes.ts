import { Routes } from '@angular/router';
import { DailyUpdateComponent } from './pages/daily-update/daily-update.component';
import { NoAccessComponent } from './auth/components/no-access.component';
import { AuthLoadingComponent } from './auth/components/auth-loading.component';
import { MyTasksComponent } from './pages/my-tasks/my-tasks.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

export const routes: Routes = [
  { path: '', component: AuthLoadingComponent }, // Start with authentication
  { path: 'auth', component: AuthLoadingComponent },
  { path: 'no-access', component: NoAccessComponent },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'my-tasks', component: MyTasksComponent },
  { path: 'daily-update', component: DailyUpdateComponent },
  {
    path: 'reports',
    loadComponent: () =>
      import('./pages/report/report.component').then((m) => m.ReportComponent),
  },
  {
    path: 'user-management',
    loadComponent: () =>
      import('./pages/user-management/user-management.component').then(
        (m) => m.UserManagementComponent,
      ),
  },
  {
    path: 'shift-management',
    loadComponent: () =>
      import('./pages/shift-management/shift-management.component').then(
        (m) => m.ShiftManagementComponent,
      ),
  },
  { path: '**', redirectTo: 'daily-update' },
];
