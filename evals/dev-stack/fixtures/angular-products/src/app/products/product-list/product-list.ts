import { AsyncPipe, CurrencyPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { ProductsService } from '../products.service';

@Component({
  selector: 'app-product-list',
  imports: [AsyncPipe, CurrencyPipe, RouterLink, TranslocoDirective],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css',
})
export class ProductList {
  private readonly productsService = inject(ProductsService);

  protected readonly products$ = this.productsService.list();
}
