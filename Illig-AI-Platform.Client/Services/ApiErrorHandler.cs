using System.Net;

namespace Illig_AI_Platform.Client.Services;

/// <summary>
/// Zentraler DelegatingHandler für den benannten HttpClient "ServerAPI": wandelt jede
/// nicht-erfolgreiche Server-Antwort (außer 404, das die Clients selbst als "nicht
/// gefunden" auswerten) sowie Netzwerkfehler in eine <see cref="ApiRequestException"/>
/// mit verständlicher deutscher Meldung um, statt eine unbehandelte
/// <see cref="HttpRequestException"/> bis zum globalen Blazor-Error-UI durchschlagen zu lassen.
/// </summary>
public class ApiErrorHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ApiRequestException(
                null, "Verbindung zum Server fehlgeschlagen. Bitte prüfe deine Internetverbindung.");
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Vom Aufrufer ausgelöster Abbruch (z. B. Komponente wurde disposed) — kein API-Fehler.
        }
        catch (TaskCanceledException)
        {
            throw new ApiRequestException(
                null, "Der Server hat zu lange nicht geantwortet (Zeitüberschreitung). Bitte versuche es erneut.");
        }

        var istErwarteterDokumentartHinweis =
            response.StatusCode == HttpStatusCode.UnprocessableEntity &&
            (request.RequestUri?.AbsolutePath.EndsWith(
                 "/auftragsanlage/innendienst/bestaetigung",
                 StringComparison.OrdinalIgnoreCase) == true
             || request.RequestUri?.AbsolutePath.EndsWith(
                 "/auftragsanlage/vertrieb/angebot/analyse",
                 StringComparison.OrdinalIgnoreCase) == true);

        if (response.IsSuccessStatusCode ||
            response.StatusCode == HttpStatusCode.NotFound ||
            istErwarteterDokumentartHinweis)
            return response;

        var statusCode = (int)response.StatusCode;
        var message = statusCode switch
        {
            401 => "Deine Sitzung ist abgelaufen. Bitte lade die Seite neu und melde dich erneut an.",
            403 => "Du hast (noch) keine Berechtigung für diese Aktion. Falls dir gerade erst Zugriff " +
                   "gewährt wurde, kann die Aktivierung serverseitig bis zu 5 Minuten dauern — bitte " +
                   "versuche es in Kürze erneut.",
            >= 500 => "Der Server hat gerade ein Problem. Bitte versuche es später erneut.",
            _ => "Die Anfrage konnte nicht verarbeitet werden. Bitte versuche es erneut."
        };

        throw new ApiRequestException(statusCode, message);
    }
}
