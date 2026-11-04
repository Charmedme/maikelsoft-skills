import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { PriceChange } from './price-change';

@Component({
  selector: 'app-price-change-done',
  imports: [CurrencyPipe, RouterLink, TranslocoDirective],
  templateUrl: './price-change-done.html',
})
export class PriceChangeDone {
  private readonly params = toSignal(inject(ActivatedRoute).paramMap);

  protected readonly changeId = computed(() => this.params()?.get('changeId'));
  protected readonly change = httpResource<PriceChange>(() =>
    this.changeId() ? `/api/price-changes/${this.changeId()}` : undefined,
  );
}
