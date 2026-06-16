import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./landing/landing.component').then((m) => m.LandingComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'app',
    canActivate: [authGuard],
    loadComponent: () => import('./shell/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'attendance',
        loadComponent: () => import('./attendance/attendance.component').then((m) => m.AttendanceComponent)
      },
      {
        path: 'monthly-tracking',
        loadComponent: () =>
          import('./monthly-tracking/monthly-tracking.component').then((m) => m.MonthlyTrackingComponent)
      },
      {
        path: 'material-tracking',
        loadComponent: () =>
          import('./material-tracking/material-tracking-layout.component').then((m) => m.MaterialTrackingLayoutComponent),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'projects' },
          {
            path: 'projects',
            loadComponent: () =>
              import('./material-tracking/material-tracking-menu.component').then((m) => m.MaterialTrackingMenuComponent),
            data: { mode: 'project' }
          },
          {
            path: 'projects/:name',
            loadComponent: () =>
              import('./material-tracking/material-tracking-board.component').then((m) => m.MaterialTrackingBoardComponent),
            data: { mode: 'project' }
          },
          {
            path: 'vendors',
            loadComponent: () =>
              import('./material-tracking/material-tracking-menu.component').then((m) => m.MaterialTrackingMenuComponent),
            data: { mode: 'vendor' }
          },
          {
            path: 'vendors/:name',
            loadComponent: () =>
              import('./material-tracking/material-tracking-board.component').then((m) => m.MaterialTrackingBoardComponent),
            data: { mode: 'vendor' }
          }
        ]
      },
      {
        path: 'payroll',
        loadComponent: () => import('./payroll/payroll.component').then((m) => m.PayrollComponent)
      },
      {
        path: 'employees',
        loadComponent: () => import('./employees/employees.component').then((m) => m.EmployeesComponent)
      },
      {
        path: 'invoices',
        loadComponent: () => import('./invoices/invoices.component').then((m) => m.InvoicesComponent)
      },
      {
        path: 'impress',
        loadComponent: () => import('./impress/impress.component').then((m) => m.ImpressComponent)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
