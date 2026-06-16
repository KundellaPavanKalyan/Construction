using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeePayroll.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImpressWeeklyAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImpressWeeklyAmounts",
                columns: table => new
                {
                    ImpressWeeklyAmountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollId = table.Column<int>(type: "int", nullable: false),
                    Week1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Week2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Week3 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Week4 = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpressWeeklyAmounts", x => x.ImpressWeeklyAmountId);
                    table.ForeignKey(
                        name: "FK_ImpressWeeklyAmounts_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "PayrollId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImpressWeeklyAmounts_PayrollId",
                table: "ImpressWeeklyAmounts",
                column: "PayrollId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImpressWeeklyAmounts");
        }
    }
}
