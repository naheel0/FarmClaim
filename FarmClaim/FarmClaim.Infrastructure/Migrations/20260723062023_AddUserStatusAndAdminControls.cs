using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmClaim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatusAndAdminControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "StatusChangeReason",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StatusChangedByUserId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InsurancePlanId",
                table: "InsurancePolicies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InsurancePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CropType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PremiumRatePerHectare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SumInsuredPerHectare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CoveragePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MinAreaInHectares = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxAreaInHectares = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PolicyDurationMonths = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StatusChangedByUserId",
                table: "Users",
                column: "StatusChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_InsurancePlanId",
                table: "InsurancePolicies",
                column: "InsurancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_CropType",
                table: "InsurancePlans",
                column: "CropType");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_IsActive",
                table: "InsurancePlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlans_Name",
                table: "InsurancePlans",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_InsurancePolicies_InsurancePlans_InsurancePlanId",
                table: "InsurancePolicies",
                column: "InsurancePlanId",
                principalTable: "InsurancePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_StatusChangedByUserId",
                table: "Users",
                column: "StatusChangedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsurancePolicies_InsurancePlans_InsurancePlanId",
                table: "InsurancePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_StatusChangedByUserId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "InsurancePlans");

            migrationBuilder.DropIndex(
                name: "IX_Users_Status",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_StatusChangedByUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_InsurancePlanId",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StatusChangeReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StatusChangedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InsurancePlanId",
                table: "InsurancePolicies");
        }
    }
}
