import { Routes } from '@angular/router';

import { adminGuard } from './auth/admin.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'search' },
  { path: 'search', loadComponent: () => import('./products/search-result/search-result').then((m) => m.SearchResult) },
  { path: 'login', loadComponent: () => import('./auth/login/login').then((m) => m.Login) },
  { path: 'compare', loadComponent: () => import('./compare/compare-page').then((m) => m.ComparePage) },
  { path: 'about', loadComponent: () => import('./about/promo-video').then((m) => m.PromoVideo) },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./admin/admin-page').then((m) => m.AdminPage),
  },
];
