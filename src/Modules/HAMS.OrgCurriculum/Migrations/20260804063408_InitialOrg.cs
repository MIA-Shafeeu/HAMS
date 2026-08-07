using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.OrgCurriculum.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "org");

            migrationBuilder.CreateTable(
                name: "EvaluationModels",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AcademicYears",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicYears_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "org",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Campuses",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campuses_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "org",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grades_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "org",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Phases",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Phases_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "org",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Terms",
                schema: "org",
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
                    table.PrimaryKey("PK_Terms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Terms_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalSchema: "org",
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classes_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalSchema: "org",
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalSchema: "org",
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "org",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeyStages",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyStages_Phases_PhaseId",
                        column: x => x.PhaseId,
                        principalSchema: "org",
                        principalTable: "Phases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyStages_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "org",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassGrades",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassGrades_Classes_ClassId",
                        column: x => x.ClassId,
                        principalSchema: "org",
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassGrades_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "org",
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradeKeyStageAssignments",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeKeyStageAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeKeyStageAssignments_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalSchema: "org",
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeKeyStageAssignments_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "org",
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeKeyStageAssignments_KeyStages_KeyStageId",
                        column: x => x.KeyStageId,
                        principalSchema: "org",
                        principalTable: "KeyStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeyStagePolicies",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssessmentSchemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PromotionPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyStagePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyStagePolicies_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalSchema: "org",
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyStagePolicies_EvaluationModels_EvaluationModelId",
                        column: x => x.EvaluationModelId,
                        principalSchema: "org",
                        principalTable: "EvaluationModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyStagePolicies_KeyStages_KeyStageId",
                        column: x => x.KeyStageId,
                        principalSchema: "org",
                        principalTable: "KeyStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "org",
                table: "EvaluationModels",
                columns: new[] { "Id", "Code", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0003-000000000001"), "MASTERY", "Continuous learning-outcome mastery only.", 1, true, "Mastery" },
                    { new Guid("00000000-0000-0000-0003-000000000002"), "ASSESSMENT", "External syndicated summative examination only.", 2, true, "Assessment" },
                    { new Guid("00000000-0000-0000-0003-000000000003"), "HYBRID", "Continuous assessment combined with a time-boxed exam.", 3, true, "Hybrid" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_SchoolId_Code",
                schema: "org",
                table: "AcademicYears",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campuses_SchoolId_Code",
                schema: "org",
                table: "Campuses",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_AcademicYearId_Code",
                schema: "org",
                table: "Classes",
                columns: new[] { "AcademicYearId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_CampusId",
                schema: "org",
                table: "Classes",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_SchoolId",
                schema: "org",
                table: "Classes",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassGrades_ClassId_GradeId",
                schema: "org",
                table: "ClassGrades",
                columns: new[] { "ClassId", "GradeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassGrades_GradeId",
                schema: "org",
                table: "ClassGrades",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationModels_Code",
                schema: "org",
                table: "EvaluationModels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeKeyStageAssignments_AcademicYearId",
                schema: "org",
                table: "GradeKeyStageAssignments",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeKeyStageAssignments_GradeId_AcademicYearId_EffectiveFrom_EffectiveTo",
                schema: "org",
                table: "GradeKeyStageAssignments",
                columns: new[] { "GradeId", "AcademicYearId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeKeyStageAssignments_KeyStageId",
                schema: "org",
                table: "GradeKeyStageAssignments",
                column: "KeyStageId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_SchoolId_Code",
                schema: "org",
                table: "Grades",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeyStagePolicies_AcademicYearId",
                schema: "org",
                table: "KeyStagePolicies",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyStagePolicies_EvaluationModelId",
                schema: "org",
                table: "KeyStagePolicies",
                column: "EvaluationModelId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyStagePolicies_KeyStageId_AcademicYearId_IsCurrent",
                schema: "org",
                table: "KeyStagePolicies",
                columns: new[] { "KeyStageId", "AcademicYearId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_KeyStages_PhaseId",
                schema: "org",
                table: "KeyStages",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyStages_SchoolId_Code",
                schema: "org",
                table: "KeyStages",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Phases_SchoolId_Code",
                schema: "org",
                table: "Phases",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Code",
                schema: "org",
                table: "Schools",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Terms_AcademicYearId_Code",
                schema: "org",
                table: "Terms",
                columns: new[] { "AcademicYearId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassGrades",
                schema: "org");

            migrationBuilder.DropTable(
                name: "GradeKeyStageAssignments",
                schema: "org");

            migrationBuilder.DropTable(
                name: "KeyStagePolicies",
                schema: "org");

            migrationBuilder.DropTable(
                name: "Terms",
                schema: "org");

            migrationBuilder.DropTable(
                name: "Classes",
                schema: "org");

            migrationBuilder.DropTable(
                name: "Grades",
                schema: "org");

            migrationBuilder.DropTable(
                name: "EvaluationModels",
                schema: "org");

            migrationBuilder.DropTable(
                name: "KeyStages",
                schema: "org");

            migrationBuilder.DropTable(
                name: "AcademicYears",
                schema: "org");

            migrationBuilder.DropTable(
                name: "Campuses",
                schema: "org");

            migrationBuilder.DropTable(
                name: "Phases",
                schema: "org");

            migrationBuilder.DropTable(
                name: "Schools",
                schema: "org");
        }
    }
}
