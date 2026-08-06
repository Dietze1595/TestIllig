using System.Net.Http.Json;
using Illig_AI_Platform.Client.Models.Lieferantenassistent;

namespace Illig_AI_Platform.Client.Services.Lieferantenassistent;

/// <summary>Kapselt die Aufrufe an den LieferantenassistentController (benannter HttpClient „ServerAPI").</summary>
public class LieferantenassistentClient(HttpClient http)
{
    public async Task<IReadOnlyList<OffenePositionAnsicht>> GetPositionenAsync(
        string? suche, IReadOnlyList<LieferterminStatus>? status, IReadOnlyList<string>? einkaeufergruppen)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(suche))
            query.Add($"suche={Uri.EscapeDataString(suche)}");
        if (status is not null)
            query.AddRange(status.Select(s => $"status={s}"));
        if (einkaeufergruppen is not null)
            query.AddRange(einkaeufergruppen.Select(g => $"einkaeufergruppen={Uri.EscapeDataString(g)}"));

        var url = "api/v1/lieferantenassistent/positionen" + (query.Count > 0 ? "?" + string.Join('&', query) : "");
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<OffenePositionAnsicht>>() ?? [];
    }

    public async Task<IReadOnlyList<string>> GetEinkaeufergruppenAsync()
    {
        var response = await http.GetAsync("api/v1/lieferantenassistent/einkaeufergruppen");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<string>>() ?? [];
    }
}
