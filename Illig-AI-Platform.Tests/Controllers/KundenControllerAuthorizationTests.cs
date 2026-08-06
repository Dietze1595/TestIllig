using System.Reflection;
using Illig_AI_Platform.Controllers;
using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Illig_AI_Platform.Tests.Controllers;

public class KundenControllerAuthorizationTests
{
    [Fact]
    public void Kundenbereich_IstMitSuchsystemRolleGeschuetzt()
    {
        var authorize = typeof(KundenController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(AppRoles.SearchSystem, authorize.Roles);
    }

    [Fact]
    public async Task Dokument_LaedtAngebotAusAuftragsanlageSpeicher()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var kunde = new Kunde
        {
            Name = "Muster GmbH",
            NormalisierterName = "MUSTER",
            NormalisierteAdresse = "",
            ErstelltAm = DateTime.UtcNow,
            AktualisiertAm = DateTime.UtcNow,
        };
        db.Kunden.Add(kunde);
        await db.SaveChangesAsync();

        var angebot = new Angebot
        {
            Angebotsnummer = "50001234",
            KundeId = kunde.Id,
            Dateiname = "angebot.pdf",
            BlobPfad = "angebote/angebot.pdf",
            Volltext = "",
            HochgeladenAm = DateTime.UtcNow,
        };
        db.Angebote.Add(angebot);
        await db.SaveChangesAsync();
        db.KundenQuellen.Add(new KundenQuelle
        {
            KundeId = kunde.Id,
            Quelltyp = KundenQuelltyp.Angebot,
            QuellId = angebot.Id,
            ErfasstAm = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var stuecklistenStorage = new FakeBlobStorage();
        var auftragsanlageStorage = new FakeBlobStorage();
        var controller = new KundenController(
            new KundenstammService(db),
            db,
            stuecklistenStorage,
            auftragsanlageStorage)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        var result = await controller.Dokument(
            kunde.Id,
            KundenQuelltyp.Angebot,
            angebot.Id);

        var datei = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("angebot.pdf", datei.FileDownloadName);
        Assert.Equal("angebote/angebot.pdf", auftragsanlageStorage.GeoeffneterBlobPfad);
        Assert.Null(stuecklistenStorage.GeoeffneterBlobPfad);
    }

    private sealed class FakeBlobStorage : IBlobStorageService
    {
        public string? GeoeffneterBlobPfad { get; private set; }

        public Task<string> UploadAsync(
            Stream inhalt,
            string dateiname,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(dateiname);

        public Task<Stream> OpenReadAsync(
            string blobPfad,
            CancellationToken cancellationToken = default)
        {
            GeoeffneterBlobPfad = blobPfad;
            return Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));
        }

        public Task DeleteAsync(
            string blobPfad,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
