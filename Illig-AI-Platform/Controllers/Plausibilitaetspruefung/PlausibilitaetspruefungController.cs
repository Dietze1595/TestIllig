using Asp.Versioning;
using Illig_AI_Platform.Services;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.SapImport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Illig_AI_Platform.Controllers.Plausibilitaetspruefung;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sap-import/plausibilitaetspruefung")]
[ApiExplorerSettings(GroupName = "sap-import")]
[Tags("Plausibilitätsprüfung")]
[Authorize(Roles = AppRoles.SearchSystem, AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
public class PlausibilitaetspruefungController(SapDirektImportService importService) : ControllerBase
{
    [HttpPost("stuecklisten")]
    [EndpointSummary("Stücklisten zu Aufträgen")]
    [EndpointDescription("Importiert eine hierarchische SAP-Auftragsstückliste direkt in StuecklistenPositionen.")]
    [ProducesResponseType(typeof(SapDirektImportErgebnis), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<SapDirektImportErgebnis>> Stuecklisten(
        [FromBody] StuecklistenPush? push,
        CancellationToken cancellationToken) =>
        AusfuehrenAsync(push, value => importService.ImportiereStuecklisteAsync(value, cancellationToken));

    [HttpPost("maximalstuecklisten")]
    [EndpointSummary("Maximalstückliste je Maschine")]
    [EndpointDescription(
        "Importiert die Kopfdaten in MaschinentypStuecklisten und die Hierarchie direkt in MaximalstuecklistenPositionen.")]
    [ProducesResponseType(typeof(SapDirektImportErgebnis), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<SapDirektImportErgebnis>> Maximalstuecklisten(
        [FromBody] MaximalstuecklistenPush? push,
        CancellationToken cancellationToken) =>
        AusfuehrenAsync(push, value => importService.ImportiereMaximalstuecklisteAsync(value, cancellationToken));

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
