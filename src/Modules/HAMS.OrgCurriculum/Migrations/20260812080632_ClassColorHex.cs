using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.OrgCurriculum.Migrations
{
    /// <inheritdoc />
    public partial class ClassColorHex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                schema: "org",
                table: "Classes",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#3B82F6");

            // Backfill every pre-existing class with a distinct color from a rotating palette,
            // rather than leaving them all on the single AddColumn default above - otherwise the
            // new whole-school timetable calendar would render every existing class in the same
            // color until an admin manually re-edited each one.
            migrationBuilder.Sql("""
                WITH Palette AS (
                    SELECT * FROM (VALUES
                        (0, N'#EF4444'), (1, N'#F97316'), (2, N'#F59E0B'), (3, N'#84CC16'),
                        (4, N'#10B981'), (5, N'#14B8A6'), (6, N'#06B6D4'), (7, N'#3B82F6'),
                        (8, N'#6366F1'), (9, N'#8B5CF6'), (10, N'#A855F7'), (11, N'#EC4899'),
                        (12, N'#F43F5E'), (13, N'#0EA5E9')
                    ) AS p(Idx, Color)
                ),
                Ranked AS (
                    SELECT Id, (ROW_NUMBER() OVER (ORDER BY Id) - 1) % 14 AS PaletteIdx
                    FROM org.Classes
                )
                UPDATE c
                SET c.ColorHex = p.Color
                FROM org.Classes c
                JOIN Ranked r ON r.Id = c.Id
                JOIN Palette p ON p.Idx = r.PaletteIdx;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorHex",
                schema: "org",
                table: "Classes");
        }
    }
}
