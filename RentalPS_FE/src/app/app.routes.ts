import { Routes } from '@angular/router';
<<<<<<< HEAD
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';

export const routes: Routes = [
    {
        path:'login',
        component: LoginComponent           
    },
    {
        path:'register',
        component: RegisterComponent
    },
    


=======
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
>>>>>>> b3dd57de739abd56c6bac2df61d901f152ee0683
];
