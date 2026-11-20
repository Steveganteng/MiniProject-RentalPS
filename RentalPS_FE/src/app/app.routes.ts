import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { DashboardComponent } from './admin/dashboard/dashboard.component';
import { CustomerDashboardComponent } from './customer/customer-dashboard/customer-dashboard.component';
import { InventoryManagementComponent } from './admin/inventory-management/inventory-management.component';
import { TransactionsComponent } from './admin/transactions/transactions.component';
import { authGuard } from './services/auth/auth.guard';
import { guestGuard } from './services/auth/guest.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [guestGuard]
  },
  {
    path: 'register',
    component: RegisterComponent,
    canActivate: [guestGuard]
  },
  {
    path: 'admin/dashboard',
    component: DashboardComponent,
    canActivate: [authGuard],
    data: { role: 'admin' }
  },
  {
    path: 'admin/transactions',
    component: TransactionsComponent,
    canActivate: [authGuard],
    data: { role: 'admin' }
  },
  {
    path: 'admin/inventory',
    component: InventoryManagementComponent,
    canActivate: [authGuard],
    data: { role: 'admin' }
  },
  {
    path: 'customer/dashboard',
    component: CustomerDashboardComponent,
    canActivate: [authGuard],
    data: { role: 'customer' }
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: ''
  }
];
