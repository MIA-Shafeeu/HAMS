using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.PeopleEnrollment.Migrations
{
    /// <inheritdoc />
    public partial class AddCanViewInterventionUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanViewInterventionUpdates",
                schema: "people",
                table: "GuardianStudentRelationships",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanViewInterventionUpdates",
                schema: "people",
                table: "GuardianStudentRelationships");
        }
    }
}
