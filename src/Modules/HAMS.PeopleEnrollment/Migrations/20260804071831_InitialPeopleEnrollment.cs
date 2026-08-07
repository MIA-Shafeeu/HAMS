using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.PeopleEnrollment.Migrations
{
    /// <inheritdoc />
    public partial class InitialPeopleEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "people");

            migrationBuilder.CreateTable(
                name: "Atolls",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameDv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atolls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentStatuses",
                schema: "people",
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
                    table.PrimaryKey("PK_EmploymentStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentTypes",
                schema: "people",
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
                    table.PrimaryKey("PK_EnrollmentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelationshipTypes",
                schema: "people",
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
                    table.PrimaryKey("PK_RelationshipTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestrictionTypes",
                schema: "people",
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
                    table.PrimaryKey("PK_RestrictionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Islands",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtollId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameDv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Islands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Islands_Atolls_AtollId",
                        column: x => x.AtollId,
                        principalSchema: "people",
                        principalTable: "Atolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentEnrollments",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_EnrollmentTypes_EnrollmentTypeId",
                        column: x => x.EnrollmentTypeId,
                        principalSchema: "people",
                        principalTable: "EnrollmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "People",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameDv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Address_IslandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address_RoadEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_RoadDv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_HouseNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_HouseNameDv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_BuildingEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_BuildingDv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_Floor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_Apartment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                    table.ForeignKey(
                        name: "FK_People_Islands_Address_IslandId",
                        column: x => x.Address_IslandId,
                        principalSchema: "people",
                        principalTable: "Islands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuardianProfiles",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardianProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuardianProfiles_People_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "people",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuardianStudentRelationships",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuardianPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationshipTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HasLegalAuthority = table.Column<bool>(type: "bit", nullable: false),
                    CanViewAcademicRecords = table.Column<bool>(type: "bit", nullable: false),
                    CanViewAttendance = table.Column<bool>(type: "bit", nullable: false),
                    CanViewBehaviourRecords = table.Column<bool>(type: "bit", nullable: false),
                    CanReceiveNotifications = table.Column<bool>(type: "bit", nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RestrictionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardianStudentRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuardianStudentRelationships_People_GuardianPersonId",
                        column: x => x.GuardianPersonId,
                        principalSchema: "people",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuardianStudentRelationships_People_StudentPersonId",
                        column: x => x.StudentPersonId,
                        principalSchema: "people",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuardianStudentRelationships_RelationshipTypes_RelationshipTypeId",
                        column: x => x.RelationshipTypeId,
                        principalSchema: "people",
                        principalTable: "RelationshipTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuardianStudentRelationships_RestrictionTypes_RestrictionTypeId",
                        column: x => x.RestrictionTypeId,
                        principalSchema: "people",
                        principalTable: "RestrictionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffProfiles",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EmploymentStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffProfiles_EmploymentStatuses_EmploymentStatusId",
                        column: x => x.EmploymentStatusId,
                        principalSchema: "people",
                        principalTable: "EmploymentStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffProfiles_People_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "people",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentProfiles",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdmissionDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentProfiles_People_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "people",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffQualifications",
                schema: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AwardingInstitution = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    YearAwarded = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffQualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffQualifications_StaffProfiles_StaffProfileId",
                        column: x => x.StaffProfileId,
                        principalSchema: "people",
                        principalTable: "StaffProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "people",
                table: "Atolls",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "NameDv", "NameEn" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0008-000000000001"), "HA", 1, true, null, "Haa Alifu" },
                    { new Guid("00000000-0000-0000-0008-000000000002"), "HDh", 2, true, null, "Haa Dhaalu" },
                    { new Guid("00000000-0000-0000-0008-000000000003"), "Sh", 3, true, null, "Shaviyani" },
                    { new Guid("00000000-0000-0000-0008-000000000004"), "N", 4, true, null, "Noonu" },
                    { new Guid("00000000-0000-0000-0008-000000000005"), "R", 5, true, null, "Raa" },
                    { new Guid("00000000-0000-0000-0008-000000000006"), "B", 6, true, null, "Baa" },
                    { new Guid("00000000-0000-0000-0008-000000000007"), "Lh", 7, true, null, "Lhaviyani" },
                    { new Guid("00000000-0000-0000-0008-000000000008"), "K", 8, true, null, "Kaafu" },
                    { new Guid("00000000-0000-0000-0008-000000000009"), "AA", 9, true, null, "Alifu Alifu" },
                    { new Guid("00000000-0000-0000-0008-000000000010"), "ADh", 10, true, null, "Alifu Dhaalu" },
                    { new Guid("00000000-0000-0000-0008-000000000011"), "V", 11, true, null, "Vaavu" },
                    { new Guid("00000000-0000-0000-0008-000000000012"), "M", 12, true, null, "Meemu" },
                    { new Guid("00000000-0000-0000-0008-000000000013"), "F", 13, true, null, "Faafu" },
                    { new Guid("00000000-0000-0000-0008-000000000014"), "Dh", 14, true, null, "Dhaalu" },
                    { new Guid("00000000-0000-0000-0008-000000000015"), "Th", 15, true, null, "Thaa" },
                    { new Guid("00000000-0000-0000-0008-000000000016"), "L", 16, true, null, "Laamu" },
                    { new Guid("00000000-0000-0000-0008-000000000017"), "GA", 17, true, null, "Gaafu Alifu" },
                    { new Guid("00000000-0000-0000-0008-000000000018"), "GDh", 18, true, null, "Gaafu Dhaalu" },
                    { new Guid("00000000-0000-0000-0008-000000000019"), "Gn", 19, true, null, "Gnaviyani" },
                    { new Guid("00000000-0000-0000-0008-000000000020"), "S", 20, true, null, "Seenu" }
                });

            migrationBuilder.InsertData(
                schema: "people",
                table: "EmploymentStatuses",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0010-000000000001"), "ACTIVE", 1, true, "Active" },
                    { new Guid("00000000-0000-0000-0010-000000000002"), "ON_LEAVE", 2, true, "On Leave" },
                    { new Guid("00000000-0000-0000-0010-000000000003"), "RESIGNED", 3, true, "Resigned" },
                    { new Guid("00000000-0000-0000-0010-000000000004"), "RETIRED", 4, true, "Retired" }
                });

            migrationBuilder.InsertData(
                schema: "people",
                table: "EnrollmentTypes",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0012-000000000001"), "ORDINARY", 1, true, "Ordinary" });

            migrationBuilder.InsertData(
                schema: "people",
                table: "RelationshipTypes",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0011-000000000001"), "MOTHER", 1, true, "Mother" },
                    { new Guid("00000000-0000-0000-0011-000000000002"), "FATHER", 2, true, "Father" },
                    { new Guid("00000000-0000-0000-0011-000000000003"), "GRANDPARENT", 3, true, "Grandparent" },
                    { new Guid("00000000-0000-0000-0011-000000000004"), "LEGAL_GUARDIAN", 4, true, "Legal Guardian" },
                    { new Guid("00000000-0000-0000-0011-000000000005"), "OTHER", 5, true, "Other" }
                });

            migrationBuilder.InsertData(
                schema: "people",
                table: "Islands",
                columns: new[] { "Id", "AtollId", "Code", "DisplayOrder", "IsActive", "NameDv", "NameEn" },
                values: new object[] { new Guid("00000000-0000-0000-0009-000000000001"), new Guid("00000000-0000-0000-0008-000000000015"), "HIRILANDHOO", 1, true, null, "Hirilandhoo" });

            migrationBuilder.CreateIndex(
                name: "IX_Atolls_Code",
                schema: "people",
                table: "Atolls",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatuses_Code",
                schema: "people",
                table: "EmploymentStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentTypes_Code",
                schema: "people",
                table: "EnrollmentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuardianProfiles_PersonId",
                schema: "people",
                table: "GuardianProfiles",
                column: "PersonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuardianStudentRelationships_GuardianPersonId_StudentPersonId_EffectiveFrom_EffectiveTo",
                schema: "people",
                table: "GuardianStudentRelationships",
                columns: new[] { "GuardianPersonId", "StudentPersonId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_GuardianStudentRelationships_RelationshipTypeId",
                schema: "people",
                table: "GuardianStudentRelationships",
                column: "RelationshipTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GuardianStudentRelationships_RestrictionTypeId",
                schema: "people",
                table: "GuardianStudentRelationships",
                column: "RestrictionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GuardianStudentRelationships_StudentPersonId",
                schema: "people",
                table: "GuardianStudentRelationships",
                column: "StudentPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Islands_AtollId",
                schema: "people",
                table: "Islands",
                column: "AtollId");

            migrationBuilder.CreateIndex(
                name: "IX_Islands_Code",
                schema: "people",
                table: "Islands",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_Address_IslandId",
                schema: "people",
                table: "People",
                column: "Address_IslandId");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipTypes_Code",
                schema: "people",
                table: "RelationshipTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestrictionTypes_Code",
                schema: "people",
                table: "RestrictionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffProfiles_EmployeeNumber",
                schema: "people",
                table: "StaffProfiles",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffProfiles_EmploymentStatusId",
                schema: "people",
                table: "StaffProfiles",
                column: "EmploymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffProfiles_PersonId",
                schema: "people",
                table: "StaffProfiles",
                column: "PersonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffQualifications_StaffProfileId",
                schema: "people",
                table: "StaffQualifications",
                column: "StaffProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_EnrollmentTypeId",
                schema: "people",
                table: "StudentEnrollments",
                column: "EnrollmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_OneActiveOrdinaryPerStudentYear",
                schema: "people",
                table: "StudentEnrollments",
                columns: new[] { "StudentPersonId", "AcademicYearId" },
                unique: true,
                filter: "[EnrollmentTypeId] = '00000000-0000-0000-0012-000000000001' AND [EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_StudentPersonId_AcademicYearId_EffectiveFrom_EffectiveTo",
                schema: "people",
                table: "StudentEnrollments",
                columns: new[] { "StudentPersonId", "AcademicYearId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_AdmissionNumber",
                schema: "people",
                table: "StudentProfiles",
                column: "AdmissionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_PersonId",
                schema: "people",
                table: "StudentProfiles",
                column: "PersonId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuardianProfiles",
                schema: "people");

            migrationBuilder.DropTable(
                name: "GuardianStudentRelationships",
                schema: "people");

            migrationBuilder.DropTable(
                name: "StaffQualifications",
                schema: "people");

            migrationBuilder.DropTable(
                name: "StudentEnrollments",
                schema: "people");

            migrationBuilder.DropTable(
                name: "StudentProfiles",
                schema: "people");

            migrationBuilder.DropTable(
                name: "RelationshipTypes",
                schema: "people");

            migrationBuilder.DropTable(
                name: "RestrictionTypes",
                schema: "people");

            migrationBuilder.DropTable(
                name: "StaffProfiles",
                schema: "people");

            migrationBuilder.DropTable(
                name: "EnrollmentTypes",
                schema: "people");

            migrationBuilder.DropTable(
                name: "EmploymentStatuses",
                schema: "people");

            migrationBuilder.DropTable(
                name: "People",
                schema: "people");

            migrationBuilder.DropTable(
                name: "Islands",
                schema: "people");

            migrationBuilder.DropTable(
                name: "Atolls",
                schema: "people");
        }
    }
}
