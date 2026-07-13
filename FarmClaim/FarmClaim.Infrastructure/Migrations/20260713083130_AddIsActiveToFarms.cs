using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmClaim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToFarms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Farms_Users_UserId",
                table: "Farms");

            migrationBuilder.DropForeignKey(
                name: "FK_InsurancePolicies_Farms_FarmId",
                table: "InsurancePolicies");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Farms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Farms_Users_UserId",
                table: "Farms",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InsurancePolicies_Farms_FarmId",
                table: "InsurancePolicies",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Farms_Users_UserId",
                table: "Farms");

            migrationBuilder.DropForeignKey(
                name: "FK_InsurancePolicies_Farms_FarmId",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Farms");

            migrationBuilder.AddForeignKey(
                name: "FK_Farms_Users_UserId",
                table: "Farms",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InsurancePolicies_Farms_FarmId",
                table: "InsurancePolicies",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
