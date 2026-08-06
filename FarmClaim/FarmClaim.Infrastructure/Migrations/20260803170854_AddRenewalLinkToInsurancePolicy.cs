using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmClaim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRenewalLinkToInsurancePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RenewedFromPolicyId",
                table: "InsurancePolicies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_RenewedFromPolicyId",
                table: "InsurancePolicies",
                column: "RenewedFromPolicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_InsurancePolicies_InsurancePolicies_RenewedFromPolicyId",
                table: "InsurancePolicies",
                column: "RenewedFromPolicyId",
                principalTable: "InsurancePolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsurancePolicies_InsurancePolicies_RenewedFromPolicyId",
                table: "InsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_InsurancePolicies_RenewedFromPolicyId",
                table: "InsurancePolicies");

            migrationBuilder.DropColumn(
                name: "RenewedFromPolicyId",
                table: "InsurancePolicies");
        }
    }
}
