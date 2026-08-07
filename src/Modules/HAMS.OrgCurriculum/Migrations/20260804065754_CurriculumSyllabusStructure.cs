using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.OrgCurriculum.Migrations
{
    /// <inheritdoc />
    public partial class CurriculumSyllabusStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurriculumFrameworks",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumFrameworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryModes",
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
                    table.PrimaryKey("PK_DeliveryModes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediumsOfInstruction",
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
                    table.PrimaryKey("PK_MediumsOfInstruction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningAreas",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumFrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningAreas_CurriculumFrameworks_CurriculumFrameworkId",
                        column: x => x.CurriculumFrameworkId,
                        principalSchema: "org",
                        principalTable: "CurriculumFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeliveryModeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultMediumOfInstructionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subjects_DeliveryModes_DeliveryModeId",
                        column: x => x.DeliveryModeId,
                        principalSchema: "org",
                        principalTable: "DeliveryModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_LearningAreas_LearningAreaId",
                        column: x => x.LearningAreaId,
                        principalSchema: "org",
                        principalTable: "LearningAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_MediumsOfInstruction_DefaultMediumOfInstructionId",
                        column: x => x.DefaultMediumOfInstructionId,
                        principalSchema: "org",
                        principalTable: "MediumsOfInstruction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "org",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Syllabuses",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Syllabuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Syllabuses_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "org",
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Strands",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SyllabusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Strands_Syllabuses_SyllabusId",
                        column: x => x.SyllabusId,
                        principalSchema: "org",
                        principalTable: "Syllabuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyllabusGradeApplicabilities",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SyllabusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusGradeApplicabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyllabusGradeApplicabilities_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "org",
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyllabusGradeApplicabilities_Syllabuses_SyllabusId",
                        column: x => x.SyllabusId,
                        principalSchema: "org",
                        principalTable: "Syllabuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubStrands",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubStrands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubStrands_Strands_StrandId",
                        column: x => x.StrandId,
                        principalSchema: "org",
                        principalTable: "Strands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningOutcomes",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubStrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningOutcomes_SubStrands_SubStrandId",
                        column: x => x.SubStrandId,
                        principalSchema: "org",
                        principalTable: "SubStrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Indicators",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Indicators_LearningOutcomes_LearningOutcomeId",
                        column: x => x.LearningOutcomeId,
                        principalSchema: "org",
                        principalTable: "LearningOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningOutcomePrerequisites",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrerequisiteLearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningOutcomePrerequisites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningOutcomePrerequisites_LearningOutcomes_LearningOutcomeId",
                        column: x => x.LearningOutcomeId,
                        principalSchema: "org",
                        principalTable: "LearningOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningOutcomePrerequisites_LearningOutcomes_PrerequisiteLearningOutcomeId",
                        column: x => x.PrerequisiteLearningOutcomeId,
                        principalSchema: "org",
                        principalTable: "LearningOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "org",
                table: "CurriculumFrameworks",
                columns: new[] { "Id", "Code", "Description", "IsActive", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0004-000000000001"), "NCF", "Maldives National Curriculum Framework.", true, "National Curriculum Framework" });

            migrationBuilder.InsertData(
                schema: "org",
                table: "DeliveryModes",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0006-000000000001"), "TIMETABLED", 1, true, "Timetabled" },
                    { new Guid("00000000-0000-0000-0006-000000000002"), "INTEGRATED", 2, true, "Integrated" }
                });

            migrationBuilder.InsertData(
                schema: "org",
                table: "MediumsOfInstruction",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0007-000000000001"), "DHIVEHI", 1, true, "Dhivehi" },
                    { new Guid("00000000-0000-0000-0007-000000000002"), "ENGLISH", 2, true, "English" }
                });

            migrationBuilder.InsertData(
                schema: "org",
                table: "LearningAreas",
                columns: new[] { "Id", "Code", "CurriculumFrameworkId", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0005-000000000001"), "ISLAM_SPIRITUALITY", new Guid("00000000-0000-0000-0004-000000000001"), 1, true, "Islam & Spirituality" },
                    { new Guid("00000000-0000-0000-0005-000000000002"), "LANGUAGE_COMMUNICATION", new Guid("00000000-0000-0000-0004-000000000001"), 2, true, "Language & Communication" },
                    { new Guid("00000000-0000-0000-0005-000000000003"), "MATHEMATICS", new Guid("00000000-0000-0000-0004-000000000001"), 3, true, "Mathematics" },
                    { new Guid("00000000-0000-0000-0005-000000000004"), "ENVIRONMENT_SCIENCE_TECHNOLOGY", new Guid("00000000-0000-0000-0004-000000000001"), 4, true, "Environment/Science & Technology" },
                    { new Guid("00000000-0000-0000-0005-000000000005"), "HEALTH_WELLBEING", new Guid("00000000-0000-0000-0004-000000000001"), 5, true, "Health & Wellbeing" },
                    { new Guid("00000000-0000-0000-0005-000000000006"), "SOCIAL_SCIENCES", new Guid("00000000-0000-0000-0004-000000000001"), 6, true, "Social Sciences" },
                    { new Guid("00000000-0000-0000-0005-000000000007"), "CREATIVE_ARTS", new Guid("00000000-0000-0000-0004-000000000001"), 7, true, "Creative Arts" },
                    { new Guid("00000000-0000-0000-0005-000000000008"), "ENTREPRENEURSHIP", new Guid("00000000-0000-0000-0004-000000000001"), 8, true, "Entrepreneurship" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumFrameworks_Code",
                schema: "org",
                table: "CurriculumFrameworks",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryModes_Code",
                schema: "org",
                table: "DeliveryModes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_LearningOutcomeId_Code",
                schema: "org",
                table: "Indicators",
                columns: new[] { "LearningOutcomeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningAreas_Code",
                schema: "org",
                table: "LearningAreas",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningAreas_CurriculumFrameworkId",
                schema: "org",
                table: "LearningAreas",
                column: "CurriculumFrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomePrerequisites_LearningOutcomeId_PrerequisiteLearningOutcomeId",
                schema: "org",
                table: "LearningOutcomePrerequisites",
                columns: new[] { "LearningOutcomeId", "PrerequisiteLearningOutcomeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomePrerequisites_PrerequisiteLearningOutcomeId",
                schema: "org",
                table: "LearningOutcomePrerequisites",
                column: "PrerequisiteLearningOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomes_SubStrandId_Code",
                schema: "org",
                table: "LearningOutcomes",
                columns: new[] { "SubStrandId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediumsOfInstruction_Code",
                schema: "org",
                table: "MediumsOfInstruction",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Strands_SyllabusId_Code",
                schema: "org",
                table: "Strands",
                columns: new[] { "SyllabusId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_DefaultMediumOfInstructionId",
                schema: "org",
                table: "Subjects",
                column: "DefaultMediumOfInstructionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_DeliveryModeId",
                schema: "org",
                table: "Subjects",
                column: "DeliveryModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_LearningAreaId",
                schema: "org",
                table: "Subjects",
                column: "LearningAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SchoolId_Code",
                schema: "org",
                table: "Subjects",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubStrands_StrandId_Code",
                schema: "org",
                table: "SubStrands",
                columns: new[] { "StrandId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Syllabuses_SubjectId_IsCurrent",
                schema: "org",
                table: "Syllabuses",
                columns: new[] { "SubjectId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusGradeApplicabilities_GradeId",
                schema: "org",
                table: "SyllabusGradeApplicabilities",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusGradeApplicabilities_SyllabusId_GradeId",
                schema: "org",
                table: "SyllabusGradeApplicabilities",
                columns: new[] { "SyllabusId", "GradeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Indicators",
                schema: "org");

            migrationBuilder.DropTable(
                name: "LearningOutcomePrerequisites",
                schema: "org");

            migrationBuilder.DropTable(
                name: "SyllabusGradeApplicabilities",
                schema: "org");

            migrationBuilder.DropTable(
                name: "LearningOutcomes",
                schema: "org");

            migrationBuilder.DropTable(
                name: "SubStrands",
                schema: "org");

            migrationBuilder.DropTable(
                name: "Strands",
                schema: "org");

            migrationBuilder.DropTable(
                name: "Syllabuses",
                schema: "org");

            migrationBuilder.DropTable(
                name: "Subjects",
                schema: "org");

            migrationBuilder.DropTable(
                name: "DeliveryModes",
                schema: "org");

            migrationBuilder.DropTable(
                name: "LearningAreas",
                schema: "org");

            migrationBuilder.DropTable(
                name: "MediumsOfInstruction",
                schema: "org");

            migrationBuilder.DropTable(
                name: "CurriculumFrameworks",
                schema: "org");
        }
    }
}
