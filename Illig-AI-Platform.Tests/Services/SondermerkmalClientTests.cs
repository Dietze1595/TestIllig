using System.Net;
using Illig_AI_Platform.Client.Services.Plausibilitaetspruefung.Sondermerkmalsuche;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class SondermerkmalClientTests
{
    [Fact]
    public async Task AnalyzeAsync_UebergibtAuftragsnummerMitLinieAlsQueryParameter_NichtImPfad()
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var client = new SondermerkmalClient(http);

        await client.AnalyzeAsync("11055627 / 40");

        var uri = new Uri(handler.CapturedUri!);
        // Der Schrägstrich der Linie darf NICHT als %2F im Pfad landen: Kestrel weist encodierte
        // Slashes im Pfad standardmäßig ab (die Anfrage erreicht die Action nie → 400/404). Die
        // Auftragsnummer muss deshalb im Query-String stehen, wo %2F unbedenklich ist.
        Assert.Equal("/api/v1/sondermerkmal/analyze", uri.AbsolutePath);
        Assert.StartsWith("?auftragsnummer=", uri.Query);
        Assert.Equal("11055627 / 40", Uri.UnescapeDataString(uri.Query["?auftragsnummer=".Length..]));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? CapturedUri;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedUri = request.RequestUri?.AbsoluteUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
