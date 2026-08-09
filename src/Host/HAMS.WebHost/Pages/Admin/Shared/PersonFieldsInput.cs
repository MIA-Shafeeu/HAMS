using HAMS.PeopleEnrollment.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HAMS.WebHost.Pages.Admin.Shared;

/// <summary>
/// Backing model for the shared <c>_PersonFields.cshtml</c> partial — the same Name/Address field
/// set is needed verbatim on the Student/Staff/Guardian create forms and on each tab's own
/// edit-person form (6 call sites total in <c>PeopleEnrollment.cshtml</c>), so this is shared rather
/// than repeated. Replaces the old Blazor <c>PersonFormModel</c>; <see cref="DateOfBirth"/> is a
/// plain <see cref="DateOnly"/> now — a native <c>&lt;input type="date"&gt;</c> model-binds
/// <see cref="DateOnly"/> directly, so the old MudDatePicker null-guard dance is gone.
/// </summary>
public sealed class PersonFieldsInput
{
    public string NameEn { get; set; } = "";
    public string NameDv { get; set; } = "";
    public DateOnly DateOfBirth { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-10));
    public Guid AtollId { get; set; }
    public Guid IslandId { get; set; }
    public string RoadEn { get; set; } = "";
    public string RoadDv { get; set; } = "";
    public string HouseNameEn { get; set; } = "";
    public string HouseNameDv { get; set; } = "";
    public string? BuildingEn { get; set; }
    public string? BuildingDv { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }

    /// <summary>
    /// Every atoll, and the islands within whichever atoll is currently selected — populated
    /// server-side by <c>PeopleEnrollmentModel.LoadAllAsync</c> (never posted back; <see cref="BindNeverAttribute"/>
    /// keeps model binding from wasting a pass over them), so the partial can render both cascading
    /// selects without every one of the 6 call sites needing to know how to load them.
    /// </summary>
    [BindNever] public IReadOnlyList<Atoll> Atolls { get; set; } = [];

    [BindNever] public IReadOnlyList<Island> Islands { get; set; } = [];

    public Address ToAddress() => new()
    {
        IslandId = IslandId, RoadEn = RoadEn, RoadDv = RoadDv,
        HouseNameEn = HouseNameEn, HouseNameDv = HouseNameDv,
        BuildingEn = BuildingEn, BuildingDv = BuildingDv, Floor = Floor, Apartment = Apartment,
    };
}
