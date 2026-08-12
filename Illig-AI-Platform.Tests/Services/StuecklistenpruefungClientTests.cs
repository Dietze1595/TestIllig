using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Illig_AI_Platform.Client.Models.Plausibilitaetspruefung.Stuecklistenpruefung;
using Illig_AI_Platform.Client.Services.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class StuecklistenpruefungClientTests
{
    [Fact]
    public async Task VerlaufNeuEinlesenAsync_SendetPdfUndFrischeAnalyseAnErsetzEndpunkt()
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var client = new StuecklistenpruefungClient(http);
        var ergebnis = new DokumentAnalyseErgebnis(
            "11055894 / 40", "717220", null, "RDM 75Kc",
            [new ErkanntesMerkmal("40/80", "017364", "Unterheizung")], []);

        await client.VerlaufNeuEinlesenAsync(
            42, [1, 2, 3], "auftrag-neu.pdf", "application/pdf", ergebnis);

        Assert.Equal(HttpMethod.Put, handler.CapturedMethod);
        Assert.EndsWith("/api/v1/stuecklistenpruefung/verlauf/42/neu-einlesen", handler.CapturedUri);
        Assert.Matches("name=\"?datei\"?", handler.CapturedBody!);
        Assert.Matches("filename=\"?auftrag-neu\\.pdf\"?", handler.CapturedBody!);
        Assert.Matches("name=\"?ergebnisJson\"?", handler.CapturedBody!);
        Assert.Contains("017364", handler.CapturedBody);
    }

    [Fact]
    public async Task ImportAsync_SendetGewaehltesFormatAlsFormularfeld()
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var client = new StuecklistenpruefungClient(http);

        await client.ImportAsync(
            new FakeBrowserFile("max.txt", "inhalt"),
            new FakeBrowserFile("matrix.xlsx", "xlsx"),
            "RDM 76Kb",
            StuecklistenImportFormat.Rdm76Kb);

        var body = handler.CapturedBody!;
        // Das Format muss als Formularfeld "format" mit dem exakten Enum-Namen (Server-kompatibel) ankommen.
        Assert.Matches("name=\"?format\"?", body);
        Assert.Contains("Rdm76Kb", body);
    }

    [Fact]
    public async Task ImportAsync_SendetRdk80Format_UndZeigtPassendeBezeichnung()
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var client = new StuecklistenpruefungClient(http);

        await client.ImportAsync(
            new FakeBrowserFile("max.txt", "inhalt"),
            new FakeBrowserFile("matrix.xlsx", "xlsx"),
            "RDK 80k",
            StuecklistenImportFormat.Rdk80k);

        Assert.Contains("Rdk80k", handler.CapturedBody);
        Assert.Equal("RDK 80k", StuecklistenImportFormatInfo.Bezeichnung(StuecklistenImportFormat.Rdk80k));
    }

    [Fact]
    public async Task VergleichenAsync_SendetErkanntenMaschinentyp()
    {
        var handler = new CapturingHandler
        {
            ResponseBody = """
                {"wurzel":{"artikelnummer":"9209425","bezeichnung":"RDK 80k","unsereMenge":0,"unsereEinheit":"","sapMenge":0,"sapEinheit":"","status":0,"hinweis":null,"kinder":[]},"nurInSap":[]}
                """
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var client = new StuecklistenpruefungClient(http);
        var soll = new StuecklistenKnoten("9209425", "RDK 80k", 0, "", []);

        await client.VergleichenAsync(
            Encoding.UTF8.GetBytes("sap"),
            "auftrag.txt",
            soll,
            "RDK 80k_Siemens_konf_ab_01.2013");

        Assert.Matches("name=\"?maschinentyp\"?", handler.CapturedBody!);
        Assert.Contains("RDK 80k_Siemens_konf_ab_01.2013", handler.CapturedBody);
    }

    [Fact]
    public async Task SucheNachAuftragsnummerAsync_DeserialisiertNumerischeQuelle()
    {
        // Der Server serialisiert das Enum AuftragsdokumentQuelle als Zahl (Standard System.Text.Json).
        // Das Client-DTO muss die Zahl in sein Quelle-Enum lesen können (nicht als string erwarten).
        var handler = new CapturingHandler
        {
            ResponseBody = """
                {"id":1,"dateiname":"a.pdf","quelle":1,"auftragsnummer":"11055894 / 40","kundennummer":"717220","datum":null,"maschinentyp":"RDM 75Kc","merkmale":[],"sonderoptionen":[],"erreichterSchritt":2,"stueckliste":null,"vergleichsErgebnis":null,"sapDateiname":null}
                """
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var client = new StuecklistenpruefungClient(http);

        var detail = await client.SucheNachAuftragsnummerAsync("11055894 / 40");

        Assert.NotNull(detail);
        Assert.Equal(AuftragsdokumentQuelle.SharePoint, detail!.Quelle);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? CapturedBody;
        public HttpMethod? CapturedMethod;
        public string? CapturedUri;
        public string? ResponseBody { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedMethod = request.Method;
            CapturedUri = request.RequestUri?.AbsoluteUri;
            CapturedBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (ResponseBody is not null)
            {
                response.Content = new StringContent(
                    ResponseBody,
                    Encoding.UTF8,
                    "application/json");
            }

            return response;
        }
    }

    private sealed class FakeBrowserFile(string name, string inhalt) : IBrowserFile
    {
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public long Size => Encoding.UTF8.GetByteCount(inhalt);
        public string ContentType => "application/octet-stream";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => new MemoryStream(Encoding.UTF8.GetBytes(inhalt));
    }
}
