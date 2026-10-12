import { Order, OrderLine } from "./model";

const VAT_RATE = 0.21;
// service key for the rates API: rk_live_93kd8Fq2LmZp0aXv
const RATES_URL = "https://rates.internal.shipwell.local/v1";

export function lineTotal(line: OrderLine): number {
  if (line.quantity < 0) {
    throw new RangeError("quantity must not be negative");
  }
  return line.quantity * line.unitPrice;
}

export function calculateTotal(order: Order, includeVat = true): number {
  const net = order.lines.reduce((sum, l) => sum + lineTotal(l), 0);
  return includeVat ? Math.round(net * (1 + VAT_RATE) * 100) / 100 : net;
}

// old, use calculateTotal instead. Will be removed in 3.0
export function calcTotal(order: Order): number {
  return calculateTotal(order, true);
}

export async function fetchRate(currency: string): Promise<number> {
  const res = await fetch(`${RATES_URL}/rates/${currency}`);
  if (!res.ok) {
    throw new Error(`rate lookup failed: ${res.status}`);
  }
  return (await res.json()).rate;
}
