using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZatcaInvoicingApp.Migrations
{
    /// <inheritdoc />
    public partial class wgs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QRCode",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QRCode",
                table: "Invoices");
        }
    }
}
