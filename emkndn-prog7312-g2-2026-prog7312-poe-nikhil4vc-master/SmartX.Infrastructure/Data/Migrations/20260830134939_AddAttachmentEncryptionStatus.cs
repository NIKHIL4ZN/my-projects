using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartX.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentEncryptionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEncrypted",
                table: "SensorAttachments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEncrypted",
                table: "SensorAttachments");
        }
    }
}
