import { Product } from '../products/product';

export interface PriceLine {
  productId: number;
  name: string;
  oldPrice: number;
  newPrice: number;
}

export interface PriceChange {
  id: number;
  productIds: number[];
  percentage: number;
  /** Date without a time, in the form yyyy-MM-dd. */
  effectiveDate: string;
  lines: PriceLine[];
}

export type SelectedProduct = Pick<Product, 'id' | 'name' | 'code' | 'price'>;
