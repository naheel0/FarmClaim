using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmClaim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyStatusAndClaimPaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "InsurancePolicies");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "InsurancePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "InsurancePolicies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "InsurancePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "InsurancePolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "InsurancePolicies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "InsurancePolicies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Claims",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "Claims",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_ApprovedByUserId",
                table: "InsurancePolicies",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_Status",
                table: "InsurancePolicies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ReviewedByUserId",
                table: "Claims",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status",
                table: "Claims",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_Users_ReviewedByUserId",
                table: "Claims",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InsurancePolicies_Users_ApprovedByUserId",
                table: "InsurancePolicies",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Claims_Users_ReviewedByUserId",
                table: "Claims");

            migrationBuilder.DropForeignKey(
                name: "FK_InsurancePolicies_Users_ApprovedByUserId",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_ApprovedByUserId",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_Status",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_Claims_ReviewedByUserId",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_Status",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Claims");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "InsurancePolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
