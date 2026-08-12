using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Illig_AI_Platform.Client.Models.Plausibilitaetspruefung.Stuecklistenpruefung;
using Illig_AI_Platform.Client.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace Illig_AI_Platform.Client.Services.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Kapselt die Aufrufe an den StuecklistenpruefungController (benannter HttpClient „ServerAPI").
/// </summary>
public class StuecklistenpruefungClient(HttpClient http)
{
    public const long MaxDateiBytes = 20 * 1024 * 1024;

    // Großzügiger als MaxDateiBytes: Maximalstückliste-Exporte können je Maschinentyp deutlich
    // größer sein als eine einzelne Auftragsinformation (Referenzdatei ~1,2 MB bei RDM 75Kc).
    // Admin-only, seltener Vorgang — kein Grund für eine enge Grenze wie beim Kunden-Upload.
    public const long MaxImportDateiBytes = 50 * 1024 * 1024;

    public async Task<DokumentAnalyseErgebnis> AnalyzeAsync(IBrowserFile datei)
    {
        using var content = new MultipartFormDataContent();
        using var stream = datei.OpenReadStream(MaxDateiBytes);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(datei.ContentType);
        // Feldname muss zum Controller-Parameter [FromForm] IFormFile datei passen.
        content.Add(fileContent, "datei", datei.Name);

        var response = await http.PostAsync("api/v1/stuecklistenpruefung/analyze", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DokumentAnalyseErgebnis>())!;
    }

    public async Task<IReadOnlyList<VerlaufEintragUebersicht>> GetVerlaufAsync(bool nurMeine)
    {
        var response = await http.GetAsync($"api/v1/stuecklistenpruefung/verlauf?nurMeine={nurMeine.ToString().ToLowerInvariant()}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<VerlaufEintragUebersicht>>() ?? [];
    }

    public async Task<VerlaufDetail?> GetVerlaufDetailAsync(int id)
    {
        var response = await http.GetAsync($"api/v1/stuecklistenpruefung/verlauf/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VerlaufDetail>();
    }

    public async Task<VerlaufDetail?> SucheNachAuftragsnummerAsync(string auftragsnummer)
    {
        var response = await http.GetAsync(
            $"api/v1/stuecklistenpruefung/suche?auftragsnummer={Uri.EscapeDataString(auftragsnummer)}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VerlaufDetail>();
    }

    public async Task<byte[]?> GetVerlaufDokumentAsync(int id)
    {
        var response = await http.GetAsync($"api/v1/stuecklistenpruefung/verlauf/{id}/dokument");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<IReadOnlyList<AuftragsdokumentUebersicht>> GetSharePointAuftragsdokumenteAsync()
    {
        var response = await http.GetAsync("api/v1/auftragsinformationen");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AuftragsdokumentUebersicht>>() ?? [];
    }

    public async Task<AuftragsdokumentDetail?> GetSharePointAuftragsdokumentAsync(int id)
    {
        var response = await http.GetAsync($"api/v1/auftragsinformationen/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuftragsdokumentDetail>();
    }

    public async Task<byte[]?> GetSharePointAuftragsdokumentInhaltAsync(int id)
    {
        var response = await http.GetAsync($"api/v1/auftragsinformationen/{id}/dokument");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task VerlaufStandAktualisierenAsync(
        int id,
        int schritt,
        IReadOnlyList<ErkanntesMerkmal> merkmale,
        IReadOnlyList<ErkanntesMerkmal> sonderoptionen,
        StuecklistenKnoten? stueckliste,
        VergleichsErgebnis? vergleichsErgebnis,
        string? sapDateiname = null)
    {
        var response = await http.PutAsJsonAsync(
            $"api/v1/stuecklistenpruefung/verlauf/{id}/stand",
            new
            {
                Schritt = schritt,
                Merkmale = merkmale,
                Sonderoptionen = sonderoptionen,
                Stueckliste = stueckliste,
                VergleichsErgebnis = vergleichsErgebnis,
                SapDateiname = sapDateiname
            });
        response.EnsureSuccessStatusCode();
    }

    public async Task VerlaufNeuEinlesenAsync(
        int id,
        byte[] inhalt,
        string dateiname,
        string contentType,
        DokumentAnalyseErgebnis ergebnis)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(inhalt);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "datei", dateiname);
        content.Add(
            new StringContent(JsonSerializer.Serialize(
                ergebnis with { BestehenderEintrag = null })),
            "ergebnisJson");

        var response = await http.PutAsync(
            $"api/v1/stuecklistenpruefung/verlauf/{id}/neu-einlesen",
            content);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new ApiRequestException(
                (int)response.StatusCode,
                "Der gespeicherte Bearbeitungsstand wurde nicht mehr gefunden. Bitte lade die Historie neu.");
        response.EnsureSuccessStatusCode();
    }

    // Liefert null bei 404 ("kein Import für diesen Maschinentyp hinterlegt") statt zu werfen —
    // das ist ein erwarteter, anzeigbarer Zustand, kein Fehler wie bei EnsureSuccessStatusCode.
    public async Task<StuecklistenKnoten?> AufbauenAsync(string maschinentyp, IReadOnlyList<string> merkmalsnummern)
    {
        var response = await http.PostAsJsonAsync("api/v1/stuecklistenpruefung/aufbauen",
            new { Maschinentyp = maschinentyp, Merkmalsnummern = merkmalsnummern });
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StuecklistenKnoten>();
    }

    // Admin-only, seltener Vorgang (siehe Design-Spec „Import") — kein 404-Sonderfall wie bei
    // AufbauenAsync, ein Fehlschlag hier ist immer ein echter Fehler (falsche Rolle, defekte
    // Datei, Encoding-Problem), kein normal anzeigbarer Zustand.
    public async Task ImportAsync(
        IBrowserFile maximalstueckliste, IBrowserFile umsetzungsmatrix,
        string maschinentypSchluessel, StuecklistenImportFormat format)
    {
        using var content = new MultipartFormDataContent();

        using var txtStream = maximalstueckliste.OpenReadStream(MaxImportDateiBytes);
        using var txtContent = new StreamContent(txtStream);
        txtContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(txtContent, "maximalstueckliste", maximalstueckliste.Name);

        using var xlsxStream = umsetzungsmatrix.OpenReadStream(MaxImportDateiBytes);
        using var xlsxContent = new StreamContent(xlsxStream);
        xlsxContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(xlsxContent, "umsetzungsmatrix", umsetzungsmatrix.Name);

        content.Add(new StringContent(maschinentypSchluessel), "maschinentypSchluessel");
        // Enum-Name muss zu den Server-Werten passen (case-insensitiv geparst):
        // Rdm75Kc/Rdm73k/Rdm76Kb/Rdk80k.
        content.Add(new StringContent(format.ToString()), "format");

        var response = await http.PostAsync("api/v1/stuecklistenpruefung/import", content);
        response.EnsureSuccessStatusCode();
    }

    // Admin-only? Nein — normale PlausibilityCheck-Rolle, seltener Vorgang pro Auftrag,
    // ähnliches Multipart-Muster wie ImportAsync, aber mit einem Text-Formularfeld statt
    // einer zweiten Datei für den bereits im Browser vorhandenen berechneten Baum.
    public async Task<VergleichsErgebnis> VergleichenAsync(
        IBrowserFile sapDatei,
        StuecklistenKnoten unsereStueckliste,
        string maschinentyp)
    {
        using var stream = sapDatei.OpenReadStream(MaxDateiBytes);
        return await VergleichenAsync(stream, sapDatei.Name, unsereStueckliste, maschinentyp);
    }

    public async Task<VergleichsErgebnis> VergleichenAsync(
        byte[] sapDatei,
        string dateiname,
        StuecklistenKnoten unsereStueckliste,
        string maschinentyp)
    {
        using var stream = new MemoryStream(sapDatei, writable: false);
        return await VergleichenAsync(stream, dateiname, unsereStueckliste, maschinentyp);
    }

    private async Task<VergleichsErgebnis> VergleichenAsync(
        Stream stream,
        string dateiname,
        StuecklistenKnoten unsereStueckliste,
        string maschinentyp)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "sapDatei", dateiname);

        content.Add(new StringContent(JsonSerializer.Serialize(unsereStueckliste)), "unsereStuecklisteJson");
        content.Add(new StringContent(maschinentyp), "maschinentyp");

        var response = await http.PostAsync("api/v1/stuecklistenpruefung/vergleichen", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VergleichsErgebnis>())!;
    }
}
