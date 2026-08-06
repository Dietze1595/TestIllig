using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class AngebotsServiceDetailTests
{
    [Fact]
    public async Task DetailAsync_LiefertDenOwnerDesAngebots()
    {
        await using var db = NeueDb();
        var ownerId = Guid.NewGuid();
        var angebot = new Angebot
        {
            Angebotsnummer = "A-2026-010",
            Version = 1,
            Dateiname = "angebot.pdf",
            BlobPfad = "angebote/angebot.pdf",
            Volltext = "Angebot",
            UserProfileId = ownerId,
        };
        db.Angebote.Add(angebot);
        await db.SaveChangesAsync();
        var service = new AngebotsService(db, new FakeBlobStorageService());

        var detail = await service.DetailAsync(angebot.Id);

        Assert.NotNull(detail);
        Assert.Equal(ownerId, detail!.UserProfileId);
    }

    [Fact]
    public async Task DetailAsync_LiefertSapKommentare()
    {
        await using var db = NeueDb();
        var angebot = new Angebot
        {
            Angebotsnummer = "A-2026-011",
            Version = 1,
            Dateiname = "angebot.pdf",
            BlobPfad = "angebote/angebot.pdf",
            Volltext = "Angebot",
            SapSparteKommentar = "Sparte-Kommentar",
            SapFuehrendKommentar = "Führend-Kommentar",
        };
        db.Angebote.Add(angebot);
        await db.SaveChangesAsync();
        var service = new AngebotsService(db, new FakeBlobStorageService());

        var detail = await service.DetailAsync(angebot.Id);

        Assert.NotNull(detail);
        Assert.Equal("Sparte-Kommentar", detail!.SapSparteKommentar);
        Assert.Equal("Führend-Kommentar", detail.SapFuehrendKommentar);
    }

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
