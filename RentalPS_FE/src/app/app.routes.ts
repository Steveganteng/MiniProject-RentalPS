import { Routes } from '@angular/router';
import { ConsoleTypeListComponent } from './features/inventory/console-type-list/console-type-list.component';

export const routes: Routes = [
    {
        path: 'inventory/console-types',
        component: ConsoleTypeListComponent
    },
    {
        path: '',
        redirectTo: 'admin/inventory/console-types',
        pathMatch: 'full'
    }
];
