import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const payload = auth.payload();
  const valid = payload?.exp === undefined || payload.exp * 1000 > Date.now();
  return valid && auth.isAdmin() ? true : inject(Router).parseUrl('/login');
};
