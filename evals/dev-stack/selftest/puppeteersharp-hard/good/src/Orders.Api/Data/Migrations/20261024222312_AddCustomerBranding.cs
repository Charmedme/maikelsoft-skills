using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandingFooter",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "BrandingLogo",
                table: "customers",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandingLogoContentType",
                table: "customers",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandingLogoUrl",
                table: "customers",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrandingFooter",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "BrandingLogo",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "BrandingLogoContentType",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "BrandingLogoUrl",
                table: "customers");
        }
    }
}
