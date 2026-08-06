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

public class BestelldokumentPruefungTests
{
    [Fact]
    public async Task BestaetigungHochladen_StopptErkanntesAngebotVorSpeicherung()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        db.Angebote.Add(new Angebot
        {
            Angebotsnummer = "A-4711",
            Version = 1,
            Volltext = "Angebot A-4711",
            Dateiname = "angebot.pdf",
            BlobPfad = "blob/angebot",
            HochgeladenAm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var analysierteDaten = new ExtrahierteAngebotsdaten(
            "A-4711", null, null, null, [], "Angebot A-4711 – gültig bis 31.12.2026");
        var vergleich = new AngebotsVergleichLlmErgebnis(
            null, null, false, [], [],
            IstBestelldokument: false,
            DokumentartHinweis: "Das Dokument ist ein Angebot ohne erkennbare Bestellung oder Annahme.");
        var controller = new AuftragsanlageController(
            new FakeAnalyseService(analysierteDaten),
            new AngebotsService(db, new FakeBlobStorageService()),
            new FakeVergleichService(vergleich),
            new NichtKonfigurierterVersandartLlmService(),
            NullLogger<AuftragsanlageController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var datei = new FormFile(new MemoryStream([1]), 0, 1, "datei", "falsches-dokument.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var result = await controller.BestaetigungHochladen(datei);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
        Assert.Contains("nicht als Kundenbestellung erkannt", unprocessable.Value?.ToString());
        Assert.Contains("Angebot ohne erkennbare Bestellung", unprocessable.Value?.ToString());
        Assert.Empty(await db.Auftragsbestaetigungen.ToListAsync());
    }

    private sealed class FakeAnalyseService(ExtrahierteAngebotsdaten daten)
        : IAngebotsdokumentAnalyseService
    {
        public Task<ExtrahierteAngebotsdaten> AnalyzeAsync(
            Stream pdfStream, CancellationToken cancellationToken = default) =>
            Task.FromResult(daten);
    }

    private sealed class FakeVergleichService(AngebotsVergleichLlmErgebnis ergebnis)
        : IAngebotsvergleichLlmService
    {
        public Task<AngebotsVergleichLlmErgebnis> VergleicheAsync(
            string angebotVolltext,
            string bestaetigungVolltext,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ergebnis);
    }

    private sealed class FakeBlobStorageService : IBlobStorageService
    {
        public Task<string> UploadAsync(
            Stream inhalt, string dateiname, CancellationToken cancellationToken = default) =>
            Task.FromResult($"blob/{dateiname}");

        public Task<Stream> OpenReadAsync(
            string blobPfad, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream([1]));

        public Task DeleteAsync(
            string blobPfad, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
