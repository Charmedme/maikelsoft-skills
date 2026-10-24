using System;
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
            migrationBuilder.AddColumn<bool>(
                name: "Express",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TrackingCode",
                table: "orders",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateIndex(
                name: "IX_orders_Express_PlacedAt",
                table: "orders",
                columns: new[] { "Express", "PlacedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_TrackingCode",
                table: "orders",
                column: "TrackingCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_Express_PlacedAt",
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
