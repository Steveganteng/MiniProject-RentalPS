import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { DashboardComponent } from './admin/dashboard/dashboard.component';
import { CustomerDashboardComponent } from './customer/customer-dashboard/customer-dashboard.component';
import { authGuard } from './services/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'register',
    component: RegisterComponent
  },
  {
    path: 'admin/dashboard',
    component: DashboardComponent,
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
  }
];