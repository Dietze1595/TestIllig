using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class AngebotsServiceFreigabeTests
{
    [Fact]
    public async Task ErstelleAngebotStatusAsync_LiefertAnzeigenamenDesFreigebendenVertrieblers()
    {
        await using var db = NeueDb();
        var hochgeladenVonId = Guid.NewGuid();
        var freigegebenVonId = Guid.NewGuid();
        db.UserProfiles.AddRange(
            Profil(hochgeladenVonId, "Hochgeladen von"),
            Profil(freigegebenVonId, "Freigegeben von"));
        await db.SaveChangesAsync();

        var angebot = new Angebot
        {
            Angebotsnummer = "A-2026-001",
            UserProfileId = hochgeladenVonId,
            FreigegebenVonUserProfileId = freigegebenVonId,
            Freigegeben = true,
            FreigegebenAm = new DateTime(2026, 7, 28, 8, 38, 0, DateTimeKind.Utc),
        };
        var service = new AngebotsService(db, new FakeBlobStorageService());

        var status = await service.ErstelleAngebotStatusAsync(angebot);

        Assert.Equal("Freigegeben von", status.FreigegebenVon);
        Assert.Equal(angebot.FreigegebenAm, status.FreigegebenAm);
    }

    [Fact]
    public async Task FreigebenAsync_SpeichertSapKommentare()
    {
        await using var db = NeueDb();
        var angebot = new Angebot
        {
            Angebotsnummer = "A-2026-003",
            Version = 1,
            Dateiname = "angebot.pdf",
            BlobPfad = "angebote/angebot.pdf",
            Volltext = "Angebot",
        };
        db.Angebote.Add(angebot);
        await db.SaveChangesAsync();
        var service = new AngebotsService(db, new FakeBlobStorageService());

        await service.FreigebenAsync(
            angebot.Id,
            sapSparteKommentar: "Sparte nach Rücksprache korrekt",
            sapFuehrendKommentar: "Führendes Angebot mündlich bestätigt");

        Assert.Equal("Sparte nach Rücksprache korrekt", angebot.SapSparteKommentar);
        Assert.Equal("Führendes Angebot mündlich bestätigt", angebot.SapFuehrendKommentar);
    }

    [Fact]
    public async Task ErstelleAngebotStatusAsync_LiefertSapKommentare()
    {
        await using var db = NeueDb();
        var angebot = new Angebot
        {
            Angebotsnummer = "A-2026-004",
            SapSparteKommentar = "Sparte-Kommentar",
            SapFuehrendKommentar = "Führend-Kommentar",
        };
        var service = new AngebotsService(db, new FakeBlobStorageService());

        var status = await service.ErstelleAngebotStatusAsync(angebot);

        Assert.Equal("Sparte-Kommentar", status.SapSparteKommentar);
        Assert.Equal("Führend-Kommentar", status.SapFuehrendKommentar);
    }

    [Fact]
    public async Task FreigebenAsync_SpeichertDenTatsaechlichFreigebendenBenutzer()
    {
        await using var db = NeueDb();
        var freigegebenVonId = Guid.NewGuid();
        var angebot = new Angebot
        {
            Angebotsnummer = "A-2026-002",
            Version = 1,
            Dateiname = "angebot.pdf",
            BlobPfad = "angebote/angebot.pdf",
            Volltext = "Angebot",
        };
        db.Angebote.Add(angebot);
        await db.SaveChangesAsync();
        var service = new AngebotsService(db, new FakeBlobStorageService());

        await service.FreigebenAsync(
            angebot.Id,
            freigegebenVonUserProfileId: freigegebenVonId);

        Assert.Equal(freigegebenVonId, angebot.FreigegebenVonUserProfileId);
    }

    private static UserProfile Profil(Guid id, string displayName) =>
        new()
        {
            Id = id,
            ObjectId = id.ToString(),
            Email = $"{id}@example.com",
            FullName = displayName,
            DisplayName = displayName,
        };

    private static AppDbContext NeueDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class FakeBlobStorageService : IBlobStorageService
    {
        public Task<string> UploadAsync(
            Stream inhalt, string dateiname, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream> OpenReadAsync(
            string blobPfad, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string blobPfad, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
