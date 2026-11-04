import { Routes } from '@angular/router';

export const PRICE_CHANGES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./price-change-page').then((m) => m.PriceChangePage),
  },
  {
    path: 'for/:productId',
    loadComponent: () => import('./price-change-page').then((m) => m.PriceChangePage),
  },
  {
    path: ':changeId',
    loadComponent: () => import('./price-change-done').then((m) => m.PriceChangeDone),
  },
];
