-- Release 2026.11: stock import and reports. The DBA runs this file with psql before the release.

CREATE TABLE stock_levels (
    id serial PRIMARY KEY,
    sku varchar(32) NOT NULL,
    available integer NOT NULL,
    updated_at timestamp NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_stock_levels_sku ON stock_levels (sku);

CREATE TABLE discontinued_skus (
    sku varchar(32),
    reason text
);

CREATE TABLE stock_imports (
    file_name text NOT NULL,
    row_count integer NOT NULL,
    imported_at timestamptz NOT NULL
);

-- Each order line gets a batch id for the recall report.
ALTER TABLE order_lines ADD COLUMN batch_id uuid NOT NULL DEFAULT gen_random_uuid();
CREATE INDEX ix_order_lines_sku ON order_lines ("Sku");
