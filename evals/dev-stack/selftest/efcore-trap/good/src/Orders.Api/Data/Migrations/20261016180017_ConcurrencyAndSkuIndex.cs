using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConcurrencyAndSkuIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "orders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            // IX_order_lines_Sku is built with CREATE INDEX CONCURRENTLY in deploy/concurrent-indexes.sql.
            // The idempotent script wraps each statement in a DO block, and PostgreSQL cannot run CONCURRENTLY there.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_order_lines_Sku\";");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "orders");
        }
    }
}
