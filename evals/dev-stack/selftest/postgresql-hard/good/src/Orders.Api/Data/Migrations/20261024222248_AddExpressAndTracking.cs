using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpressAndTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // orders is a big table with writes all day. Fail fast instead of queueing all traffic behind a lock.
            migrationBuilder.Sql("SET lock_timeout = '5s';");

            // A constant default and a nullable column: no table rewrite.
            migrationBuilder.AddColumn<bool>(
                name: "Express",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            // Give the existing orders a code in batches of 5000, each in its own transaction,
            // so no long transaction holds row locks on orders. Same alphabet and length as TrackingCodes.New().
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    alphabet constant text := 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
                    updated int;
                BEGIN
                    LOOP
                        UPDATE orders AS o
                        SET "TrackingCode" = (
                            SELECT string_agg(substr(alphabet, 1 + get_byte(n.b, i) % 32, 1), '' ORDER BY i)
                            FROM unnest(ARRAY[0, 1, 2, 3, 4, 5, 10, 11, 12, 13, 14, 15]) AS i)
                        FROM (
                            -- 12 random bytes of a version 4 UUID (bytes 6 to 9 hold the version and the variant).
                            SELECT "Id", uuid_send(gen_random_uuid()) AS b
                            FROM orders
                            WHERE "TrackingCode" IS NULL
                            LIMIT 5000) AS n
                        WHERE o."Id" = n."Id";
                        GET DIAGNOSTICS updated = ROW_COUNT;
                        EXIT WHEN updated = 0;
                        COMMIT;
                    END LOOP;
                END $$;
                """,
                suppressTransaction: true);

            // Built without blocking writes (outside a transaction).
            migrationBuilder.CreateIndex(
                name: "IX_orders_picking_queue",
                table: "orders",
                columns: new[] { "Express", "PlacedAt", "Id" },
                descending: new[] { true, false, false },
                filter: "\"Status\" = 'Placed'")
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_TrackingCode",
                table: "orders",
                column: "TrackingCode",
                unique: true)
                .Annotation("Npgsql:CreatedConcurrently", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_picking_queue",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_TrackingCode",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Express",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "orders");
        }
    }
}
