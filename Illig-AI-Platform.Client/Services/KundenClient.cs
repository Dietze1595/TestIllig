using System.Net;
using System.Net.Http.Json;
using Illig_AI_Platform.Client.Models.Kunden;

namespace Illig_AI_Platform.Client.Services;

public sealed class KundenClient(HttpClient http)
{
    public async Task<IReadOnlyList<KundenUebersicht>> ListeAsync(string? suche = null)
    {
        var query = string.IsNullOrWhiteSpace(suche)
            ? ""
            : $"?suche={Uri.EscapeDataString(suche.Trim())}";
        return await http.GetFromJsonAsync<List<KundenUebersicht>>($"api/v1/kunden{query}") ?? [];
    }

    public async Task<KundenDetail?> DetailAsync(int id)
    {
        var response = await http.GetAsync($"api/v1/kunden/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KundenDetail>();
    }

    public async Task<byte[]?> DokumentAsync(int kundeId, KundenQuelltyp quelltyp, int quellId)
    {
        var response = await http.GetAsync(
            $"api/v1/kunden/{kundeId}/dokument/{(int)quelltyp}/{quellId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
