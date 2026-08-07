using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.Intervention.Migrations
{
    /// <inheritdoc />
    public partial class BehaviourPastoral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BehaviourCategories",
                schema: "intervention",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPositive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BehaviourCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BehaviourIncidents",
                schema: "intervention",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BehaviourCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ActionTaken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConfidentialityTierCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecordedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BehaviourIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BehaviourIncidents_BehaviourCategories_BehaviourCategoryId",
                        column: x => x.BehaviourCategoryId,
                        principalSchema: "intervention",
                        principalTable: "BehaviourCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "intervention",
                table: "BehaviourCategories",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "IsPositive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0026-000000000001"), "MERIT", 1, true, true, "Merit" },
                    { new Guid("00000000-0000-0000-0026-000000000002"), "RECOGNITION", 2, true, true, "Recognition" },
                    { new Guid("00000000-0000-0000-0026-000000000003"), "DISRUPTION", 3, true, false, "Disruption" },
                    { new Guid("00000000-0000-0000-0026-000000000004"), "DISRESPECT", 4, true, false, "Disrespect" },
                    { new Guid("00000000-0000-0000-0026-000000000005"), "BULLYING", 5, true, false, "Bullying" },
                    { new Guid("00000000-0000-0000-0026-000000000006"), "OTHER", 6, true, false, "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BehaviourCategories_Code",
                schema: "intervention",
                table: "BehaviourCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BehaviourIncidents_BehaviourCategoryId",
                schema: "intervention",
                table: "BehaviourIncidents",
                column: "BehaviourCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BehaviourIncidents_StudentPersonId_OccurredDate",
                schema: "intervention",
                table: "BehaviourIncidents",
                columns: new[] { "StudentPersonId", "OccurredDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BehaviourIncidents",
                schema: "intervention");

            migrationBuilder.DropTable(
                name: "BehaviourCategories",
                schema: "intervention");
        }
    }
}
