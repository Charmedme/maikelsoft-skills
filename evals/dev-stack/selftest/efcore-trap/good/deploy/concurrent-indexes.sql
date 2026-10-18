-- Run after the idempotent migration script, outside a transaction (psql without --single-transaction).
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_order_lines_Sku" ON order_lines ("Sku");
