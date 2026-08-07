using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.Platform.Access.Migrations
{
    /// <inheritdoc />
    public partial class InitialAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "access");

            migrationBuilder.CreateTable(
                name: "ConfidentialityTiers",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfidentialityTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfidentialAccessGrants",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfidentialityTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfidentialAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfidentialAccessGrants_ConfidentialityTiers_ConfidentialityTierId",
                        column: x => x.ConfidentialityTierId,
                        principalSchema: "access",
                        principalTable: "ConfidentialityTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessGrants",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    KeyStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfidentialityTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessGrants_ConfidentialityTiers_ConfidentialityTierId",
                        column: x => x.ConfidentialityTierId,
                        principalSchema: "access",
                        principalTable: "ConfidentialityTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessGrants_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "access",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonRoleAssignments",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonRoleAssignments_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "access",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "access",
                table: "ConfidentialityTiers",
                columns: new[] { "Id", "Code", "Description", "DisplayOrder", "IsActive", "Name", "Rank" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0002-000000000001"), "RESTRICTED", null, 1, true, "Restricted", 1 },
                    { new Guid("00000000-0000-0000-0002-000000000002"), "SAFEGUARDING", null, 2, true, "Safeguarding", 2 }
                });

            migrationBuilder.InsertData(
                schema: "access",
                table: "Roles",
                columns: new[] { "Id", "Code", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0001-000000000001"), "SYSTEM_ADMINISTRATOR", null, 1, true, "System Administrator" },
                    { new Guid("00000000-0000-0000-0001-000000000002"), "SCHOOL_ADMINISTRATOR", null, 2, true, "School Administrator" },
                    { new Guid("00000000-0000-0000-0001-000000000003"), "PRINCIPAL", null, 3, true, "Principal" },
                    { new Guid("00000000-0000-0000-0001-000000000004"), "DEPUTY_PRINCIPAL", null, 4, true, "Deputy Principal" },
                    { new Guid("00000000-0000-0000-0001-000000000005"), "CLASS_TEACHER", null, 5, true, "Class Teacher" },
                    { new Guid("00000000-0000-0000-0001-000000000006"), "SUBJECT_TEACHER", null, 6, true, "Subject Teacher" },
                    { new Guid("00000000-0000-0000-0001-000000000007"), "LEADING_TEACHER", null, 7, true, "Leading Teacher" },
                    { new Guid("00000000-0000-0000-0001-000000000008"), "STUDENT", null, 8, true, "Student" },
                    { new Guid("00000000-0000-0000-0001-000000000009"), "GUARDIAN", null, 9, true, "Guardian" },
                    { new Guid("00000000-0000-0000-0001-000000000010"), "REGULATORY_OFFICER", null, 10, true, "Regulatory Officer" },
                    { new Guid("00000000-0000-0000-0001-000000000011"), "SCHOOL_INSPECTOR", null, 11, true, "School Inspector" },
                    { new Guid("00000000-0000-0000-0001-000000000012"), "AUDITOR", null, 12, true, "Auditor" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_ConfidentialityTierId",
                schema: "access",
                table: "AccessGrants",
                column: "ConfidentialityTierId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_PersonId_EffectiveFrom_EffectiveTo",
                schema: "access",
                table: "AccessGrants",
                columns: new[] { "PersonId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_RoleId",
                schema: "access",
                table: "AccessGrants",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_SourceType_SourceId",
                schema: "access",
                table: "AccessGrants",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfidentialAccessGrants_ConfidentialityTierId",
                schema: "access",
                table: "ConfidentialAccessGrants",
                column: "ConfidentialityTierId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfidentialAccessGrants_PersonId_StudentId_EffectiveFrom_EffectiveTo",
                schema: "access",
                table: "ConfidentialAccessGrants",
                columns: new[] { "PersonId", "StudentId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfidentialityTiers_Code",
                schema: "access",
                table: "ConfidentialityTiers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoleAssignments_PersonId_EffectiveFrom_EffectiveTo",
                schema: "access",
                table: "PersonRoleAssignments",
                columns: new[] { "PersonId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoleAssignments_RoleId",
                schema: "access",
                table: "PersonRoleAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                schema: "access",
                table: "Roles",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessGrants",
                schema: "access");

            migrationBuilder.DropTable(
                name: "ConfidentialAccessGrants",
                schema: "access");

            migrationBuilder.DropTable(
                name: "PersonRoleAssignments",
                schema: "access");

            migrationBuilder.DropTable(
                name: "ConfidentialityTiers",
                schema: "access");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "access");
        }
    }
}
