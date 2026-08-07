using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.OrgCurriculum.Migrations
{
    /// <inheritdoc />
    public partial class SchoolCalendarAndHolidays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HolidayTypes",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkingDays",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkingDays_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "org",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    HolidayTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameDv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holidays_HolidayTypes_HolidayTypeId",
                        column: x => x.HolidayTypeId,
                        principalSchema: "org",
                        principalTable: "HolidayTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Holidays_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "org",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "org",
                table: "HolidayTypes",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0014-000000000001"), "PUBLIC_HOLIDAY", 1, true, "Public Holiday" },
                    { new Guid("00000000-0000-0000-0014-000000000002"), "RELIGIOUS_HOLIDAY", 2, true, "Religious Holiday" },
                    { new Guid("00000000-0000-0000-0014-000000000003"), "SCHOOL_DECLARED", 3, true, "School-Declared Holiday" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_HolidayTypeId",
                schema: "org",
                table: "Holidays",
                column: "HolidayTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_SchoolId_Date",
                schema: "org",
                table: "Holidays",
                columns: new[] { "SchoolId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HolidayTypes_Code",
                schema: "org",
                table: "HolidayTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkingDays_SchoolId_DayOfWeek",
                schema: "org",
                table: "WorkingDays",
                columns: new[] { "SchoolId", "DayOfWeek" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holidays",
                schema: "org");

            migrationBuilder.DropTable(
                name: "WorkingDays",
                schema: "org");

            migrationBuilder.DropTable(
                name: "HolidayTypes",
                schema: "org");
        }
    }
}
