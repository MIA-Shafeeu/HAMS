using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.TeachingTimetable.Migrations
{
    /// <inheritdoc />
    public partial class PeriodTimeSpanUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Periods_SchoolId_StartTime_EndTime",
                schema: "teaching",
                table: "Periods",
                columns: new[] { "SchoolId", "StartTime", "EndTime" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Periods_SchoolId_StartTime_EndTime",
                schema: "teaching",
                table: "Periods");
        }
    }
}
