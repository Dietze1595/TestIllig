namespace Illig_AI_Platform.Client.Services;

/// <summary>
/// Wird von <see cref="ApiErrorHandler"/> für jede nicht-2xx-Server-Antwort (außer 404)
/// sowie für Netzwerkfehler geworfen. Trägt bereits eine für die UI verständliche Meldung.
/// </summary>
public class ApiRequestException(int? statusCode, string message) : Exception(message)
{
    /// <summary>Null bei Netzwerkfehlern (kein Response erhalten).</summary>
    public int? StatusCode { get; } = statusCode;
}
