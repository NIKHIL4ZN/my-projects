using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartX.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryPacketMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "TelemetryReadings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "TelemetryReadings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryReadings_SensorId_SequenceNumber",
                table: "TelemetryReadings",
                columns: new[] { "SensorId", "SequenceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TelemetryReadings_SensorId_SequenceNumber",
                table: "TelemetryReadings");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "TelemetryReadings");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "TelemetryReadings");
        }
    }
}
