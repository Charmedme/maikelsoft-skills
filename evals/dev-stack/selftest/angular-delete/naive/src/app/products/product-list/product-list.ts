import { AsyncPipe, CurrencyPipe, NgIf } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { Product } from '../product';
import { ProductsService } from '../products.service';

@Component({
  selector: 'app-product-list',
  imports: [AsyncPipe, CurrencyPipe, NgIf, RouterLink, TranslocoDirective],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css',
})
export class ProductList {
  private readonly productsService = inject(ProductsService);

  protected readonly products$ = this.productsService.list();
  protected toDelete: Product | null = null;
  protected message = '';

  protected askDelete(product: Product): void {
    this.toDelete = product;
  }

  protected cancel(): void {
    this.toDelete = null;
  }

  protected confirm(): void {
    if (!this.toDelete) {
      return;
    }
    const name = this.toDelete.name;
    this.productsService.delete(this.toDelete.id).subscribe(() => (this.message = `${name} was deleted.`));
    this.toDelete = null;
  }
}
