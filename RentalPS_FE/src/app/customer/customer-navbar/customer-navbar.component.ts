import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-customer-navbar',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './customer-navbar.component.html',
  styleUrl: './customer-navbar.component.css'
})
export class CustomerNavbarComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly userName = this.extractUserFromToken();

  private extractUserFromToken(): string {
    const token = this.authService.getToken();
    if (!token) return 'Customer';

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
        payload['name'] ||
        'Customer';
    } catch {
      return 'Customer';
    }
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
