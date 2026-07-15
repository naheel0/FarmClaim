using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmClaim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherAndAIAnalysisToClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AIAnalysisResult",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeatherSnapshot",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AIAnalysisResult",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "WeatherSnapshot",
                table: "Claims");
        }
    }
}
