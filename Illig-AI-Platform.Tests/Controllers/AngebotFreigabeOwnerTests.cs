using System.Security.Claims;
using Illig_AI_Platform.Controllers.Auftragsanlage;
using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Illig_AI_Platform.Tests.Controllers;

public class AngebotFreigabeOwnerTests
{
    [Fact]
    public async Task AngebotFreigeben_DurchFremdenBenutzer_Liefert403()
    {
        await using var db = NeueDb();
        var ownerId = Guid.NewGuid();
        var fremderId = Guid.NewGuid();
        var angebot = GespeichertesAngebot(ownerId);
        db.Angebote.Add(angebot);
        await db.SaveChangesAsync();

        var controller = Controller(db, fremderId);

        var result = await controller.AngebotFreigeben(angebot.Id, VollstaendigeAnfrage());

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task AngebotFreigeben_DurchOwner_GibtFreigabeZurueck()
    {
        await using var db = NeueDb();
        var ownerId = Guid.NewGuid();
        var angebot = GespeichertesAngebot(ownerId);
        db.Angebote.Add(angebot);
        await db.SaveChangesAsync();

        var controller = Controller(db, ownerId);

        var result = await controller.AngebotFreigeben(angebot.Id, VollstaendigeAnfrage());

        Assert.IsType<OkObjectResult>(result.Result);
        var gespeichert = await db.Angebote.FindAsync(angebot.Id);
        Assert.True(gespeichert!.Freigegeben);
    }

    [Fact]
    public async Task AngebotFreigeben_OhneOwnerAmAngebot_Liefert403()
    {
        await using var db = NeueDb();
        var angebot = GespeichertesAngebot(null);
        db.Angebote.Add(angebot);
        await db.SaveChangesAsync();

        var controller = Controller(db, Guid.NewGuid());

        var result = await controller.AngebotFreigeben(angebot.Id, VollstaendigeAnfrage());

        Assert.IsType<ForbidResult>(result.Result);
    }

    // Alle Prüfpunkte sind bei einem leeren Angebot "nicht erfüllt", daher müssen alle
    // Kommentare gesetzt und beide SAP-Bestätigungen bejaht sein, damit nicht die
    // Checklisten-Validierung (400) vor der Owner-Prüfung greift.
    private static AngebotFreigebenAnfrage VollstaendigeAnfrage() =>
        new(
            KundeKommentar: "ok",
            ZahlungsbedingungenKommentar: "ok",
            IncotermKommentar: "ok",
            VersandbedingungKommentar: "ok",
            ZahlungsplanKommentar: "ok",
            VerkaeuferKommentar: "ok",
            LieferterminKommentar: "ok",
            GueltigkeitsdatumKommentar: "ok",
            SapFuehrendBestaetigt: true,
            SapSparteBestaetigt: true,
            LieferadresseKommentar: "ok");

    private static Angebot GespeichertesAngebot(Guid? ownerId) =>
        new()
        {
            Angebotsnummer = "A-2026-020",
            Version = 1,
            Dateiname = "angebot.pdf",
            BlobPfad = "angebote/angebot.pdf",
            Volltext = "Angebot",
            UserProfileId = ownerId,
        };

    private static AuftragsanlageController Controller(AppDbContext db, Guid benutzerId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("oid", benutzerId.ToString())], "test")),
        };

        return new AuftragsanlageController(
            new FakeAnalyseService(),
            new AngebotsService(db, new FakeBlobStorageService()),
            new FakeVergleichService(),
            new NichtKonfigurierterVersandartLlmService(),
            NullLogger<AuftragsanlageController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    private static AppDbContext NeueDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class FakeAnalyseService : IAngebotsdokumentAnalyseService
    {
        public Task<ExtrahierteAngebotsdaten> AnalyzeAsync(
            Stream pdfStream, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeVergleichService : IAngebotsvergleichLlmService
    {
        public Task<AngebotsVergleichLlmErgebnis> VergleicheAsync(
            string angebotVolltext, string bestaetigungVolltext,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

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
            Task.CompletedTask;
    }
}
