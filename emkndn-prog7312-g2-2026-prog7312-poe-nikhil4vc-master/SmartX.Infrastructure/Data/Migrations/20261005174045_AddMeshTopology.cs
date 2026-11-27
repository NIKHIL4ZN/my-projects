using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartX.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeshTopology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeshNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SensorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MacAddress = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    DeploymentLocation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsGateway = table.Column<bool>(type: "bit", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeshNodes", x => x.Id);
                    table.CheckConstraint("CK_MeshNodes_NodeRole", "([IsGateway] = 1 AND [SensorId] IS NULL) OR ([IsGateway] = 0 AND [SensorId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_MeshNodes_Sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeshConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecondNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnergyCost = table.Column<double>(type: "float", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeshConnections", x => x.Id);
                    table.CheckConstraint("CK_MeshConnections_EndpointOrder", "[FirstNodeId] < [SecondNodeId]");
                    table.CheckConstraint("CK_MeshConnections_PositiveCost", "[EnergyCost] > 0");
                    table.ForeignKey(
                        name: "FK_MeshConnections_MeshNodes_FirstNodeId",
                        column: x => x.FirstNodeId,
                        principalTable: "MeshNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeshConnections_MeshNodes_SecondNodeId",
                        column: x => x.SecondNodeId,
                        principalTable: "MeshNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeshConnections_FirstNodeId_SecondNodeId",
                table: "MeshConnections",
                columns: new[] { "FirstNodeId", "SecondNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeshConnections_SecondNodeId",
                table: "MeshConnections",
                column: "SecondNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MeshNodes_MacAddress",
                table: "MeshNodes",
                column: "MacAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeshNodes_SensorId",
                table: "MeshNodes",
                column: "SensorId",
                unique: true,
                filter: "[SensorId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeshConnections");

            migrationBuilder.DropTable(
                name: "MeshNodes");
        }
    }
}
