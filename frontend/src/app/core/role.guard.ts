import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { Role } from './models';

export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  const allowed = route.data?.['roles'] as Role[] | undefined;
  if (allowed && !auth.hasRole(...allowed)) {
    return router.createUrlTree([auth.homeRoute()]);
  }

  return true;
};
