import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { DashboardComponent } from './admin/dashboard/dashboard.component';
import { ConsoleTypeListComponent } from './inventory/console-type-list/console-type-list.component';
import { RoomTypeListComponent } from './inventory/room-type-list/room-type-list.component';
import { RoomListComponent } from './inventory/room-list/room-list.component';
import { ConsoleUnitListComponent } from './inventory/console-unit-list/console-unit-list.component';

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
  path: '',
  redirectTo: 'admin/dashboard',
  pathMatch: 'full'
},
  {
    path: 'admin/dashboard',
    component: DashboardComponent
  },
  {
    path: 'admin/inventory/console-types',
    component: ConsoleTypeListComponent
  },
  {
    path: 'admin/inventory/room-type-list',
    component: RoomTypeListComponent
  },
  {
    path: 'admin/inventory/room-list',
    component: RoomListComponent
  },
  {
    path: 'admin/inventory/console-unit-list',
    component: ConsoleUnitListComponent
  }
];