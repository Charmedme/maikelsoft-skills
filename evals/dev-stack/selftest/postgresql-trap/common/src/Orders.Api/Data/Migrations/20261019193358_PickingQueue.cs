using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PickingQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop instead of queueing behind a long transaction on the live orders table (DATA-25).
            migrationBuilder.Sql("SET lock_timeout = '5s';");

            migrationBuilder.AddColumn<string>(
                name: "PickedBy",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PickingStartedAt",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_picking_queue",
                table: "orders",
                columns: new[] { "PlacedAt", "Id" },
                filter: "\"Status\" = 'Placed'")
                .Annotation("Npgsql:CreatedConcurrently", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_picking_queue",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PickedBy",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PickingStartedAt",
                table: "orders");
        }
    }
}
