import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Smart root guard: redirects to /home if the user has an active session,
 * or to /login if no session exists in localStorage.
 */
export const rootGuard: CanActivateFn = () => {
  const router = inject(Router);
  const authService = inject(AuthService);

  const user = authService.currentUser();
  if (user) {
    const role: any = user.role;
    // Handle both numeric and string-based Enum values
    if (role === 2 || role === 'Admin') return router.createUrlTree(['/admin']);
    if (role === 1 || role === 'Operator') return router.createUrlTree(['/operator']);
    return router.createUrlTree(['/home']);
  }

  return router.createUrlTree(['/login']);
};
