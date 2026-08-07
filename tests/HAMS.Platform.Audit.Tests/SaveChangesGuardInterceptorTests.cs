using HAMS.Platform.Audit.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Audit.Tests;

/// <summary>A minimal <see cref="IVersionedRecord{TKey}"/> stand-in — <see cref="IsImmutable"/> is
/// directly settable here (rather than derived from a Status enum) purely so tests can control it precisely.</summary>
public sealed class TestVersionedEntity : IVersionedRecord<Guid>
{
    public Guid Id { get; set; }
    public int Version { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public Guid? SupersedesId { get; set; }
    public Guid? SupersededById { get; set; }
    public bool IsImmutable { get; set; }
    public string Name { get; set; } = "";
}

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestVersionedEntity> Entities => Set<TestVersionedEntity>();
}

public class SaveChangesGuardInterceptorTests
{
    private static TestDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new SaveChangesGuardInterceptor())
            .Options);

    [Fact]
    public async Task Allows_inserting_a_new_row_regardless_of_IsImmutable()
    {
        await using var db = CreateContext();
        db.Entities.Add(new TestVersionedEntity { Id = Guid.NewGuid(), IsImmutable = true, Name = "v1" });

        var exception = await Record.ExceptionAsync(() => db.SaveChangesAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task Allows_the_legitimate_transition_into_immutable()
    {
        await using var db = CreateContext();
        var entity = new TestVersionedEntity { Id = Guid.NewGuid(), IsImmutable = false, Name = "draft" };
        db.Entities.Add(entity);
        await db.SaveChangesAsync();

        entity.IsImmutable = true; // e.g. Draft -> Published

        var exception = await Record.ExceptionAsync(() => db.SaveChangesAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task Blocks_modifying_a_row_that_was_already_immutable()
    {
        await using var db = CreateContext();
        var entity = new TestVersionedEntity { Id = Guid.NewGuid(), IsImmutable = true, Name = "published" };
        db.Entities.Add(entity);
        await db.SaveChangesAsync();

        entity.Name = "tampered";

        await Assert.ThrowsAsync<ImmutableRecordMutationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Blocks_deleting_a_row_that_was_already_immutable()
    {
        await using var db = CreateContext();
        var entity = new TestVersionedEntity { Id = Guid.NewGuid(), IsImmutable = true, Name = "published" };
        db.Entities.Add(entity);
        await db.SaveChangesAsync();

        db.Entities.Remove(entity);

        await Assert.ThrowsAsync<ImmutableRecordMutationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Allows_modifying_a_row_that_has_never_been_immutable()
    {
        await using var db = CreateContext();
        var entity = new TestVersionedEntity { Id = Guid.NewGuid(), IsImmutable = false, Name = "v1" };
        db.Entities.Add(entity);
        await db.SaveChangesAsync();

        entity.Name = "v1-edited";

        var exception = await Record.ExceptionAsync(() => db.SaveChangesAsync());

        Assert.Null(exception);
    }
}
