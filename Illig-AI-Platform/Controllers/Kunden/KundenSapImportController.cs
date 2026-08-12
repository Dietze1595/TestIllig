using Asp.Versioning;
using Illig_AI_Platform.Services;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.SapImport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Illig_AI_Platform.Controllers.Kunden;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sap-import/kunden")]
[ApiExplorerSettings(GroupName = "sap-import")]
[Tags("Kunden")]
[Authorize(Roles = AppRoles.SearchSystem, AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
public class KundenSapImportController(SapDirektImportService importService) : ControllerBase
{
    [HttpPost("partneradressen")]
    [EndpointSummary("Partneradressen zu Kunden")]
    [EndpointDescription("Importiert die SAP-Partneradressen (Auftraggeber, Rechnungsempfänger, Regulierer, Vertretung, Warenempfänger, Endkunde) je Hauptkundennummer direkt in KundenPartneradressen.")]
    [ProducesResponseType(typeof(SapDirektImportErgebnis), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<SapDirektImportErgebnis>> Partneradressen(
        [FromBody] KundenAdressenPush? push,
        CancellationToken cancellationToken) =>
        AusfuehrenAsync(push, value => importService.ImportiereKundenAdressenAsync(value, cancellationToken));

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
