using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonthlyClaims_System.Migrations
{
    /// <inheritdoc />
    public partial class MakeFileFieldsOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlobFileName",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlobFileName",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Claims");
        }
    }
}
