using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeePayroll.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialTrackingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "Invoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialInvoiceItems",
                columns: table => new
                {
                    MaterialInvoiceItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    MaterialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialInvoiceItems", x => x.MaterialInvoiceItemId);
                    table.ForeignKey(
                        name: "FK_MaterialInvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackingProjects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingProjects", x => x.ProjectId);
                });

            migrationBuilder.CreateTable(
                name: "TrackingVendors",
                columns: table => new
                {
                    VendorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingVendors", x => x.VendorId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialInvoiceItems_InvoiceId_MaterialName",
                table: "MaterialInvoiceItems",
                columns: new[] { "InvoiceId", "MaterialName" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingProjects_ProjectName",
                table: "TrackingProjects",
                column: "ProjectName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackingVendors_VendorName",
                table: "TrackingVendors",
                column: "VendorName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialInvoiceItems");

            migrationBuilder.DropTable(
                name: "TrackingProjects");

            migrationBuilder.DropTable(
                name: "TrackingVendors");

            migrationBuilder.DropColumn(
                name: "ProjectName",
                table: "Invoices");
        }
    }
}
