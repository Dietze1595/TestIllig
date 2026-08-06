using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Options;

namespace Illig_AI_Platform.Services.Auftragsinformationen;

public sealed class GraphSharePointDokumentClient(
    HttpClient http,
    TokenCredential credential,
    IOptions<SharePointAuftragsinformationenOptions> options) : ISharePointDokumentClient
{
    private static readonly TokenRequestContext GraphTokenContext =
        new(["https://graph.microsoft.com/.default"]);

    private readonly SharePointAuftragsinformationenOptions _options = options.Value;

    public async Task<SharePointQuelle> QuelleAufloesenAsync(CancellationToken cancellationToken = default)
    {
        KonfigurationPruefen();

        if (!string.IsNullOrWhiteSpace(_options.DriveId)
            && !string.IsNullOrWhiteSpace(_options.PermissionRootItemId))
        {
            if (string.IsNullOrWhiteSpace(_options.SyncSubfolderPath))
                return new SharePointQuelle(_options.DriveId, _options.PermissionRootItemId);

            // Der App ist nur der freigegebene Elternordner bekannt. Unterordner werden relativ
            // zu dessen stabiler Item-ID adressiert; eine Site-/Bibliotheks-Auflistung ist damit
            // weder erforderlich noch aufgrund der engen Selected-Permission moeglich.
            var syncFolderUrl =
                $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(_options.DriveId)}/items/{Uri.EscapeDataString(_options.PermissionRootItemId)}:{PfadKodieren(_options.SyncSubfolderPath)}?$select=id";
            using var syncFolderJson = await HoleJsonAsync(syncFolderUrl, cancellationToken);
            return new SharePointQuelle(
                _options.DriveId,
                PflichtString(syncFolderJson.RootElement, "id"));
        }

        var siteUrl = $"https://graph.microsoft.com/v1.0/sites/{Uri.EscapeDataString(_options.HostName)}:{PfadKodieren(_options.SitePath)}";
        using var siteJson = await HoleJsonAsync(siteUrl, cancellationToken);
        var siteId = PflichtString(siteJson.RootElement, "id");

        string? driveId = null;
        string? drivesUrl = $"https://graph.microsoft.com/v1.0/sites/{Uri.EscapeDataString(siteId)}/drives?$select=id,name";
        while (drivesUrl is not null && driveId is null)
        {
            using var drivesJson = await HoleJsonAsync(drivesUrl, cancellationToken);
            foreach (var drive in drivesJson.RootElement.GetProperty("value").EnumerateArray())
            {
                if (string.Equals(PflichtString(drive, "name"), _options.LibraryName, StringComparison.OrdinalIgnoreCase))
                {
                    driveId = PflichtString(drive, "id");
                    break;
                }
            }

            drivesUrl = OptionalString(drivesJson.RootElement, "@odata.nextLink");
        }

        if (driveId is null)
            throw new InvalidOperationException($"SharePoint-Dokumentbibliothek '{_options.LibraryName}' wurde nicht gefunden.");

        var folderUrl = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/root:{PfadKodieren(_options.FolderPath)}?$select=id";
        using var folderJson = await HoleJsonAsync(folderUrl, cancellationToken);
        return new SharePointQuelle(driveId, PflichtString(folderJson.RootElement, "id"));
    }

    public async Task<SharePointAenderungsseite> AenderungsseiteAsync(
        SharePointQuelle quelle,
        string? fortsetzungsUrl,
        CancellationToken cancellationToken = default)
    {
        var url = fortsetzungsUrl
            ?? $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(quelle.DriveId)}/items/{Uri.EscapeDataString(quelle.OrdnerItemId)}/delta?$select=id,name,eTag,webUrl,createdDateTime,lastModifiedDateTime,file,folder,deleted,parentReference";
        using var json = await HoleJsonAsync(url, cancellationToken);

        var eintraege = new List<SharePointAenderung>();
        foreach (var item in json.RootElement.GetProperty("value").EnumerateArray())
        {
            var parentDriveId = item.TryGetProperty("parentReference", out var parent)
                ? OptionalString(parent, "driveId")
                : null;
            eintraege.Add(new SharePointAenderung(
                parentDriveId ?? quelle.DriveId,
                PflichtString(item, "id"),
                OptionalString(item, "name") ?? "",
                OptionalString(item, "eTag") ?? "",
                OptionalString(item, "webUrl") ?? "",
                OptionalUtcDateTime(item, "createdDateTime") ?? DateTime.UnixEpoch,
                OptionalUtcDateTime(item, "lastModifiedDateTime") ?? DateTime.UnixEpoch,
                item.TryGetProperty("file", out _),
                item.TryGetProperty("deleted", out _)));
        }

        return new SharePointAenderungsseite(
            eintraege,
            OptionalString(json.RootElement, "@odata.nextLink"),
            OptionalString(json.RootElement, "@odata.deltaLink"));
    }

    public async Task<Stream> OeffnenAsync(
        string driveId,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(itemId)}/content";
        using var response = await SendenAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var maxBytes = Math.Max(1, _options.MaxFileSizeMegabytes) * 1024L * 1024L;
        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength.HasValue && contentLength.Value > maxBytes)
            throw new InvalidOperationException($"SharePoint-Dokument ist groesser als {_options.MaxFileSizeMegabytes} MB.");

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                await result.DisposeAsync();
                throw new InvalidOperationException($"SharePoint-Dokument ist groesser als {_options.MaxFileSizeMegabytes} MB.");
            }
            await result.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        result.Position = 0;
        return result;
    }

    private async Task<JsonDocument> HoleJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await SendenAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private async Task<HttpResponseMessage> SendenAsync(string url, CancellationToken cancellationToken)
    {
        for (var versuch = 0; ; versuch++)
        {
            using var request = await ErzeugeRequestAsync(url, cancellationToken);
            var response = await http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (versuch >= 3 || response.StatusCode is not (HttpStatusCode.TooManyRequests
                    or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout))
                return response;

            var wartezeit = response.Headers.RetryAfter?.Delta
                ?? TimeSpan.FromSeconds(Math.Pow(2, versuch + 1));
            response.Dispose();
            await Task.Delay(wartezeit, cancellationToken);
        }
    }

    private async Task<HttpRequestMessage> ErzeugeRequestAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, "graph.microsoft.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Microsoft Graph hat eine ungueltige Fortsetzungs-URL geliefert.");

        var token = await credential.GetTokenAsync(GraphTokenContext, cancellationToken);
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private void KonfigurationPruefen()
    {
        if (string.IsNullOrWhiteSpace(_options.DriveId) != string.IsNullOrWhiteSpace(_options.PermissionRootItemId))
            throw new InvalidOperationException(
                "SharePointAuftragsinformationen:DriveId und PermissionRootItemId muessen gemeinsam gesetzt werden.");

        if (!string.IsNullOrWhiteSpace(_options.DriveId))
            return;

        if (string.IsNullOrWhiteSpace(_options.HostName) || string.IsNullOrWhiteSpace(_options.SitePath)
            || string.IsNullOrWhiteSpace(_options.LibraryName) || string.IsNullOrWhiteSpace(_options.FolderPath))
            throw new InvalidOperationException("SharePointAuftragsinformationen ist nicht vollstaendig konfiguriert.");
    }

    private static string PfadKodieren(string path) =>
        "/" + string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

    private static string PflichtString(JsonElement element, string name) =>
        OptionalString(element, name)
        ?? throw new InvalidOperationException($"Microsoft Graph-Antwort enthaelt '{name}' nicht.");

    private static string? OptionalString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTime? OptionalUtcDateTime(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(value.GetString(), out var parsed)
                ? parsed.UtcDateTime
                : null;
}
