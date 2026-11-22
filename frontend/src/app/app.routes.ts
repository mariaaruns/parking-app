import { Routes } from '@angular/router';
import { roleGuard } from './core/role.guard';
import { ShellComponent } from './shared/shell/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.page').then((m) => m.LoginPage),
    title: 'Sign in • ParkEase',
  },
  {
    path: 'p/:token',
    loadComponent: () => import('./pages/public-ticket/public-ticket.page').then((m) => m.PublicTicketPage),
    title: 'Digital Parking Pass • ParkEase',
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [roleGuard],
    children: [
      {
        path: 'staff',
        loadComponent: () => import('./pages/staff/staff.page').then((m) => m.StaffPage),
        canActivate: [roleGuard],
        data: { roles: ['Staff'] },
        title: 'Gate Workspace • ParkEase',
      },
      {
        path: 'admin',
        loadComponent: () => import('./pages/admin/admin.page').then((m) => m.AdminPage),
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SuperAdmin'] },
        title: 'Management & Analytics • ParkEase',
      },
      {
        path: 'super-admin',
        loadComponent: () => import('./pages/super-admin/super-admin.page').then((m) => m.SuperAdminPage),
        canActivate: [roleGuard],
        data: { roles: ['SuperAdmin'] },
        title: 'Network Control • ParkEase',
      },
      { path: '', pathMatch: 'full', redirectTo: 'login' },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
