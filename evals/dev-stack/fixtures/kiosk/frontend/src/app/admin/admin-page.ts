import { CurrencyPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';

interface UserSummary {
  id: number;
  email: string;
  role: string;
  displayName: string | null;
}

interface ShopSummary {
  customers: number;
  orders: number;
  revenue: number;
}

@Component({
  selector: 'app-admin-page',
  imports: [CurrencyPipe],
  template: `
    <h1>Administration</h1>
    @if (summary(); as s) {
      <p class="summary">{{ s.customers }} customers, {{ s.orders }} orders, {{ s.revenue | currency: 'EUR' }} revenue</p>
    }
    <table>
      <thead><tr><th>Email</th><th>Role</th></tr></thead>
      <tbody>
        @for (user of users(); track user.id) {
          <tr><td>{{ user.email }}</td><td>{{ user.role }}</td></tr>
        }
      </tbody>
    </table>
  `,
})
export class AdminPage {
  private readonly http = inject(HttpClient);
  protected readonly users = toSignal(this.http.get<UserSummary[]>('/api/admin/users'), { initialValue: [] });
  protected readonly summary = toSignal(this.http.get<ShopSummary>('/api/metrics/summary'));
}
