import { Injectable, computed, signal } from '@angular/core';

const STORAGE_KEY = 'kiosk.compare';
const MAX_ITEMS = 4;

/** The products that the customer picked to compare. Kept for the tab, so a reload keeps the choice. */
@Injectable({ providedIn: 'root' })
export class CompareList {
  private readonly ids = signal<number[]>(CompareList.restore());

  readonly productIds = this.ids.asReadonly();
  readonly count = computed(() => this.ids().length);
  readonly full = computed(() => this.ids().length >= MAX_ITEMS);

  has(id: number): boolean {
    return this.ids().includes(id);
  }

  toggle(id: number): void {
    const next = this.has(id) ? this.ids().filter((x) => x !== id) : this.full() ? this.ids() : [...this.ids(), id];
    this.ids.set(next);
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  }

  clear(): void {
    this.ids.set([]);
    sessionStorage.removeItem(STORAGE_KEY);
  }

  private static restore(): number[] {
    try {
      const value: unknown = JSON.parse(sessionStorage.getItem(STORAGE_KEY) ?? '[]');
      return Array.isArray(value) ? value.filter((x): x is number => Number.isInteger(x)).slice(0, MAX_ITEMS) : [];
    } catch {
      return [];
    }
  }
}
