using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmClaim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallmentSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentInstallmentNumber",
                table: "InsurancePolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InstallmentAmount",
                table: "InsurancePolicies",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextInstallmentDueDate",
                table: "InsurancePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "InsurancePlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentFrequency",
                table: "InsurancePlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsInstallments",
                table: "InsurancePlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PremiumSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AmountDue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PremiumSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PremiumSchedules_InsurancePolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "InsurancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PremiumSchedules_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PremiumSchedules_PaymentId",
                table: "PremiumSchedules",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PremiumSchedules_PolicyId",
                table: "PremiumSchedules",
                column: "PolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PremiumSchedules");

            migrationBuilder.DropColumn(
                name: "CurrentInstallmentNumber",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "InstallmentAmount",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "NextInstallmentDueDate",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "InstallmentFrequency",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "SupportsInstallments",
                table: "InsurancePlans");
        }
    }
}
