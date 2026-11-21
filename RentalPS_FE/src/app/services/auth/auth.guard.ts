import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getToken();
  const role = authService.getRole()?.toLowerCase();

  if (!token) {
    authService.logout();
    return router.createUrlTree(['/login']);
  }

  const requiredRole = (route.data['role'] as string | undefined)?.toLowerCase();

  if (requiredRole && role !== requiredRole) {
    if (role === 'admin') {
      return router.createUrlTree(['/admin/dashboard']);
    }

    if (role === 'customer') {
      return router.createUrlTree(['/customer/dashboard']);
    }

    authService.logout();
    return router.createUrlTree(['/login']);
  }

  return true;
};
