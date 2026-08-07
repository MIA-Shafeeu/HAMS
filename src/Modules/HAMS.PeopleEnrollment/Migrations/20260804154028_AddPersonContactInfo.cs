using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.PeopleEnrollment.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonContactInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "people",
                table: "People",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                schema: "people",
                table: "People",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                schema: "people",
                table: "People");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                schema: "people",
                table: "People");
        }
    }
}
