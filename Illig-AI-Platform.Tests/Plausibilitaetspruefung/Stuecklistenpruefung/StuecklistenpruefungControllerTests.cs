using System.Security.Claims;
using Illig_AI_Platform.Controllers.Plausibilitaetspruefung.Stuecklistenpruefung;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class StuecklistenpruefungControllerTests
{
    private class FakeDocumentAnalyseService(DokumentAnalyseErgebnis ergebnis) : IDocumentAnalyseService
    {
        public Task<DokumentAnalyseErgebnis> AnalyzeAsync(Stream pdfStream, CancellationToken cancellationToken = default) =>
            Task.FromResult(ergebnis);
    }

    private class FakeThrowingDocumentAnalyseService : IDocumentAnalyseService
    {
        public Task<DokumentAnalyseErgebnis> AnalyzeAsync(Stream pdfStream, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("irrelevant test message");
    }

    private class FakeBlobStorageService : IBlobStorageService
    {
        public List<string> HochgeladeneDateinamen { get; } = [];

        public Task<string> UploadAsync(Stream inhalt, string dateiname, CancellationToken cancellationToken = default)
        {
            HochgeladeneDateinamen.Add(dateiname);
            return Task.FromResult($"blob/{dateiname}");
        }

        public Task<Stream> OpenReadAsync(string blobPfad, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));

        public Task DeleteAsync(string blobPfad, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // StuecklistenImportService (siehe Import-Test unten) umschließt den Full-Replace-Import mit
    // einer Transaktion — der InMemory-Provider unterstützt keine Transaktionen und würde ohne
    // diese Warnungsunterdrückung mit einer TransactionIgnoredWarning abbrechen.
    private static AppDbContext NeueDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static StuecklistenpruefungController CreateController(
        IDocumentAnalyseService service, StuecklistenpruefungVerlaufService? verlaufService = null,
        StuecklistenAufbauService? aufbauService = null, StuecklistenImportService? importService = null)
    {
        var db = NeueDb();
        var controller = new StuecklistenpruefungController(
            service,
            verlaufService ?? new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService()),
            aufbauService ?? new StuecklistenAufbauService(db),
            importService ?? new StuecklistenImportService(db, NullLogger<StuecklistenImportService>.Instance),
            NullLogger<StuecklistenpruefungController>.Instance)
        {
            // ControllerBase.User greift auf HttpContext.User zu; ohne HttpContext wirft das eine NRE.
            // Ein anonymer DefaultHttpContext liefert ein leeres User-Principal, genau wie in echten
            // anonymen Requests, sodass User.GetUserId() korrekt null zurückgibt.
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    private static void SetUser(StuecklistenpruefungController controller, Guid userId)
    {
        var identity = new ClaimsIdentity([new Claim("oid", userId.ToString())], "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private static DokumentAnalyseErgebnis GueltigesErgebnis() => new(
        "11055627 / 40",
        "708555",
        new DateOnly(2025, 5, 21),
        "RDK 80k_Siemens_konf_ab_01.2013",
        [new ErkanntesMerkmal("40", "9209425", "ILLIG Druckluft-Formungsautomat")],
        []);

    private static DokumentAnalyseErgebnis KeinAuftragsinformationErgebnis() =>
        new("", "", null, "", [], [], IstAuftragsinformation: false);

    // Stream.Null genügt: FakeDocumentAnalyseService liest den Stream-Inhalt nie,
    // FormFile.Length kommt unabhängig vom BaseStream aus dem Konstruktor-Parameter.
    private static IFormFile PdfDatei(long laenge = 1024, string contentType = "application/pdf") =>
        new FormFile(Stream.Null, 0, laenge, "datei", "auftrag.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    [Fact]
    public async Task Analyze_ReturnsBadRequest_WhenFileMissing()
    {
        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()));

        var result = await controller.Analyze(null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Analyze_ReturnsBadRequest_WhenFileTooLarge()
    {
        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()));

        var result = await controller.Analyze(PdfDatei(laenge: 21 * 1024 * 1024));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Analyze_ReturnsBadRequest_WhenContentTypeNotPdf()
    {
        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()));

        var result = await controller.Analyze(PdfDatei(contentType: "image/png"));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Analyze_ReturnsOk_WithErgebnis_WhenValid()
    {
        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()));

        var result = await controller.Analyze(PdfDatei());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DokumentAnalyseErgebnis>(ok.Value);
        Assert.Equal("11055627 / 40", body.Auftragsnummer);
    }

    [Fact]
    public async Task Analyze_MeldetVorhandeneUmsetzungsmatrixFuerMaschinentyp()
    {
        var db = NeueDb();
        db.MaschinentypStuecklisten.Add(new MaschinentypStueckliste
        {
            MaschinentypSchluessel = "RDK 80k",
            Kopfmaterial = "9209307"
        });
        await db.SaveChangesAsync();
        var controller = CreateController(
            new FakeDocumentAnalyseService(GueltigesErgebnis()),
            aufbauService: new StuecklistenAufbauService(db));

        var result = await controller.Analyze(PdfDatei());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DokumentAnalyseErgebnis>(ok.Value);
        Assert.True(body.UmsetzungsmatrixVorhanden);
        Assert.Contains("RDK 80k", body.VerfuegbareUmsetzungsmatrizen!);
    }

    [Fact]
    public async Task Analyze_ReturnsVerlaufId_ForAuthenticatedUser()
    {
        var db = NeueDb();
        var verlaufService = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var controller = CreateController(
            new FakeDocumentAnalyseService(GueltigesErgebnis()),
            verlaufService);
        SetUser(controller, Guid.NewGuid());

        var result = await controller.Analyze(PdfDatei());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DokumentAnalyseErgebnis>(ok.Value);
        Assert.NotNull(body.VerlaufId);
        Assert.Equal(body.VerlaufId, (await db.StuecklistenpruefungVerlaufEintraege.SingleAsync()).Id);
    }

    [Fact]
    public async Task Analyze_ReturnsUnprocessableEntity_WennKeineAuftragsinformation()
    {
        var controller = CreateController(new FakeDocumentAnalyseService(KeinAuftragsinformationErgebnis()));

        var result = await controller.Analyze(PdfDatei());

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
        Assert.Equal(
            "Das hochgeladene Dokument ist keine Auftragsinformation. Bitte die richtige Datei hochladen.",
            unprocessable.Value);
    }

    [Fact]
    public async Task Analyze_ReturnsUnprocessableEntity_WhenAuftragsnummerLeer()
    {
        var leeresErgebnis = new DokumentAnalyseErgebnis("", "", null, "", [], []);
        var controller = CreateController(new FakeDocumentAnalyseService(leeresErgebnis));

        var result = await controller.Analyze(PdfDatei());

        Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
    }

    [Fact]
    public async Task Analyze_ReturnsStatusCode500_AndDoesNotLeakExceptionMessage_WhenServiceThrows()
    {
        var controller = CreateController(new FakeThrowingDocumentAnalyseService());

        var result = await controller.Analyze(PdfDatei());

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.DoesNotContain("irrelevant test message", statusCodeResult.Value?.ToString());
    }

    [Fact]
    public async Task Verlauf_LiefertNurEintraegeDesEingeloggtenUsers()
    {
        var db = NeueDb();
        var verlaufService = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await verlaufService.SpeichernAsync(userA, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());
        await verlaufService.SpeichernAsync(userB, "b.pdf", new MemoryStream([2]), GueltigesErgebnis());

        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()), verlaufService);
        SetUser(controller, userA);

        var result = await controller.Verlauf(nurMeine: true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var liste = Assert.IsAssignableFrom<IReadOnlyList<VerlaufEintragUebersicht>>(ok.Value);
        var eintrag = Assert.Single(liste);
        Assert.Equal("a.pdf", eintrag.Dateiname);
    }

    [Fact]
    public async Task Verlauf_LiefertInAlleAnsichtEintraegeAndererUser()
    {
        var db = NeueDb();
        var verlaufService = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await verlaufService.SpeichernAsync(userA, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());
        await verlaufService.SpeichernAsync(userB, "b.pdf", new MemoryStream([2]), GueltigesErgebnis());

        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()), verlaufService);
        SetUser(controller, userA);

        var result = await controller.Verlauf(nurMeine: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var liste = Assert.IsAssignableFrom<IReadOnlyList<VerlaufEintragUebersicht>>(ok.Value);
        Assert.Equal(2, liste.Count);
    }

    [Fact]
    public async Task VerlaufDetail_ReturnsOk_WhenFremderUser()
    {
        var db = NeueDb();
        var verlaufService = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var eigentuemer = Guid.NewGuid();
        var fremder = Guid.NewGuid();
        var id = await verlaufService.SpeichernAsync(eigentuemer, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());

        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()), verlaufService);
        SetUser(controller, fremder);

        var result = await controller.VerlaufDetail(id);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task VerlaufDetail_ReturnsOk_WhenEigenerUser()
    {
        var db = NeueDb();
        var verlaufService = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var eigentuemer = Guid.NewGuid();
        var id = await verlaufService.SpeichernAsync(eigentuemer, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());
        db.MaschinentypStuecklisten.Add(new MaschinentypStueckliste
        {
            MaschinentypSchluessel = "RDK 80k",
            Kopfmaterial = "9209307"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(
            new FakeDocumentAnalyseService(GueltigesErgebnis()),
            verlaufService,
            new StuecklistenAufbauService(db));
        SetUser(controller, eigentuemer);

        var result = await controller.VerlaufDetail(id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var detail = Assert.IsType<VerlaufDetail>(ok.Value);
        Assert.Equal("11055627 / 40", detail.Auftragsnummer);
        Assert.True(detail.UmsetzungsmatrixVorhanden);
        Assert.Contains("RDK 80k", detail.VerfuegbareUmsetzungsmatrizen!);
    }

    [Fact]
    public async Task VerlaufDokument_ReturnsPdfFile_WhenFremderUser()
    {
        var db = NeueDb();
        var verlaufService = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var eigentuemer = Guid.NewGuid();
        var fremder = Guid.NewGuid();
        var id = await verlaufService.SpeichernAsync(eigentuemer, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());

        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()), verlaufService);
        SetUser(controller, fremder);

        var result = await controller.VerlaufDokument(id);

        Assert.IsType<FileStreamResult>(result);
    }

    [Fact]
    public async Task VerlaufDokument_ReturnsPdfFile_WhenEigenerUser()
    {
        var db = NeueDb();
        var verlaufService = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var eigentuemer = Guid.NewGuid();
        var id = await verlaufService.SpeichernAsync(eigentuemer, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());

        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()), verlaufService);
        SetUser(controller, eigentuemer);

        var result = await controller.VerlaufDokument(id);

        var fileResult = Assert.IsAssignableFrom<FileResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
    }

    [Fact]
    public async Task Aufbauen_ReturnsOk_MitErgebnisBaum()
    {
        var db = NeueDb();
        var stueckliste = new MaschinentypStueckliste { MaschinentypSchluessel = "RDK 80k", Kopfmaterial = "9209307" };
        db.MaschinentypStuecklisten.Add(stueckliste);
        await db.SaveChangesAsync();
        db.MaximalstuecklistenPositionen.Add(new MaximalstuecklistenPosition
        {
            MaschinentypStuecklisteId = stueckliste.Id, Artikelnummer = "9209307", Bezeichnung = "RDK 80k", Menge = 1, Einheit = "ST"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(
            new FakeDocumentAnalyseService(GueltigesErgebnis()), aufbauService: new StuecklistenAufbauService(db));

        var result = await controller.Aufbauen(new AufbauenAnfrage("RDK 80k_Siemens_konf_ab_01.2013", []));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var knoten = Assert.IsType<StuecklistenKnoten>(ok.Value);
        Assert.Equal("9209307", knoten.Artikelnummer);
    }

    [Fact]
    public async Task Aufbauen_ReturnsNotFound_WennKeineStuecklisteFuerMaschinentyp()
    {
        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()));

        var result = await controller.Aufbauen(new AufbauenAnfrage("Unbekannt", []));

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private static IFormFile TextDatei(string inhalt, string dateiname)
    {
        var bytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(inhalt);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "datei", dateiname);
    }

    private static string[] NeueSapZeile()
    {
        var felder = new string[40];
        Array.Fill(felder, string.Empty);
        return felder;
    }

    private static IFormFile MiniUmsetzungsmatrix()
    {
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle_TEST");
        ws.Cell(5, 2).Value = "9209307";
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        return new FormFile(stream, 0, stream.Length, "datei", "matrix.xlsx");
    }

    private static IFormFile MiniRdk80Umsetzungsmatrix()
    {
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.AddWorksheet("Umsetzmatrix_V05_V06");
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        return new FormFile(stream, 0, stream.Length, "datei", "matrix.xlsx");
    }

    [Fact]
    public async Task Import_PersistiertBeideDateien_UndGibtOkZurueck()
    {
        // Minimaler, gültiger Auszug im echten Spaltenformat (Spalte 1 = Wurzeltoken, Spalte 17 = Kurztext).
        var felder = new string[38];
        Array.Fill(felder, "");
        felder[1] = "9209307 0001 1 01";
        felder[17] = "RDK 80k";
        var txtInhalt = string.Join('\t', felder);

        var db = NeueDb();
        var controller = CreateController(
            new FakeDocumentAnalyseService(GueltigesErgebnis()),
            importService: new StuecklistenImportService(db, NullLogger<StuecklistenImportService>.Instance));

        var result = await controller.Import(TextDatei(txtInhalt, "max.txt"), MiniUmsetzungsmatrix(), "RDK 80k", "Rdm75Kc");

        Assert.IsType<OkResult>(result);
        var gespeichert = Assert.Single(await db.MaschinentypStuecklisten.ToListAsync());
        Assert.Equal("RDK 80k", gespeichert.MaschinentypSchluessel);
    }

    [Fact]
    public async Task Import_MitRdk80Format_PersistiertRdk80Wurzel()
    {
        var felder = new string[40];
        Array.Fill(felder, "");
        felder[1] = "9209425 0001 1 01";
        felder[19] = "RDK 80k_Siemens_konf";
        var txtInhalt = string.Join('\t', felder);

        var db = NeueDb();
        var controller = CreateController(
            new FakeDocumentAnalyseService(GueltigesErgebnis()),
            importService: new StuecklistenImportService(db, NullLogger<StuecklistenImportService>.Instance));

        var result = await controller.Import(
            TextDatei(txtInhalt, "max.txt"), MiniRdk80Umsetzungsmatrix(), "RDK 80k", "Rdk80k");

        Assert.IsType<OkResult>(result);
        var gespeichert = Assert.Single(await db.MaschinentypStuecklisten.ToListAsync());
        Assert.Equal("RDK 80k", gespeichert.MaschinentypSchluessel);
        Assert.Equal("9209425", gespeichert.Kopfmaterial);
    }

    [Fact]
    public async Task Import_MitUngueltigemFormat_GibtBadRequest()
    {
        var felder = new string[38];
        Array.Fill(felder, "");
        felder[1] = "9209307 0001 1 01";
        felder[17] = "RDK 80k";
        var txtInhalt = string.Join('\t', felder);

        var db = NeueDb();
        var controller = CreateController(
            new FakeDocumentAnalyseService(GueltigesErgebnis()),
            importService: new StuecklistenImportService(db, NullLogger<StuecklistenImportService>.Instance));

        var result = await controller.Import(
            TextDatei(txtInhalt, "max.txt"), MiniUmsetzungsmatrix(), "RDK 80k", "GibtEsNicht");

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.MaschinentypStuecklisten.ToListAsync());
    }

    [Fact]
    public async Task Vergleichen_ReturnsOk_MitVergleichsErgebnis()
    {
        var felder = new string[38];
        Array.Fill(felder, "");
        felder[1] = "9209307 0001 1 01";
        felder[17] = "Teil A";
        var txtInhalt = string.Join('\t', felder);

        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()));
        var unsereStueckliste = new StuecklistenKnoten("9209307", "Teil A", 1, "ST", []);
        var json = System.Text.Json.JsonSerializer.Serialize(unsereStueckliste);

        var result = await controller.Vergleichen(TextDatei(txtInhalt, "sap.txt"), json, "RDM 75Kc");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var ergebnis = Assert.IsType<VergleichsErgebnis>(ok.Value);
        Assert.Equal("9209307", ergebnis.Wurzel.Artikelnummer);
    }

    [Fact]
    public async Task Vergleichen_ReturnsStatusCode500_WennStuecklisteJsonUngueltig()
    {
        var felder = new string[38];
        Array.Fill(felder, "");
        felder[1] = "9209307 0001 1 01";
        felder[17] = "Teil A";
        var txtInhalt = string.Join('\t', felder);

        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()));

        var result = await controller.Vergleichen(TextDatei(txtInhalt, "sap.txt"), "kein-json", "RDM 75Kc");

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.DoesNotContain("JsonException", statusCodeResult.Value?.ToString());
    }

    [Fact]
    public async Task Vergleichen_MitRdk80Maschinentyp_LiestRdk80Kindknoten()
    {
        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()));
        var wurzelFelder = NeueSapZeile();
        wurzelFelder[1] = "11055895 / 40 9209425 1";
        wurzelFelder[19] = "RDK 80k";
        var kindFelder = NeueSapZeile();
        kindFelder[2] = "0100 L 9209711";
        kindFelder[19] = "Formmaschine";
        kindFelder[20] = "1,000";
        kindFelder[24] = "ST";
        var sap = string.Join('\n', string.Join('\t', wurzelFelder), string.Join('\t', kindFelder));
        var soll = new StuecklistenKnoten(
            "9209425", "RDK 80k", 0, "",
            [new StuecklistenKnoten("9209711", "Formmaschine", 1, "ST", [])]);

        var result = await controller.Vergleichen(
            TextDatei(sap, "sap.txt"),
            System.Text.Json.JsonSerializer.Serialize(soll),
            "RDK 80k_Siemens_konf_ab_01.2013");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var vergleich = Assert.IsType<VergleichsErgebnis>(ok.Value);
        Assert.Equal(VergleichsStatus.Uebereinstimmung, Assert.Single(vergleich.Wurzel.Kinder).Status);
    }

    [Fact]
    public async Task Vergleichen_MitUnbekanntemMaschinentyp_GibtBadRequest()
    {
        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()));
        var result = await controller.Vergleichen(
            TextDatei("inhalt", "sap.txt"),
            System.Text.Json.JsonSerializer.Serialize(
                new StuecklistenKnoten("1", "", 0, "", [])),
            "Unbekannt");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Analyze_Duplikat_LegtNichtsNeuesAn_UndLiefertBestehenden()
    {
        var db = NeueDb();
        var blob = new FakeBlobStorageService();
        var verlaufService = new StuecklistenpruefungVerlaufService(db, blob);
        var userId = Guid.NewGuid();
        var vorhandeneId = await verlaufService.SpeichernAsync(userId, "vorhanden.pdf", new MemoryStream([1]), GueltigesErgebnis());

        var controller = CreateController(new FakeDocumentAnalyseService(GueltigesErgebnis()), verlaufService);
        SetUser(controller, userId);

        var result = await controller.Analyze(PdfDatei());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DokumentAnalyseErgebnis>(ok.Value);
        Assert.Equal(vorhandeneId, body.VerlaufId);
        Assert.NotNull(body.BestehenderEintrag);
        Assert.Equal(vorhandeneId, body.BestehenderEintrag!.Id);
        Assert.Equal(1, await db.StuecklistenpruefungVerlaufEintraege.CountAsync());  // nichts Neues
        Assert.Single(blob.HochgeladeneDateinamen);                                   // kein zweiter Upload
    }

    [Fact]
    public async Task VerlaufNeuEinlesen_ErsetztEigenenGespeichertenStand()
    {
        var db = NeueDb();
        var blob = new FakeBlobStorageService();
        var verlaufService = new StuecklistenpruefungVerlaufService(db, blob);
        var userId = Guid.NewGuid();
        var id = await verlaufService.SpeichernAsync(
            userId, "vorhanden.pdf", new MemoryStream([1]), GueltigesErgebnis());
        var frisch = GueltigesErgebnis() with
        {
            Merkmale = [new ErkanntesMerkmal("40/140", "012516", "Temperaturfühler")]
        };
        var controller = CreateController(new FakeDocumentAnalyseService(frisch), verlaufService);
        SetUser(controller, userId);

        var result = await controller.VerlaufNeuEinlesen(
            id,
            PdfDatei(),
            System.Text.Json.JsonSerializer.Serialize(frisch));

        Assert.IsType<NoContentResult>(result);
        var detail = await verlaufService.DetailAsync(userId, id);
        Assert.Equal("012516", Assert.Single(detail!.Merkmale).Merkmalsnummer);
        Assert.Equal(2, detail.ErreichterSchritt);
        Assert.Equal(2, blob.HochgeladeneDateinamen.Count);
    }
}
