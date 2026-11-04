import { AsyncPipe, CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { Subject, debounceTime, distinctUntilChanged, mergeMap, of } from 'rxjs';

import { AuthService } from '../../auth/auth.service';
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
  protected readonly auth = inject(AuthService);

  protected readonly products$ = this.productsService.list();

  private readonly searchTerms = new Subject<string>();
  protected readonly term = signal('');
  protected readonly suggestions = toSignal(
    this.searchTerms.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      mergeMap((term) => (term.length < 2 ? of([]) : this.productsService.search(term))),
    ),
    { initialValue: [] },
  );
  protected readonly suggestionStatus = computed(() =>
    this.term().length < 2 ? '' : this.transloco.translate('products.search.count', { count: this.suggestions().length }),
  );

  protected onSearch(value: string): void {
    this.term.set(value);
    this.searchTerms.next(value.trim());
  }
}
