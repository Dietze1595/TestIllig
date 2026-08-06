using Asp.Versioning;
using Illig_AI_Platform.Services.Auftragsinformationen;
using Illig_AI_Platform.Shared.Auftragsinformationen;
using Illig_AI_Platform.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Illig_AI_Platform.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auftragsinformationen")]
[ApiExplorerSettings(GroupName = "app")]
[Tags("Auftragsinformationen")]
[Authorize(Roles = AppRoles.PlausibilityCheck)]
public sealed class AuftragsinformationenController(
    AuftragsdokumentService dokumente,
    AuftragsinformationenImportService import,
    ILogger<AuftragsinformationenController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuftragsdokumentUebersicht>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuftragsdokumentUebersicht>>> Liste(
        CancellationToken cancellationToken) =>
        Ok(await dokumente.ListeAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AuftragsdokumentDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuftragsdokumentDetail>> Detail(
        int id,
        CancellationToken cancellationToken)
    {
        var detail = await dokumente.DetailAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("{id:int}/dokument")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Dokument(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var dokument = await dokumente.DokumentAsync(id, cancellationToken);
            if (dokument is null)
                return NotFound();

            Response.Headers.ContentDisposition =
                $"inline; filename*=UTF-8''{Uri.EscapeDataString(dokument.Value.Dateiname)}";
            return File(dokument.Value.Inhalt, "application/pdf", enableRangeProcessing: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "SharePoint-Auftragsdokument {Id} konnte nicht geladen werden.", id);
            return StatusCode(502, "Das Dokument konnte nicht aus SharePoint geladen werden.");
        }
    }

    [HttpPost("synchronisieren")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(AuftragsinformationenImportErgebnis), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuftragsinformationenImportErgebnis>> Synchronisieren(
        CancellationToken cancellationToken) =>
        Ok(await import.SynchronisierenAsync(cancellationToken));
}
