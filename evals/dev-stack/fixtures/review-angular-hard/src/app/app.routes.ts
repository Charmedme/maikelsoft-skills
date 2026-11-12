import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  {
    path: 'products',
    loadChildren: () => import('./products/products.routes').then((m) => m.PRODUCTS_ROUTES),
  },
  {
    path: 'price-changes',
    loadChildren: () => import('./price-changes/price-changes.routes').then((m) => m.PRICE_CHANGES_ROUTES),
  },
  { path: '**', redirectTo: 'products' },
];
