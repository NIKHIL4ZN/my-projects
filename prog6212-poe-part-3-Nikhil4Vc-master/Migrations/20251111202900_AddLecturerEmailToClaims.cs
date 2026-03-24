using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonthlyClaims_System.Migrations
{
    /// <inheritdoc />
    public partial class AddLecturerEmailToClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LecturerEmail",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LecturerEmail",
                table: "Claims");
        }
    }
}
