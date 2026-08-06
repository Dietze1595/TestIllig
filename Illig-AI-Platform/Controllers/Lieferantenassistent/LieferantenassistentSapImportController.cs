using Asp.Versioning;
using Illig_AI_Platform.Services;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.SapImport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Illig_AI_Platform.Controllers.Lieferantenassistent;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sap-import/lieferantenassistent")]
[ApiExplorerSettings(GroupName = "sap-import")]
[Tags("Lieferantenassistent")]
[Authorize(Roles = AppRoles.SearchSystem, AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
public class LieferantenassistentSapImportController(SapDirektImportService importService) : ControllerBase
{
    [HttpPost("dispositionsliste")]
    [EndpointSummary("Dispositionslisten")]
    [EndpointDescription("Importiert offene Bestellungen und Einteilungen direkt in Dispositionspositionen.")]
    [ProducesResponseType(typeof(SapDirektImportErgebnis), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<SapDirektImportErgebnis>> Dispositionsliste(
        [FromBody] OffeneBestellungenPush? push,
        CancellationToken cancellationToken) =>
        AusfuehrenAsync(push, value => importService.ImportiereDispositionAsync(value, cancellationToken));

    [HttpPost("lieferanten-kreditoren-stammdaten")]
    [EndpointSummary("Stammdaten der Lieferanten und Kreditoren")]
    [EndpointDescription("Importiert Lieferanten direkt in Lieferanten und Kontakte in LieferantEmailAdressen.")]
    [ProducesResponseType(typeof(SapDirektImportErgebnis), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<SapDirektImportErgebnis>> LieferantenKreditorenStammdaten(
        [FromBody] LieferantenDatenPush? push,
        CancellationToken cancellationToken) =>
        AusfuehrenAsync(push, value => importService.ImportiereLieferantenAsync(value, cancellationToken));

    private static async Task<ActionResult<SapDirektImportErgebnis>> AusfuehrenAsync<T>(
        T? push,
        Func<T, Task<SapDirektImportErgebnis>> importieren)
        where T : class
    {
        if (push is null)
            return new BadRequestObjectResult(new { fehler = new[] { "Kein JSON-Payload übergeben." } });

        try
        {
            return new OkObjectResult(await importieren(push));
        }
        catch (SapImportValidierungsException exception)
        {
            return new BadRequestObjectResult(new { fehler = exception.Fehler });
        }
    }
}
