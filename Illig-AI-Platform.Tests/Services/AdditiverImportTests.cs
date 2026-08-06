using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Lieferantenassistent;
using Illig_AI_Platform.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class AdditiverImportTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Lieferant Eintrag(int kreditor, string name) =>
        new() { Kreditor = kreditor, Name = name };

    [Fact]
    public async Task ErsetzeProSchluesselAsync_FuegtNeueSchluesselHinzu_OhneAndereZuLoeschen()
    {
        await using var db = CreateDb();
        db.Lieferanten.Add(Eintrag(1, "alt"));
        await db.SaveChangesAsync();

        var ersetzt = await AdditiverImport.ErsetzeProSchluesselAsync(
            db, [Eintrag(2, "neu")], e => e.Kreditor);

        Assert.Equal(0, ersetzt);
        var rows = await db.Lieferanten.ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Kreditor == 1 && r.Name == "alt");
        Assert.Contains(rows, r => r.Kreditor == 2 && r.Name == "neu");
    }

    [Fact]
    public async Task ErsetzeProSchluesselAsync_ErsetztNurZeilenDerGepushtenSchluessel()
    {
        await using var db = CreateDb();
        db.LieferantEmailAdressen.AddRange(
            new LieferantEmailAdresse { AdressNummer = 1, EmailAdresse = "alt-1@test.de" },
            new LieferantEmailAdresse { AdressNummer = 1, EmailAdresse = "alt-2@test.de" },
            new LieferantEmailAdresse { AdressNummer = 2, EmailAdresse = "bleibt@test.de" });
        await db.SaveChangesAsync();

        var ersetzt = await AdditiverImport.ErsetzeProSchluesselAsync(
            db,
            [new LieferantEmailAdresse { AdressNummer = 1, EmailAdresse = "neu@test.de" }],
            e => e.AdressNummer);

        Assert.Equal(2, ersetzt);
        var rows = await db.LieferantEmailAdressen.ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.AdressNummer == 1 && r.EmailAdresse == "neu@test.de");
        Assert.Contains(rows, r => r.AdressNummer == 2 && r.EmailAdresse == "bleibt@test.de");
    }
}
