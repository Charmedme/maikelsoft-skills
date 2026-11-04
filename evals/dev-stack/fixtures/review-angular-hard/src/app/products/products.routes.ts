import { Routes } from '@angular/router';

export const PRODUCTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./product-list/product-list').then((m) => m.ProductList),
  },
  {
    path: 'new',
    loadComponent: () => import('./product-create/product-create').then((m) => m.ProductCreate),
  },
];
