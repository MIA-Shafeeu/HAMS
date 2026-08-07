using HAMS.PeopleEnrollment.Domain;

namespace HAMS.WebHost.Components.Pages.Admin.Shared;

/// <summary>
/// Backing model for <c>PersonFieldsEditor.razor</c> — the same Name/Address field set is needed
/// verbatim on the Student/Staff/Guardian creation forms (a person is created fresh alongside
/// whichever role profile attaches to them), so this is shared rather than tripled.
/// </summary>
public sealed class PersonFormModel
{
    public string NameEn { get; set; } = "";
    public string NameDv { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
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

    public Address ToAddress() => new()
    {
        IslandId = IslandId, RoadEn = RoadEn, RoadDv = RoadDv,
        HouseNameEn = HouseNameEn, HouseNameDv = HouseNameDv,
        BuildingEn = BuildingEn, BuildingDv = BuildingDv, Floor = Floor, Apartment = Apartment,
    };
}
