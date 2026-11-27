import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';

import { CompareList } from '../../compare/compare-list';
import { Product } from '../product';
import { ProductService } from '../product.service';

@Component({
  selector: 'app-search-result',
  imports: [CurrencyPipe],
  templateUrl: './search-result.html',
  styleUrl: './search-result.css',
})
export class SearchResult {
  private readonly products = inject(ProductService);
  protected readonly compare = inject(CompareList);

  protected readonly rows = signal<Product[]>([]);
  protected readonly loaded = signal(false);
  protected readonly searchValue = signal<string | undefined>(undefined);

  constructor() {
    inject(ActivatedRoute)
      .queryParamMap.pipe(takeUntilDestroyed())
      .subscribe((params) => this.load(params.get('q') ?? ''));
  }

  private load(query: string): void {
    const term = query.trim();
    // The search term is plain text. Descriptions go through the Angular sanitizer in the template,
    // which keeps formatting tags and removes scripts and event handlers.
    this.searchValue.set(term || undefined);
    this.products.search(term).subscribe((products) => {
      this.rows.set(products);
      this.loaded.set(true);
    });
  }
}
