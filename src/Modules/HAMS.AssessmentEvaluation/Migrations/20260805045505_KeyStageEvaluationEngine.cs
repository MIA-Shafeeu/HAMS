using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.AssessmentEvaluation.Migrations
{
    /// <inheritdoc />
    public partial class KeyStageEvaluationEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResultAggregationRuleId",
                schema: "assessment",
                table: "AssessmentSchemeComponents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "EvaluationPeriods",
                schema: "assessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResultAggregationRules",
                schema: "assessment",
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
                    table.PrimaryKey("PK_ResultAggregationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeyStageEvaluations",
                schema: "assessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyStagePolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallAchievementLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OverallPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    OverallGradeBandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyStageEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyStageEvaluations_EvaluationPeriods_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalSchema: "assessment",
                        principalTable: "EvaluationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyStageEvaluations_GradeBands_OverallGradeBandId",
                        column: x => x.OverallGradeBandId,
                        principalSchema: "assessment",
                        principalTable: "GradeBands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "assessment",
                table: "ResultAggregationRules",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0023-000000000001"), "LATEST", 1, true, "Latest Attempt" },
                    { new Guid("00000000-0000-0000-0023-000000000002"), "HIGHEST", 2, true, "Highest Attempt" },
                    { new Guid("00000000-0000-0000-0023-000000000003"), "AVERAGE", 3, true, "Attempt Average" }
                });

            // Backfill any AssessmentSchemeComponent rows that existed before this column was
            // added (defaulted to Guid.Empty above) to a real rule, so the FK constraint added
            // below doesn't reject pre-existing data — Average is the safest default since it's
            // the least surprising behavior for a component nobody explicitly configured yet.
            migrationBuilder.Sql(
                "UPDATE [assessment].[AssessmentSchemeComponents] " +
                "SET [ResultAggregationRuleId] = '00000000-0000-0000-0023-000000000003' " +
                "WHERE [ResultAggregationRuleId] = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSchemeComponents_ResultAggregationRuleId",
                schema: "assessment",
                table: "AssessmentSchemeComponents",
                column: "ResultAggregationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPeriods_AcademicYearId_Code",
                schema: "assessment",
                table: "EvaluationPeriods",
                columns: new[] { "AcademicYearId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeyStageEvaluations_EvaluationPeriodId",
                schema: "assessment",
                table: "KeyStageEvaluations",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyStageEvaluations_OverallGradeBandId",
                schema: "assessment",
                table: "KeyStageEvaluations",
                column: "OverallGradeBandId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyStageEvaluations_StudentPersonId_SubjectId_EvaluationPeriodId_RecordedAtUtc",
                schema: "assessment",
                table: "KeyStageEvaluations",
                columns: new[] { "StudentPersonId", "SubjectId", "EvaluationPeriodId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ResultAggregationRules_Code",
                schema: "assessment",
                table: "ResultAggregationRules",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentSchemeComponents_ResultAggregationRules_ResultAggregationRuleId",
                schema: "assessment",
                table: "AssessmentSchemeComponents",
                column: "ResultAggregationRuleId",
                principalSchema: "assessment",
                principalTable: "ResultAggregationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentSchemeComponents_ResultAggregationRules_ResultAggregationRuleId",
                schema: "assessment",
                table: "AssessmentSchemeComponents");

            migrationBuilder.DropTable(
                name: "KeyStageEvaluations",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "ResultAggregationRules",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "EvaluationPeriods",
                schema: "assessment");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentSchemeComponents_ResultAggregationRuleId",
                schema: "assessment",
                table: "AssessmentSchemeComponents");

            migrationBuilder.DropColumn(
                name: "ResultAggregationRuleId",
                schema: "assessment",
                table: "AssessmentSchemeComponents");
        }
    }
}
