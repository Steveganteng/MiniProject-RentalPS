import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CustomerNavbarComponent } from '../customer-navbar/customer-navbar.component';

@Component({
  selector: 'app-customer-layout',
  standalone: true,
  imports: [CustomerNavbarComponent, RouterOutlet],
  template: `
    <app-customer-navbar class="sticky-top"></app-customer-navbar>
    <router-outlet></router-outlet>
  `
})
export class CustomerLayoutComponent {}
