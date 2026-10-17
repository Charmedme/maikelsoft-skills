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
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "orders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_Sku",
                table: "order_lines",
                column: "Sku")
                .Annotation("Npgsql:CreatedConcurrently", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_order_lines_Sku",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "orders");
        }
    }
}
