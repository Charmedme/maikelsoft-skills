import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, input, signal } from '@angular/core';
import { DomSanitizer, SafeHtml, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { Product } from '../products/product';
import { ProductService } from '../products/product.service';
import { CompareList } from './compare-list';

interface CompareRow {
  id: number;
  name: string;
  price: number;
  imageUrl: string | null;
  details: SafeHtml;
}

@Component({
  selector: 'app-compare-page',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './compare-page.html',
  styleUrl: './compare-page.css',
})
export class ComparePage {
  private readonly sanitizer = inject(DomSanitizer);
  private readonly products = inject(ProductService);
  private readonly list = inject(CompareList);
  private readonly title = inject(Title);

  /** Product IDs from a shared link (?ids=1,2,3). Without them, the page shows the own compare list. */
  readonly ids = input<string>();
  /** Headline of a shared comparison. Marketing sends links with a headline that has bold or italic words. */
  readonly headline = input<string>();

  protected readonly rows = signal<CompareRow[]>([]);
  protected readonly heading = computed(() => {
    const text = this.headline();
    return text ? this.sanitizer.bypassSecurityTrustHtml(text) : undefined;
  });

  constructor() {
    queueMicrotask(() => this.load());
  }

  protected remove(id: number): void {
    this.list.toggle(id);
    this.rows.update((rows) => rows.filter((r) => r.id !== id));
  }

  private load(): void {
    const shared = (this.ids() ?? '').split(',').map(Number).filter(Number.isInteger).filter((n) => n > 0);
    const ids = shared.length > 0 ? shared.slice(0, 4) : this.list.productIds();
    this.title.setTitle(this.headline() ? `Compare: ${this.headline()}` : 'Compare products');
    if (ids.length === 0) {
      return;
    }

    forkJoin(ids.map((id) => this.products.byId(id))).subscribe((products) => this.rows.set(products.map((p) => this.toRow(p))));
  }

  // The descriptions come from the spec sheets of the makers and hold lists and tables.
  private toRow(product: Product): CompareRow {
    return {
      id: product.id,
      name: product.name,
      price: product.price,
      imageUrl: product.imageUrl,
      details: this.sanitizer.bypassSecurityTrustHtml(product.description),
    };
  }
}
