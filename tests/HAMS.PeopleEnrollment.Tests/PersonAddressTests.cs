using HAMS.PeopleEnrollment.Domain;
using HAMS.PeopleEnrollment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Tests;

public class PersonAddressTests
{
    private static PeopleDbContext CreateContext(string? databaseName = null) => new(
        new DbContextOptionsBuilder<PeopleDbContext>().UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Persists_and_reloads_bilingual_name_and_full_address_correctly()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateContext(databaseName);
        var islandId = Guid.NewGuid();
        db.Islands.Add(new Island { Id = islandId, AtollId = Guid.NewGuid(), Code = "TEST_ISLAND", NameEn = "Test Island" });

        var person = new Person
        {
            Id = Guid.NewGuid(),
            NameEn = "Ahmed Naseer",
            NameDv = "އަހްމަދު ނަސީރު",
            DateOfBirth = new DateOnly(2012, 5, 14),
            Address = new Address
            {
                IslandId = islandId,
                RoadEn = "Coral Way",
                RoadDv = "ކޮރަލް ވޭ",
                HouseNameEn = "Asseyri",
                HouseNameDv = "އަސެއިރި",
                BuildingEn = "Sosun Villa",
                BuildingDv = "ސޯސަން ވިލާ",
                Floor = "3",
                Apartment = "2B",
            },
        };
        db.People.Add(person);
        await db.SaveChangesAsync();

        // Force a reload from a fresh context (same in-memory database) so we're verifying
        // persisted mapping, not just the in-memory graph EF is already tracking.
        await using var reloadDb = CreateContext(databaseName);
        var reloaded = await reloadDb.People.SingleAsync(p => p.Id == person.Id);

        Assert.Equal("Ahmed Naseer", reloaded.NameEn);
        Assert.Equal("އަހްމަދު ނަސީރު", reloaded.NameDv);
        Assert.Equal(islandId, reloaded.Address.IslandId);
        Assert.Equal("Coral Way", reloaded.Address.RoadEn);
        Assert.Equal("ކޮރަލް ވޭ", reloaded.Address.RoadDv);
        Assert.Equal("Asseyri", reloaded.Address.HouseNameEn);
        Assert.Equal("އަސެއިރި", reloaded.Address.HouseNameDv);
        Assert.Equal("Sosun Villa", reloaded.Address.BuildingEn);
        Assert.Equal("3", reloaded.Address.Floor);
        Assert.Equal("2B", reloaded.Address.Apartment);
    }

    [Fact]
    public async Task Building_floor_and_apartment_are_optional()
    {
        await using var db = CreateContext();
        var islandId = Guid.NewGuid();

        var person = new Person
        {
            Id = Guid.NewGuid(),
            NameEn = "Fathimath Shifa",
            NameDv = "ފާތިމަތު ޝިފާ",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Address = new Address
            {
                IslandId = islandId,
                RoadEn = "Bodu Magu",
                RoadDv = "ބޮޑު މަގު",
                HouseNameEn = "Vaadhee",
                HouseNameDv = "ވާދީ",
                BuildingEn = null,
                BuildingDv = null,
                Floor = null,
                Apartment = null,
            },
        };
        db.People.Add(person);

        var exception = await Record.ExceptionAsync(() => db.SaveChangesAsync());

        Assert.Null(exception);
    }
}
