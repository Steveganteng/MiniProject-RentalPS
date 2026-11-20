import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { DashboardComponent } from './admin/dashboard/dashboard.component';
import { CustomerDashboardComponent } from './customer/customer-dashboard/customer-dashboard.component';
import { authGuard } from './services/auth/auth.guard';

import { ConsoleTypeListComponent } from './inventory/console-type-list/console-type-list.component';
import { RoomTypeListComponent } from './inventory/room-type-list/room-type-list.component';
import { RoomListComponent } from './inventory/room-list/room-list.component';
import { ConsoleUnitListComponent } from './inventory/console-unit-list/console-unit-list.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

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
    path: 'admin/inventory/console-types',
    component: ConsoleTypeListComponent,
    canActivate: [authGuard],
    data: { role: 'admin' }
  },
  {
    path: 'admin/inventory/room-type-list',
    component: RoomTypeListComponent,
    canActivate: [authGuard],
    data: { role: 'admin' }
  },
  {
    path: 'admin/inventory/room-list',
    component: RoomListComponent,
    canActivate: [authGuard],
    data: { role: 'admin' }
  },
  {
    path: 'admin/inventory/console-unit-list',
    component: ConsoleUnitListComponent,
    canActivate: [authGuard],
    data: { role: 'admin' }
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];