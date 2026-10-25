using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpressAlertsAndSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // orders and order_lines get writes all day: fail after 5 seconds instead of queueing traffic behind a lock.
            migrationBuilder.Sql("SET lock_timeout = '5s';");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:ci", "und-u-ks-level1,und-u-ks-level1,icu,False");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "ci",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<bool>(
                name: "Express",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "back_in_stock_alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    Sku = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_back_in_stock_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_back_in_stock_alerts_products_Sku",
                        column: x => x.Sku,
                        principalTable: "products",
                        principalColumn: "Sku",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_picking_queue",
                table: "orders",
                columns: new[] { "Express", "PlacedAt", "Id" },
                descending: new[] { true, false, false },
                filter: "\"Status\" = 'Placed'")
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_order_lines_quantity",
                table: "order_lines",
                sql: "\"Quantity\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_back_in_stock_alerts_Sku_CustomerId",
                table: "back_in_stock_alerts",
                columns: new[] { "Sku", "CustomerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "back_in_stock_alerts");

            migrationBuilder.DropIndex(
                name: "IX_orders_picking_queue",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_order_lines_quantity",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "Express",
                table: "orders");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:CollationDefinition:ci", "und-u-ks-level1,und-u-ks-level1,icu,False");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "ci");
        }
    }
}
