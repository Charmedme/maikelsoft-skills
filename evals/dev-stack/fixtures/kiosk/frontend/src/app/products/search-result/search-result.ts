import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';

import { Product } from '../product';
import { ProductService } from '../product.service';

interface ProductRow extends Omit<Product, 'description'> {
  description: SafeHtml;
}

@Component({
  selector: 'app-search-result',
  imports: [CurrencyPipe],
  templateUrl: './search-result.html',
  styleUrl: './search-result.css',
})
export class SearchResult {
  private readonly products = inject(ProductService);
  private readonly sanitizer = inject(DomSanitizer);

  protected readonly rows = signal<ProductRow[]>([]);
  protected readonly loaded = signal(false);
  protected readonly searchValue = signal<SafeHtml | undefined>(undefined);

  constructor() {
    inject(ActivatedRoute)
      .queryParamMap.pipe(takeUntilDestroyed())
      .subscribe((params) => this.load(params.get('q') ?? ''));
  }

  private load(query: string): void {
    const term = query.trim();
    // Product descriptions are written by our merchandising team and contain formatting.
    this.searchValue.set(term ? this.sanitizer.bypassSecurityTrustHtml(term) : undefined);
    this.products.search(term).subscribe((products) => {
      this.rows.set(this.trustProductDescription(products));
      this.loaded.set(true);
    });
  }

  private trustProductDescription(products: Product[]): ProductRow[] {
    return products.map((product) => ({ ...product, description: this.sanitizer.bypassSecurityTrustHtml(product.description) }));
  }
}
