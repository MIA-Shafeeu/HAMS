using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.CommunicationPortals.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommunicationPortals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "portals");

            migrationBuilder.CreateTable(
                name: "GuardianAcknowledgements",
                schema: "portals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuardianPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardianAcknowledgements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuardianAcknowledgements_GuardianPersonId_StudentPersonId_EntityType_EntityId",
                schema: "portals",
                table: "GuardianAcknowledgements",
                columns: new[] { "GuardianPersonId", "StudentPersonId", "EntityType", "EntityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuardianAcknowledgements",
                schema: "portals");
        }
    }
}
