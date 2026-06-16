using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeePayroll.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandInvoiceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GstAmount",
                table: "Invoices",
                newName: "TransportCharges");

            migrationBuilder.AddColumn<decimal>(
                name: "BasicTotal",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CgstAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IgstAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasicTotal",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CgstAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IgstAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SgstAmount",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "TransportCharges",
                table: "Invoices",
                newName: "GstAmount");
        }
    }
}
