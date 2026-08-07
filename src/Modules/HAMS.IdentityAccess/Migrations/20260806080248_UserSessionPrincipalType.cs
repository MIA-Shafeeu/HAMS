using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.IdentityAccess.Migrations
{
    /// <inheritdoc />
    public partial class UserSessionPrincipalType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGuardian",
                schema: "identity",
                table: "UserSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsStaff",
                schema: "identity",
                table: "UserSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsStudent",
                schema: "identity",
                table: "UserSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGuardian",
                schema: "identity",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "IsStaff",
                schema: "identity",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "IsStudent",
                schema: "identity",
                table: "UserSessions");
        }
    }
}
