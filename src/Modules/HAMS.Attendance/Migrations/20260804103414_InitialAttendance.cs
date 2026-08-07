using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.Attendance.Migrations
{
    /// <inheritdoc />
    public partial class InitialAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "attendance");

            migrationBuilder.CreateTable(
                name: "AttendanceStatuses",
                schema: "attendance",
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
                    table.PrimaryKey("PK_AttendanceStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyAttendanceRecords",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyAttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyAttendanceRecords_AttendanceStatuses_AttendanceStatusId",
                        column: x => x.AttendanceStatusId,
                        principalSchema: "attendance",
                        principalTable: "AttendanceStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonAttendanceRecords",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonAttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonAttendanceRecords_AttendanceStatuses_AttendanceStatusId",
                        column: x => x.AttendanceStatusId,
                        principalSchema: "attendance",
                        principalTable: "AttendanceStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "attendance",
                table: "AttendanceStatuses",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0015-000000000001"), "PRESENT", 1, true, "Present" },
                    { new Guid("00000000-0000-0000-0015-000000000002"), "ABSENT", 2, true, "Absent" },
                    { new Guid("00000000-0000-0000-0015-000000000003"), "LATE", 3, true, "Late" },
                    { new Guid("00000000-0000-0000-0015-000000000004"), "EXCUSED", 4, true, "Excused" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStatuses_Code",
                schema: "attendance",
                table: "AttendanceStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceRecords_AttendanceStatusId",
                schema: "attendance",
                table: "DailyAttendanceRecords",
                column: "AttendanceStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceRecords_StudentPersonId_Date",
                schema: "attendance",
                table: "DailyAttendanceRecords",
                columns: new[] { "StudentPersonId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonAttendanceRecords_AttendanceStatusId",
                schema: "attendance",
                table: "LessonAttendanceRecords",
                column: "AttendanceStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonAttendanceRecords_StudentPersonId_LessonSessionId",
                schema: "attendance",
                table: "LessonAttendanceRecords",
                columns: new[] { "StudentPersonId", "LessonSessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyAttendanceRecords",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "LessonAttendanceRecords",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "AttendanceStatuses",
                schema: "attendance");
        }
    }
}
