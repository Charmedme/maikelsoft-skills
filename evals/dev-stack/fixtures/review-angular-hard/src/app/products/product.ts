export interface Product {
  id: number;
  name: string;
  code: string;
  price: number;
  /** Date without a time, in the form yyyy-MM-dd. */
  availableFrom: string;
}

export type NewProduct = Omit<Product, 'id'>;
