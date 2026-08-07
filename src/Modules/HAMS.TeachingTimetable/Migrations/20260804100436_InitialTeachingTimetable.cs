using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.TeachingTimetable.Migrations
{
    /// <inheritdoc />
    public partial class InitialTeachingTimetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "teaching");

            migrationBuilder.CreateTable(
                name: "AssignmentRoles",
                schema: "teaching",
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
                    table.PrimaryKey("PK_AssignmentRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassTeacherAssignments",
                schema: "teaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassTeacherAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadingTeacherAssignments",
                schema: "teaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadingTeacherAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Periods",
                schema: "teaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Periods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubjectTeachingAssignments",
                schema: "teaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectTeachingAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectTeachingAssignments_AssignmentRoles_AssignmentRoleId",
                        column: x => x.AssignmentRoleId,
                        principalSchema: "teaching",
                        principalTable: "AssignmentRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubstitutionRecords",
                schema: "teaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubstituteStaffPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubstitutionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubstitutionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubstitutionRecords_SubjectTeachingAssignments_GeneratedAssignmentId",
                        column: x => x.GeneratedAssignmentId,
                        principalSchema: "teaching",
                        principalTable: "SubjectTeachingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubstitutionRecords_SubjectTeachingAssignments_OriginalAssignmentId",
                        column: x => x.OriginalAssignmentId,
                        principalSchema: "teaching",
                        principalTable: "SubjectTeachingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimetableEntries",
                schema: "teaching",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeachingAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimetableEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimetableEntries_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalSchema: "teaching",
                        principalTable: "Periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimetableEntries_SubjectTeachingAssignments_TeachingAssignmentId",
                        column: x => x.TeachingAssignmentId,
                        principalSchema: "teaching",
                        principalTable: "SubjectTeachingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "teaching",
                table: "AssignmentRoles",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0013-000000000001"), "ORDINARY", 1, true, "Ordinary" },
                    { new Guid("00000000-0000-0000-0013-000000000002"), "SUBSTITUTE", 2, true, "Substitute" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRoles_Code",
                schema: "teaching",
                table: "AssignmentRoles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeacherAssignments_ClassId_AcademicYearId_EffectiveFrom_EffectiveTo",
                schema: "teaching",
                table: "ClassTeacherAssignments",
                columns: new[] { "ClassId", "AcademicYearId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeacherAssignments_StaffPersonId_EffectiveFrom_EffectiveTo",
                schema: "teaching",
                table: "ClassTeacherAssignments",
                columns: new[] { "StaffPersonId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadingTeacherAssignments_StaffPersonId_EffectiveFrom_EffectiveTo",
                schema: "teaching",
                table: "LeadingTeacherAssignments",
                columns: new[] { "StaffPersonId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadingTeacherAssignments_SubjectId_AcademicYearId_EffectiveFrom_EffectiveTo",
                schema: "teaching",
                table: "LeadingTeacherAssignments",
                columns: new[] { "SubjectId", "AcademicYearId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_Periods_SchoolId_Code",
                schema: "teaching",
                table: "Periods",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectTeachingAssignments_AssignmentRoleId",
                schema: "teaching",
                table: "SubjectTeachingAssignments",
                column: "AssignmentRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectTeachingAssignments_StaffPersonId_EffectiveFrom_EffectiveTo",
                schema: "teaching",
                table: "SubjectTeachingAssignments",
                columns: new[] { "StaffPersonId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectTeachingAssignments_SubjectId_ClassId_AcademicYearId_EffectiveFrom_EffectiveTo",
                schema: "teaching",
                table: "SubjectTeachingAssignments",
                columns: new[] { "SubjectId", "ClassId", "AcademicYearId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_SubstitutionRecords_GeneratedAssignmentId",
                schema: "teaching",
                table: "SubstitutionRecords",
                column: "GeneratedAssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubstitutionRecords_OriginalAssignmentId",
                schema: "teaching",
                table: "SubstitutionRecords",
                column: "OriginalAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEntries_ClassId_AcademicYearId_DayOfWeek_PeriodId",
                schema: "teaching",
                table: "TimetableEntries",
                columns: new[] { "ClassId", "AcademicYearId", "DayOfWeek", "PeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEntries_PeriodId",
                schema: "teaching",
                table: "TimetableEntries",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEntries_TeachingAssignmentId",
                schema: "teaching",
                table: "TimetableEntries",
                column: "TeachingAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassTeacherAssignments",
                schema: "teaching");

            migrationBuilder.DropTable(
                name: "LeadingTeacherAssignments",
                schema: "teaching");

            migrationBuilder.DropTable(
                name: "SubstitutionRecords",
                schema: "teaching");

            migrationBuilder.DropTable(
                name: "TimetableEntries",
                schema: "teaching");

            migrationBuilder.DropTable(
                name: "Periods",
                schema: "teaching");

            migrationBuilder.DropTable(
                name: "SubjectTeachingAssignments",
                schema: "teaching");

            migrationBuilder.DropTable(
                name: "AssignmentRoles",
                schema: "teaching");
        }
    }
}
