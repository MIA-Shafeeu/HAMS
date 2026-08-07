using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.AssessmentEvaluation.Migrations
{
    /// <inheritdoc />
    public partial class InitialAssessmentEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "assessment");

            migrationBuilder.CreateTable(
                name: "AssessmentCategories",
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
                    table.PrimaryKey("PK_AssessmentCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentSchemes",
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
                    table.PrimaryKey("PK_AssessmentSchemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalExaminationBoards",
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
                    table.PrimaryKey("PK_ExternalExaminationBoards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeScales",
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
                    table.PrimaryKey("PK_GradeScales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecialResultStates",
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
                    table.PrimaryKey("PK_SpecialResultStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentSchemeComponents",
                schema: "assessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentSchemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeightPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentSchemeComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentSchemeComponents_AssessmentCategories_AssessmentCategoryId",
                        column: x => x.AssessmentCategoryId,
                        principalSchema: "assessment",
                        principalTable: "AssessmentCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentSchemeComponents_AssessmentSchemes_AssessmentSchemeId",
                        column: x => x.AssessmentSchemeId,
                        principalSchema: "assessment",
                        principalTable: "AssessmentSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                schema: "assessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaxMarks = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    ExternalExaminationBoardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalSyllabusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assessments_AssessmentCategories_AssessmentCategoryId",
                        column: x => x.AssessmentCategoryId,
                        principalSchema: "assessment",
                        principalTable: "AssessmentCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assessments_ExternalExaminationBoards_ExternalExaminationBoardId",
                        column: x => x.ExternalExaminationBoardId,
                        principalSchema: "assessment",
                        principalTable: "ExternalExaminationBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradeBands",
                schema: "assessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeBands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeBands_GradeScales_GradeScaleId",
                        column: x => x.GradeScaleId,
                        principalSchema: "assessment",
                        principalTable: "GradeScales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentResults",
                schema: "assessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyStagePolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RawMark = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    AdjustedMark = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    ModeratedMark = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    FinalMark = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    SpecialResultStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModerationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentResults_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalSchema: "assessment",
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentResults_SpecialResultStates_SpecialResultStateId",
                        column: x => x.SpecialResultStateId,
                        principalSchema: "assessment",
                        principalTable: "SpecialResultStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "assessment",
                table: "AssessmentCategories",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0020-000000000001"), "TERM_EXAM", 1, true, "Term Exam" },
                    { new Guid("00000000-0000-0000-0020-000000000002"), "CONTINUOUS_ASSESSMENT", 2, true, "Continuous Assessment" },
                    { new Guid("00000000-0000-0000-0020-000000000003"), "QUIZ", 3, true, "Quiz" },
                    { new Guid("00000000-0000-0000-0020-000000000004"), "PROJECT", 4, true, "Project" },
                    { new Guid("00000000-0000-0000-0020-000000000005"), "OTHER", 5, true, "Other" }
                });

            migrationBuilder.InsertData(
                schema: "assessment",
                table: "ExternalExaminationBoards",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0021-000000000001"), "CAMBRIDGE", 1, true, "Cambridge Assessment International Education" },
                    { new Guid("00000000-0000-0000-0021-000000000002"), "EDEXCEL", 2, true, "Pearson Edexcel" },
                    { new Guid("00000000-0000-0000-0021-000000000003"), "SSC", 3, true, "Secondary School Certificate (SSC)" },
                    { new Guid("00000000-0000-0000-0021-000000000004"), "HSC", 4, true, "Higher Secondary Certificate (HSC)" }
                });

            migrationBuilder.InsertData(
                schema: "assessment",
                table: "SpecialResultStates",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0022-000000000001"), "MEDICAL_CERTIFICATE_EXCUSED", 1, true, "Medical Certificate Excused" },
                    { new Guid("00000000-0000-0000-0022-000000000002"), "AUTHORIZED_TRAVEL_MAKEUP", 2, true, "Authorized Travel - Make-Up Exam" },
                    { new Guid("00000000-0000-0000-0022-000000000003"), "CALIBRATION_ONLY", 3, true, "Calibration Only" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCategories_Code",
                schema: "assessment",
                table: "AssessmentCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AssessmentId_StudentPersonId_IsCurrent",
                schema: "assessment",
                table: "AssessmentResults",
                columns: new[] { "AssessmentId", "StudentPersonId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_SpecialResultStateId",
                schema: "assessment",
                table: "AssessmentResults",
                column: "SpecialResultStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_AssessmentCategoryId",
                schema: "assessment",
                table: "Assessments",
                column: "AssessmentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_ExternalExaminationBoardId",
                schema: "assessment",
                table: "Assessments",
                column: "ExternalExaminationBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SubjectId_GradeId_TermId",
                schema: "assessment",
                table: "Assessments",
                columns: new[] { "SubjectId", "GradeId", "TermId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSchemeComponents_AssessmentCategoryId",
                schema: "assessment",
                table: "AssessmentSchemeComponents",
                column: "AssessmentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSchemeComponents_AssessmentSchemeId_AssessmentCategoryId",
                schema: "assessment",
                table: "AssessmentSchemeComponents",
                columns: new[] { "AssessmentSchemeId", "AssessmentCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSchemes_Code",
                schema: "assessment",
                table: "AssessmentSchemes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalExaminationBoards_Code",
                schema: "assessment",
                table: "ExternalExaminationBoards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeBands_GradeScaleId_Code",
                schema: "assessment",
                table: "GradeBands",
                columns: new[] { "GradeScaleId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeScales_Code",
                schema: "assessment",
                table: "GradeScales",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecialResultStates_Code",
                schema: "assessment",
                table: "SpecialResultStates",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentResults",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "AssessmentSchemeComponents",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "GradeBands",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "Assessments",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "SpecialResultStates",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "AssessmentSchemes",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "GradeScales",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "AssessmentCategories",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "ExternalExaminationBoards",
                schema: "assessment");
        }
    }
}
