import { AsyncPipe, CurrencyPipe } from '@angular/common';
import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';

import { Product } from '../product';
import { ProductsService } from '../products.service';

@Component({
  selector: 'app-product-list',
  imports: [AsyncPipe, CurrencyPipe, RouterLink, TranslocoDirective],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css',
})
export class ProductList {
  private readonly productsService = inject(ProductsService);
  private readonly transloco = inject(TranslocoService);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('deleteDialog');
  private readonly heading = viewChild.required<ElementRef<HTMLHeadingElement>>('heading');

  protected readonly products$ = this.productsService.list();
  protected readonly pending = signal<Product | null>(null);
  protected readonly deleting = signal(false);
  protected readonly status = signal('');
  protected readonly failed = signal(false);
  private opener: HTMLElement | null = null;

  protected askDelete(product: Product, button: HTMLElement): void {
    this.opener = button;
    this.pending.set(product);
    this.dialog().nativeElement.showModal();
  }

  protected cancelDelete(): void {
    this.dialog().nativeElement.close();
  }

  protected onDialogClose(): void {
    if (!this.deleting()) {
      this.pending.set(null);
      this.opener?.focus();
    }
  }

  protected confirmDelete(): void {
    const product = this.pending();
    if (!product || this.deleting()) {
      return;
    }
    this.deleting.set(true);
    this.productsService.delete(product.id).subscribe({
      next: () => {
        this.finish();
        this.failed.set(false);
        this.status.set(this.transloco.translate('products.list.deleted', { name: product.name }));
        this.heading().nativeElement.focus();
      },
      error: () => {
        this.finish();
        this.failed.set(true);
        this.status.set(this.transloco.translate('products.list.deleteFailed', { name: product.name }));
        this.opener?.focus();
      },
    });
  }

  private finish(): void {
    this.dialog().nativeElement.close();
    this.deleting.set(false);
    this.pending.set(null);
  }
}
