import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getToken();
  const role = authService.getRole()?.toLowerCase();

  if (!token) {
    return true;
  }

  if (role === 'admin') {
    return router.createUrlTree(['/admin/dashboard']);
  }

  if (role === 'customer') {
    return router.createUrlTree(['/customer/dashboard']);
  }

  // Token/role lokal tidak valid atau tidak dikenali.
  authService.logout();
  return true;
};
