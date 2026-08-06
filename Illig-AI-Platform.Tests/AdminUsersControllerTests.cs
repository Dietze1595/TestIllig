using System.Reflection;
using System.Security.Claims;
using Illig_AI_Platform.Controllers;
using Illig_AI_Platform.Services;
using Illig_AI_Platform.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Illig_AI_Platform.Tests;

public class AdminUsersControllerTests
{
    private static AppDbContext NeueDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static AdminUsersController CreateController(
        AppDbContext db, IMemoryCache? cache = null, Guid? currentUserId = null, string? currentUserEmail = null)
    {
        var controller = new AdminUsersController(
            db, cache ?? new MemoryCache(new MemoryCacheOptions()),
            NullLogger<AdminUsersController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var claims = new List<Claim>();
        if (currentUserId is Guid id)
            claims.Add(new Claim("oid", id.ToString()));
        if (currentUserEmail is not null)
            claims.Add(new Claim("emails", currentUserEmail));
        if (claims.Count > 0)
            controller.ControllerContext.HttpContext.User =
                new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        return controller;
    }

    private static UserProfile NeuerNutzer(Guid id, string email, string fullName, params int[] roleIds)
    {
        var nutzer = new UserProfile
        {
            Id = id,
            ObjectId = id.ToString(),
            Email = email,
            FullName = fullName,
            DisplayName = fullName,
            Name = fullName,
            Vorname = fullName
        };
        foreach (var rid in roleIds)
            nutzer.UserProfileRoles.Add(new UserProfileRole { UserProfileId = id, RoleId = rid });
        return nutzer;
    }

    private static int RoleId(string name) => AppRoles.Seed.First(r => r.Name == name).Id;

    [Fact]
    public void KlasseErfordertAdminRolle()
    {
        var attr = typeof(AdminUsersController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal(AppRoles.Admin, attr!.Roles);
    }

    [Fact]
    public async Task GetUsers_LiefertAlleNutzerMitRollennamen()
    {
        await using var db = NeueDb();
        db.UserProfiles.Add(NeuerNutzer(Guid.NewGuid(), "anna@illig.de", "Anna Beispiel",
            RoleId(AppRoles.PlausibilityCheck), RoleId(AppRoles.SearchSystem)));
        db.UserProfiles.Add(NeuerNutzer(Guid.NewGuid(), "bert@illig.de", "Bert Beispiel"));
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.GetUsersAsync();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var liste = Assert.IsAssignableFrom<IReadOnlyList<AdminUsersController.AdminUserResponse>>(ok.Value);
        Assert.Equal(2, liste.Count);

        var anna = liste.Single(u => u.Email == "anna@illig.de");
        Assert.Equal([AppRoles.PlausibilityCheck, AppRoles.SearchSystem], anna.Roles.OrderBy(r => r));

        var bert = liste.Single(u => u.Email == "bert@illig.de");
        Assert.Empty(bert.Roles);
    }

    [Fact]
    public void GetRoles_LiefertAlleSeedRollen()
    {
        using var db = NeueDb();
        var controller = CreateController(db);

        var result = controller.GetRoles();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rollen = Assert.IsAssignableFrom<IReadOnlyList<AdminUsersController.AdminRoleResponse>>(ok.Value);
        Assert.Equal(AppRoles.Seed.Count, rollen.Count);
        Assert.Contains(rollen, r => r.Name == AppRoles.Admin);
        Assert.Contains(rollen, r => r.Name == AppRoles.Dev);
        Assert.Contains(rollen, r => r.Name == AppRoles.SearchSystem);
    }

    [Fact]
    public async Task UpdateRoles_ErsetztRollen_UndInvalidiertCache()
    {
        var zielId = Guid.NewGuid();
        await using var db = NeueDb();
        db.UserProfiles.Add(NeuerNutzer(zielId, "carla@illig.de", "Carla Beispiel", RoleId(AppRoles.PlausibilityCheck)));
        await db.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set(RoleClaimsTransformation.RoleCacheKey(zielId), new List<string> { AppRoles.PlausibilityCheck });

        // Ausführender Admin ist ein anderer Nutzer → Selbstschutz greift nicht.
        var controller = CreateController(db, cache, currentUserId: Guid.NewGuid());

        var neueRollen = new List<int> { RoleId(AppRoles.SearchSystem), RoleId(AppRoles.Lieferantenassistent) };
        var result = await controller.UpdateRolesAsync(zielId, new AdminUsersController.UpdateUserRolesRequest(neueRollen));

        Assert.IsType<OkResult>(result);

        var gespeichert = await db.UserProfileRoles.Where(r => r.UserProfileId == zielId).Select(r => r.RoleId).ToListAsync();
        Assert.Equal(neueRollen.OrderBy(x => x), gespeichert.OrderBy(x => x));
        Assert.False(cache.TryGetValue(RoleClaimsTransformation.RoleCacheKey(zielId), out _));
    }

    [Fact]
    public async Task UpdateRoles_VerbietetSelbstEntzugDerAdminRolle()
    {
        var adminId = Guid.NewGuid();
        await using var db = NeueDb();
        db.UserProfiles.Add(NeuerNutzer(adminId, "admin@illig.de", "Adam Admin", RoleId(AppRoles.Admin)));
        await db.SaveChangesAsync();

        // Der ausführende Admin ist der Zielnutzer und entfernt die Admin-Rolle aus der Auswahl.
        var controller = CreateController(db, currentUserId: adminId);

        var result = await controller.UpdateRolesAsync(adminId,
            new AdminUsersController.UpdateUserRolesRequest([RoleId(AppRoles.SearchSystem)]));

        Assert.IsType<BadRequestObjectResult>(result);
        var nochAdmin = await db.UserProfileRoles.AnyAsync(r => r.UserProfileId == adminId && r.RoleId == RoleId(AppRoles.Admin));
        Assert.True(nochAdmin);
    }

    [Fact]
    public async Task UpdateRoles_VerbietetSelbstEntzugDerDevRolle()
    {
        var devId = Guid.NewGuid();
        await using var db = NeueDb();
        db.UserProfiles.Add(NeuerNutzer(devId, "dev@illig.de", "Daniela Entwicklung", RoleId(AppRoles.Dev)));
        await db.SaveChangesAsync();

        var controller = CreateController(db, currentUserId: devId);

        var result = await controller.UpdateRolesAsync(devId,
            new AdminUsersController.UpdateUserRolesRequest([RoleId(AppRoles.SearchSystem)]));

        Assert.IsType<BadRequestObjectResult>(result);
        var nochDev = await db.UserProfileRoles.AnyAsync(
            r => r.UserProfileId == devId && r.RoleId == RoleId(AppRoles.Dev));
        Assert.True(nochDev);
    }

    [Fact]
    public async Task UpdateRoles_ErlaubtAdminAnderemNutzerZuEntziehen()
    {
        var adminId = Guid.NewGuid();
        var zielId = Guid.NewGuid();
        await using var db = NeueDb();
        db.UserProfiles.Add(NeuerNutzer(zielId, "opfer@illig.de", "Otto Opfer", RoleId(AppRoles.Admin)));
        await db.SaveChangesAsync();

        var controller = CreateController(db, currentUserId: adminId);

        var result = await controller.UpdateRolesAsync(zielId,
            new AdminUsersController.UpdateUserRolesRequest([]));

        Assert.IsType<OkResult>(result);
        Assert.Empty(await db.UserProfileRoles.Where(r => r.UserProfileId == zielId).ToListAsync());
    }

    [Fact]
    public async Task UpdateRoles_VerbietetNichtNovazoonAdmin_BeiNovazoonZielnutzer()
    {
        var zielId = Guid.NewGuid();
        await using var db = NeueDb();
        // Zielnutzer mit Novazoon-Mail, bewusst gemischte Groß-/Kleinschreibung → Vergleich case-insensitiv.
        db.UserProfiles.Add(NeuerNutzer(zielId, "chef@Novazoon.DE", "Nora Novazoon", RoleId(AppRoles.Admin)));
        await db.SaveChangesAsync();

        // Handelnder Admin hat eine @illig.de-Mail → darf Novazoon-Rollen nicht ändern.
        var controller = CreateController(db, currentUserId: Guid.NewGuid(), currentUserEmail: "admin@illig.de");

        var result = await controller.UpdateRolesAsync(zielId,
            new AdminUsersController.UpdateUserRolesRequest([]));

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
        // Admin-Rolle des Novazoon-Nutzers bleibt unangetastet.
        Assert.True(await db.UserProfileRoles.AnyAsync(
            r => r.UserProfileId == zielId && r.RoleId == RoleId(AppRoles.Admin)));
    }

    [Fact]
    public async Task UpdateRoles_ErlaubtNovazoonAdmin_BeiNovazoonZielnutzer()
    {
        var zielId = Guid.NewGuid();
        await using var db = NeueDb();
        db.UserProfiles.Add(NeuerNutzer(zielId, "nora@novazoon.de", "Nora Novazoon", RoleId(AppRoles.Admin)));
        await db.SaveChangesAsync();

        // Handelnder Admin ist selbst Novazoon → darf ändern.
        var controller = CreateController(db, currentUserId: Guid.NewGuid(), currentUserEmail: "marcel@novazoon.de");

        var result = await controller.UpdateRolesAsync(zielId,
            new AdminUsersController.UpdateUserRolesRequest([RoleId(AppRoles.SearchSystem)]));

        Assert.IsType<OkResult>(result);
        Assert.Equal(RoleId(AppRoles.SearchSystem),
            Assert.Single(await db.UserProfileRoles.Where(r => r.UserProfileId == zielId).Select(r => r.RoleId).ToListAsync()));
    }

    [Fact]
    public async Task UpdateRoles_ErlaubtIlligAdmin_BeiIlligZielnutzer()
    {
        var zielId = Guid.NewGuid();
        await using var db = NeueDb();
        db.UserProfiles.Add(NeuerNutzer(zielId, "otto@illig.de", "Otto Opfer", RoleId(AppRoles.Admin)));
        await db.SaveChangesAsync();

        // Nicht-Novazoon-Zielnutzer ist nicht geschützt → illig-Admin darf ändern.
        var controller = CreateController(db, currentUserId: Guid.NewGuid(), currentUserEmail: "admin@illig.de");

        var result = await controller.UpdateRolesAsync(zielId,
            new AdminUsersController.UpdateUserRolesRequest([RoleId(AppRoles.SearchSystem)]));

        Assert.IsType<OkResult>(result);
        Assert.Equal(RoleId(AppRoles.SearchSystem),
            Assert.Single(await db.UserProfileRoles.Where(r => r.UserProfileId == zielId).Select(r => r.RoleId).ToListAsync()));
    }

    [Fact]
    public async Task UpdateRoles_LehntUnbekannteRolleAb()
    {
        var zielId = Guid.NewGuid();
        await using var db = NeueDb();
        db.UserProfiles.Add(NeuerNutzer(zielId, "dora@illig.de", "Dora Beispiel"));
        await db.SaveChangesAsync();

        var controller = CreateController(db, currentUserId: Guid.NewGuid());

        var result = await controller.UpdateRolesAsync(zielId,
            new AdminUsersController.UpdateUserRolesRequest([9999]));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRoles_GibtNotFound_WennNutzerFehlt()
    {
        await using var db = NeueDb();
        var controller = CreateController(db, currentUserId: Guid.NewGuid());

        var result = await controller.UpdateRolesAsync(Guid.NewGuid(),
            new AdminUsersController.UpdateUserRolesRequest([RoleId(AppRoles.SearchSystem)]));

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
