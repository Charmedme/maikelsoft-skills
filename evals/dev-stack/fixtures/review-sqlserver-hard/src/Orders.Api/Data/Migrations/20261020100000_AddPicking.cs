using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPicking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DATA-26: readers (the picks report, the claim of the next task) do not wait for writers.
            // ALTER DATABASE cannot run in a transaction, and it needs a moment without other sessions.
            migrationBuilder.Sql(
                "ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;",
                suppressTransaction: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "StockItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWSEQUENTIALID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "PickTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Zone = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PickerId = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    TakenAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    PickedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickTasks_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PickZones",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Zone = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickZones", x => x.Sku);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_OrderId",
                table: "PickTasks",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_Zone_Status_CreatedAt",
                table: "PickTasks",
                columns: new[] { "Zone", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickTasks");

            migrationBuilder.DropTable(
                name: "PickZones");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "StockItems",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWSEQUENTIALID()");
        }
    }
}
