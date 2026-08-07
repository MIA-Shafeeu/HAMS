using HAMS.ReportingAnalyticsAudit.Application;

namespace HAMS.ReportingAnalyticsAudit.Tests;

public class DashboardSnapshotCalculatorTests
{
    [Fact]
    public void Calculate_counts_distinct_students_not_enrollment_rows()
    {
        var studentA = Guid.NewGuid();
        var studentB = Guid.NewGuid();
        var roster = new List<(Guid StudentPersonId, string GradeName)>
        {
            (studentA, "Grade 5"), (studentB, "Grade 5"), (studentA, "Grade 5"),
        };

        var result = DashboardSnapshotCalculator.Calculate(roster, [], [], []);

        Assert.Equal(2, result.TotalActiveStudents);
        var grade5 = Assert.Single(result.EnrollmentByGrade);
        Assert.Equal(2, grade5.StudentCount);
    }

    [Fact]
    public void Calculate_groups_enrollment_by_grade_and_orders_alphabetically()
    {
        var roster = new List<(Guid StudentPersonId, string GradeName)>
        {
            (Guid.NewGuid(), "Grade 8"), (Guid.NewGuid(), "Grade 5"), (Guid.NewGuid(), "Grade 5"), (Guid.NewGuid(), "Grade 6"),
        };

        var result = DashboardSnapshotCalculator.Calculate(roster, [], [], []);

        Assert.Equal(["Grade 5", "Grade 6", "Grade 8"], result.EnrollmentByGrade.Select(e => e.GradeName));
        Assert.Equal(2, result.EnrollmentByGrade.Single(e => e.GradeName == "Grade 5").StudentCount);
    }

    [Fact]
    public void Calculate_computes_attendance_rate_percentage_rounded_to_one_decimal()
    {
        string[] attendance = ["PRESENT", "PRESENT", "PRESENT", "ABSENT"];

        var result = DashboardSnapshotCalculator.Calculate([], attendance, [], []);

        Assert.Equal(4, result.AttendanceLast30Days.TotalRecords);
        Assert.Equal(3, result.AttendanceLast30Days.PresentCount);
        Assert.Equal(75.0, result.AttendanceLast30Days.PresentRatePercent);
    }

    [Fact]
    public void Calculate_attendance_rate_is_zero_not_a_divide_by_zero_when_no_records_exist()
    {
        var result = DashboardSnapshotCalculator.Calculate([], [], [], []);

        Assert.Equal(0, result.AttendanceLast30Days.TotalRecords);
        Assert.Equal(0, result.AttendanceLast30Days.PresentRatePercent);
    }

    [Fact]
    public void Calculate_sums_intervention_case_counts_by_type_across_multiple_rows_of_the_same_status()
    {
        var interventionRows = new List<(string InterventionTypeName, string Status, int CaseCount)>
        {
            ("Additional Practice", "Open", 3), ("Additional Practice", "Closed", 2), ("One-on-One Support", "Open", 1),
        };

        var result = DashboardSnapshotCalculator.Calculate([], [], interventionRows, []);

        var additionalPractice = result.InterventionCasesByType.Single(c => c.InterventionTypeName == "Additional Practice");
        Assert.Equal(3, additionalPractice.OpenCount);
        Assert.Equal(2, additionalPractice.ClosedCount);
        var oneOnOne = result.InterventionCasesByType.Single(c => c.InterventionTypeName == "One-on-One Support");
        Assert.Equal(1, oneOnOne.OpenCount);
        Assert.Equal(0, oneOnOne.ClosedCount);
    }

    [Fact]
    public void Calculate_splits_promotion_outcomes_into_promoted_and_not_promoted_counts()
    {
        bool[] outcomes = [true, true, false];

        var result = DashboardSnapshotCalculator.Calculate([], [], [], outcomes);

        Assert.Equal(2, result.PromotionDecisions.PromotedCount);
        Assert.Equal(1, result.PromotionDecisions.NotPromotedCount);
    }
}
