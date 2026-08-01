using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmClaim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PremiumSchedules_Payments_PaymentId",
                table: "PremiumSchedules");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PremiumSchedules",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EventId",
                table: "WebhookEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_OrderId",
                table: "WebhookEvents",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_PaymentId",
                table: "WebhookEvents",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_ProcessedAt",
                table: "WebhookEvents",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PremiumSchedules_DueDate",
                table: "PremiumSchedules",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_PremiumSchedules_PolicyId_Status_InstallmentNumber",
                table: "PremiumSchedules",
                columns: new[] { "PolicyId", "Status", "InstallmentNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PremiumSchedules_Status",
                table: "PremiumSchedules",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_PremiumSchedules_Payments_PaymentId",
                table: "PremiumSchedules",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PremiumSchedules_Payments_PaymentId",
                table: "PremiumSchedules");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_EventId",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_OrderId",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_PaymentId",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_ProcessedAt",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_PremiumSchedules_DueDate",
                table: "PremiumSchedules");

            migrationBuilder.DropIndex(
                name: "IX_PremiumSchedules_PolicyId_Status_InstallmentNumber",
                table: "PremiumSchedules");

            migrationBuilder.DropIndex(
                name: "IX_PremiumSchedules_Status",
                table: "PremiumSchedules");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "PremiumSchedules",
                type: "int",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");

            migrationBuilder.AddForeignKey(
                name: "FK_PremiumSchedules_Payments_PaymentId",
                table: "PremiumSchedules",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id");
        }
    }
}
