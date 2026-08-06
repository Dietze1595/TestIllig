using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class UserProfileServiceTests
{
    private static AppDbContext CreateDb()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        db.Database.EnsureCreated(); // nötig, damit die per HasData geseedeten Rollen im InMemory-Provider existieren
        return db;
    }

    [Fact]
    public async Task EnsureProfileCreatedAsync_CreatesProfile_WhenNotExists()
    {
        await using var db = CreateDb();
        var service = new UserProfileService(db);
        var id = Guid.NewGuid();

        await service.EnsureProfileCreatedAsync(id, "a@b.com", "Max Müller", "Müller", "Max");

        var profile = await db.UserProfiles.SingleAsync();
        Assert.Equal(id, profile.Id);
        Assert.Equal(id.ToString(), profile.ObjectId);
        Assert.Equal("a@b.com", profile.Email);
        Assert.Equal("Max Müller", profile.DisplayName);
        Assert.Equal("Müller", profile.Name);
        Assert.Equal("Max", profile.Vorname);
        Assert.Equal("Max Müller", profile.FullName);
        Assert.True(profile.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task EnsureProfileCreatedAsync_DoesNotUpdate_WhenAlreadyExists()
    {
        await using var db = CreateDb();
        var service = new UserProfileService(db);
        var id = Guid.NewGuid();

        await service.EnsureProfileCreatedAsync(id, "a@b.com", "Max Müller", "Müller", "Max");
        await service.EnsureProfileCreatedAsync(id, "other@b.com", "Andere Person", "Person", "Andere");

        Assert.Equal(1, await db.UserProfiles.CountAsync());
        Assert.Equal("a@b.com", (await db.UserProfiles.SingleAsync()).Email);
    }

    [Fact]
    public async Task EnsureProfileCreatedAsync_CreatesTwoProfiles_ForDifferentIds()
    {
        await using var db = CreateDb();
        var service = new UserProfileService(db);

        await service.EnsureProfileCreatedAsync(Guid.NewGuid(), "a@b.com", "User A", "A", "User");
        await service.EnsureProfileCreatedAsync(Guid.NewGuid(), "b@b.com", "User B", "B", "User");

        Assert.Equal(2, await db.UserProfiles.CountAsync());
    }

    [Fact]
    public async Task EnsureProfileCreatedAsync_CreatesNewProfileWithoutRoles()
    {
        await using var db = CreateDb();
        var service = new UserProfileService(db);
        var id = Guid.NewGuid();

        await service.EnsureProfileCreatedAsync(id, "a@b.com", "Max Müller", "Müller", "Max");

        Assert.Empty(await db.UserProfileRoles
            .Where(upr => upr.UserProfileId == id)
            .ToListAsync());
    }

    [Fact]
    public async Task EnsureProfileCreatedAsync_DoesNotCreateRoles_WhenAlreadyExists()
    {
        await using var db = CreateDb();
        var service = new UserProfileService(db);
        var id = Guid.NewGuid();

        await service.EnsureProfileCreatedAsync(id, "a@b.com", "Max Müller", "Müller", "Max");
        await service.EnsureProfileCreatedAsync(id, "other@b.com", "Andere Person", "Person", "Andere");

        Assert.Equal(0, await db.UserProfileRoles.CountAsync(upr => upr.UserProfileId == id));
    }
}
