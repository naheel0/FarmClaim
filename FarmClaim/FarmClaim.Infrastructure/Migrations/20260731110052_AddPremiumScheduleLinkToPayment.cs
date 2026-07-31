using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmClaim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumScheduleLinkToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PremiumScheduleId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PremiumScheduleId",
                table: "Payments",
                column: "PremiumScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PremiumSchedules_PremiumScheduleId",
                table: "Payments",
                column: "PremiumScheduleId",
                principalTable: "PremiumSchedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PremiumSchedules_PremiumScheduleId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PremiumScheduleId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PremiumScheduleId",
                table: "Payments");
        }
    }
}
