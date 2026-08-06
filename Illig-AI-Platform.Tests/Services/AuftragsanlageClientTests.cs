using System.Net;
using System.Net.Http.Json;
using Illig_AI_Platform.Client.Services;
using Illig_AI_Platform.Client.Services.Auftragsanlage;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class AuftragsanlageClientTests
{
    [Fact]
    public async Task AnalyseAsync_WirftVerstaendlichenHinweisBeiFalscherDokumentart()
    {
        const string meldung = "Kein Angebot erkannt. Bitte lade ein gültiges ILLIG-Angebot hoch.";
        using var http = new HttpClient(new FakeHandler(new HttpResponseMessage(
            HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(meldung)
        }))
        {
            BaseAddress = new Uri("https://localhost/")
        };
        var client = new AuftragsanlageClient(http);

        var exception = await Assert.ThrowsAsync<ApiRequestException>(
            () => client.AnalyseAsync([1], "bestellung.pdf", "application/pdf"));

        Assert.Equal(422, exception.StatusCode);
        Assert.Equal(meldung, exception.Message);
    }

    [Fact]
    public async Task BestaetigungPruefenAsync_LiefertDokumentartHinweisBeiStatus422()
    {
        const string meldung =
            "Das hochgeladene Dokument wurde nicht als Kundenbestellung erkannt. Es handelt sich um ein Angebot.";
        using var http = new HttpClient(new FakeHandler(new HttpResponseMessage(
            HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(meldung)
        }))
        {
            BaseAddress = new Uri("https://localhost/")
        };
        var client = new AuftragsanlageClient(http);

        var result = await client.BestaetigungPruefenAsync(
            [1], "angebot.pdf", "application/pdf");

        Assert.Null(result.Antwort);
        Assert.Equal(meldung, result.HinweisMeldung);
    }

    private sealed class FakeHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
