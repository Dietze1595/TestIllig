using Illig_AI_Platform.Client.Models.Auftragsanlage;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Illig_AI_Platform.Client.Services.Auftragsanlage;

/// <summary>
/// Kapselt die Aufrufe an den AuftragsanlageController (benannter HttpClient „ServerAPI").
/// Uploads laufen über byte[] statt IBrowserFile, weil der Vertrieb dieselbe Datei nach
/// einer Konflikt-Rückfrage erneut senden muss — ein IBrowserFile-Stream ist nur einmal lesbar.
/// </summary>
public class AuftragsanlageClient(HttpClient http)
{
    public const long MaxDateiBytes = 20 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonWeb = new(JsonSerializerDefaults.Web);

    public async Task<AngebotAnalyseAntwort> AnalyseAsync(byte[] inhalt, string dateiname, string contentType)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(inhalt);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        // Feldname muss zum Controller-Parameter [FromForm] IFormFile datei passen.
        content.Add(fileContent, "datei", dateiname);

        var response = await http.PostAsync("api/v1/auftragsanlage/vertrieb/angebot/analyse", content);
        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            throw new ApiRequestException(
                (int)response.StatusCode,
                await AntworttextLesenAsync(response.Content));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AngebotAnalyseAntwort>())!;
    }

    public async Task<AngebotSpeichernAntwort> SpeichernAsync(
        byte[] inhalt, string dateiname, string contentType,
        ExtrahierteAngebotsdaten daten, string? konfliktStrategie = null, string? versionsKommentar = null)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(inhalt);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "datei", dateiname);
        content.Add(new StringContent(JsonSerializer.Serialize(daten, JsonWeb)), "datenJson");
        if (konfliktStrategie is not null)
            content.Add(new StringContent(konfliktStrategie), "konfliktStrategie");
        if (!string.IsNullOrWhiteSpace(versionsKommentar))
            content.Add(new StringContent(versionsKommentar), "versionsKommentar");

        var response = await http.PostAsync("api/v1/auftragsanlage/vertrieb/angebot/speichern", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AngebotSpeichernAntwort>())!;
    }

    public async Task<AngebotFreigabeAntwort> AngebotFreigebenAsync(
        int id, string? kundeKommentar, string? zahlungsbedingungenKommentar,
        string? incotermKommentar, string? versandbedingungKommentar,
        string? zahlungsplanKommentar, string? verkaeuferKommentar,
        string? lieferterminKommentar,
        string? gueltigkeitsdatumKommentar, bool sapFuehrendBestaetigt,
        bool sapSparteBestaetigt, string? lieferadresseKommentar,
        string? sapSparteKommentar = null, string? sapFuehrendKommentar = null)
    {
        var anfrage = new AngebotFreigebenAnfrage(
            kundeKommentar, zahlungsbedingungenKommentar, incotermKommentar, versandbedingungKommentar,
            zahlungsplanKommentar, verkaeuferKommentar, lieferterminKommentar,
            gueltigkeitsdatumKommentar, sapFuehrendBestaetigt, sapSparteBestaetigt,
            lieferadresseKommentar, sapSparteKommentar, sapFuehrendKommentar);

        var response = await http.PostAsJsonAsync($"api/v1/auftragsanlage/vertrieb/angebot/{id}/freigeben", anfrage);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AngebotFreigabeAntwort>())!;
    }

    public async Task<BestaetigungErgebnis> BestaetigungPruefenAsync(byte[] inhalt, string dateiname, string contentType)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(inhalt);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "datei", dateiname);

        var response = await http.PostAsync("api/v1/auftragsanlage/innendienst/bestaetigung", content);

        // 404 = kein zugehöriges Angebot, 422 = falsche Dokumentart. Beides sind erwartete,
        // direkt beim Upload anzeigbare Zustände und keine technischen Fehler.
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound
            or System.Net.HttpStatusCode.UnprocessableEntity)
            return new BestaetigungErgebnis(null, await AntworttextLesenAsync(response.Content));

        response.EnsureSuccessStatusCode();
        return new BestaetigungErgebnis((await response.Content.ReadFromJsonAsync<BestaetigungVergleichAntwort>())!, null);
    }

    public async Task<BestaetigungVergleichAntwort> BestaetigungWechselnAsync(int id, int version)
    {
        var response = await http.PostAsync($"api/v1/auftragsanlage/innendienst/bestaetigung/{id}/wechseln?version={version}", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BestaetigungVergleichAntwort>())!;
    }

    public async Task<List<AngebotUebersicht>> GetBestaetigungenAsync(bool nurMeine)
    {
        var response = await http.GetAsync(
            $"api/v1/auftragsanlage/innendienst/bestaetigungen?nurMeine={nurMeine.ToString().ToLowerInvariant()}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<AngebotUebersicht>>()) ?? [];
    }

    public async Task<BestaetigungDetailAntwort?> GetBestaetigungDetailAsync(int id)
    {
        var response = await http.GetAsync($"api/v1/auftragsanlage/innendienst/bestaetigung/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BestaetigungDetailAntwort>();
    }

    public async Task<byte[]?> GetBestaetigungDokumentAsync(int id)
    {
        var response = await http.GetAsync($"api/v1/auftragsanlage/innendienst/bestaetigung/{id}/dokument");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]?> GetAngebotDokumentAsync(int id)
    {
        var response = await http.GetAsync($"api/v1/auftragsanlage/angebot/{id}/dokument");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<List<AngebotUebersicht>> GetAngeboteAsync(bool nurMeine)
    {
        var response = await http.GetAsync($"api/v1/auftragsanlage/vertrieb/angebote?nurMeine={nurMeine.ToString().ToLowerInvariant()}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<AngebotUebersicht>>()) ?? [];
    }

    public async Task<AngebotDetailAntwort?> GetAngebotDetailAsync(int id)
    {
        var response = await http.GetAsync($"api/v1/auftragsanlage/vertrieb/angebot/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AngebotDetailAntwort>();
    }

    private static async Task<string> AntworttextLesenAsync(HttpContent content)
    {
        var text = await content.ReadAsStringAsync();
        try
        {
            return JsonSerializer.Deserialize<string>(text, JsonWeb) ?? text;
        }
        catch (JsonException)
        {
            return text;
        }
    }
}
