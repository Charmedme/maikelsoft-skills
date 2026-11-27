import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

/**
 * Opens the admin pages for the admin role. The role comes from the token that the browser has, so the
 * menu opens without a call to /api/users/me on each navigation.
 */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAdmin() ? true : inject(Router).parseUrl('/login');
};
