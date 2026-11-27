import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

/**
 * Opens the admin pages only when the server confirms the admin role. The server-side policy on the
 * admin API stays the real protection; this guard only keeps other users away from an empty page.
 */
export const adminGuard: CanActivateFn = () => {
  const router = inject(Router);
  return inject(HttpClient)
    .get<{ role: string }>('/api/users/me')
    .pipe(
      map((me) => (me.role === 'admin' ? true : router.parseUrl('/login'))),
      catchError(() => of(router.parseUrl('/login'))),
    );
};
