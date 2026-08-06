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

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? CapturedBody;
        public HttpMethod? CapturedMethod;
        public string? CapturedUri;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedMethod = request.Method;
            CapturedUri = request.RequestUri?.AbsoluteUri;
            CapturedBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
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
