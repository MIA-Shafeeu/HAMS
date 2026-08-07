using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.AssessmentEvaluation.Migrations
{
    /// <inheritdoc />
    public partial class PromotionPolicyAndDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromotionDecisions",
                schema: "assessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentGradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Promoted = table.Column<bool>(type: "bit", nullable: false),
                    NextGradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionPolicies",
                schema: "assessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinimumRank = table.Column<int>(type: "int", nullable: false),
                    MinimumSubjectsRequiredToClear = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionPolicies", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "assessment",
                table: "PromotionPolicies",
                columns: new[] { "Id", "Code", "IsActive", "MinimumRank", "MinimumSubjectsRequiredToClear", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0025-000000000001"), "STANDARD", true, 2, 1, "Standard Promotion Policy" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionDecisions_StudentPersonId_RecordedAtUtc",
                schema: "assessment",
                table: "PromotionDecisions",
                columns: new[] { "StudentPersonId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionPolicies_Code",
                schema: "assessment",
                table: "PromotionPolicies",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionDecisions",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "PromotionPolicies",
                schema: "assessment");
        }
    }
}
