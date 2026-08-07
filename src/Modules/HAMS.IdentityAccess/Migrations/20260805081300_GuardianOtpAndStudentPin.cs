using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.IdentityAccess.Migrations
{
    /// <inheritdoc />
    public partial class GuardianOtpAndStudentPin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuardianOtpChallenges",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    ConsumedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardianOtpChallenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuardianOtpChallenges_PhoneNumber",
                schema: "identity",
                table: "GuardianOtpChallenges",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuardianOtpChallenges",
                schema: "identity");
        }
    }
}
