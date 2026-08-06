using System.Net;
using System.Net.Http.Json;
using Illig_AI_Platform.Client.Models.Plausibilitaetspruefung.Sondermerkmalsuche;

namespace Illig_AI_Platform.Client.Services.Plausibilitaetspruefung.Sondermerkmalsuche;

/// <summary>
/// Kapselt die Aufrufe an den SondermerkmalController (benannter HttpClient „ServerAPI").
/// </summary>
public class SondermerkmalClient(HttpClient http)
{
    /// <summary>Step 1→2. Null, wenn der Auftrag nicht gefunden wurde.</summary>
    public async Task<StuecklisteAnalyse?> AnalyzeAsync(string auftragsnummer)
    {
        // Auftragsnummer als Query-Parameter, nicht als Pfadsegment: eine Linie erzeugt einen
        // Schrägstrich ("11055627 / 40"), der als %2F im Pfad von Kestrel standardmäßig abgewiesen
        // würde. Im Query-String ist %2F unbedenklich.
        var response = await http.GetAsync($"api/v1/sondermerkmal/analyze?auftragsnummer={Uri.EscapeDataString(auftragsnummer)}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StuecklisteAnalyse>();
    }

    /// <summary>Step 2→3: Top-5-Treffer.</summary>
    public async Task<IReadOnlyList<ReferenzTreffer>> SearchAsync(SearchRequest request)
    {
        var response = await http.PostAsJsonAsync("api/v1/sondermerkmal/search", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ReferenzTreffer>>() ?? [];
    }

    /// <summary>Step 3: Detailansicht. Null, wenn nicht gefunden.</summary>
    public async Task<StuecklisteDetail?> GetDetailAsync(string auftragsnummer)
    {
        var response = await http.GetAsync($"api/v1/sondermerkmal/detail/{Uri.EscapeDataString(auftragsnummer)}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StuecklisteDetail>();
    }

    public async Task<ReferenzDokument?> GetDokumentAsync(string auftragsnummer)
    {
        var response = await http.GetAsync(
            $"api/v1/sondermerkmal/detail/{Uri.EscapeDataString(auftragsnummer)}/dokument");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var dateiname = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"{auftragsnummer}.pdf";
        return new ReferenzDokument(dateiname, await response.Content.ReadAsByteArrayAsync());
    }
}
