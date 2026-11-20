import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();

  if (!token) {
    return router.createUrlTree(['/login']);
  }

  const requiredRole = route.data['role'] as string | undefined;
  const role = authService.getRole();

  if (requiredRole && role?.toLowerCase() !== requiredRole.toLowerCase()) {
    if (role?.toLowerCase() === 'admin') {
      return router.createUrlTree(['/admin/dashboard']);
    }

    if (role?.toLowerCase() === 'customer') {
      return router.createUrlTree(['/customer/dashboard']);
    }

    authService.logout();
    return router.createUrlTree(['/login']);
  }

  return true;
};
