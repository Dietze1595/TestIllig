using Asp.Versioning;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Lieferantenassistent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Illig_AI_Platform.Controllers.Lieferantenassistent;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/lieferantenassistent")]
[ApiExplorerSettings(GroupName = "app")]
[Tags("Lieferantenassistent")]
[Authorize(Roles = AppRoles.Lieferantenassistent)]
public class LieferantenassistentController(
    LieferantenassistentAbfrageService abfrageService,
    LieferantenassistentImportService importService,
    ILogger<LieferantenassistentController> logger) : ControllerBase
{
    // [FromQuery] auf den Array-Parametern ist nicht optional: [ApiController] leitet
    // Array-/komplexe Parameter sonst als [FromBody] her, und zwei Body-gebundene Parameter
    // auf einer Action lassen die App beim Start abstürzen (nicht nur diesen Endpunkt).
    [HttpGet("positionen")]
    [ProducesResponseType(typeof(IReadOnlyList<OffenePositionAnsicht>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<OffenePositionAnsicht>>> Positionen(
        string? suche, [FromQuery] LieferterminStatus[]? status, [FromQuery] string[]? einkaeufergruppen)
    {
        try
        {
            return Ok(await abfrageService.OffenePositionenAsync(suche, status, einkaeufergruppen: einkaeufergruppen));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Laden der offenen Positionen fehlgeschlagen.");
            return StatusCode(500, "Die Liste konnte nicht geladen werden. Bitte versuche es erneut.");
        }
    }

    [HttpGet("einkaeufergruppen")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<string>>> Einkaeufergruppen()
    {
        try
        {
            return Ok(await abfrageService.EinkaeufergruppenAsync());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Laden der Einkäufergruppen fehlgeschlagen.");
            return StatusCode(500, "Die Einkäufergruppen konnten nicht geladen werden. Bitte versuche es erneut.");
        }
    }

    // Einmaliger Prototyp-Import (siehe Design-Spec, Abschnitt "Einmal-Import") — bewusst kein
    // Kunden-UI, nur ein Admin-Endpunkt. Admin-Rolle kommt additiv zur klassenweiten
    // Lieferantenassistent-Rolle hinzu (siehe StuecklistenpruefungController.Import).
    [HttpPost("import")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(LieferantenassistentImportBericht), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LieferantenassistentImportBericht>> Import(LieferantenassistentImportAnfrage anfrage)
    {
        try
        {
            return Ok(await importService.ImportierenAsync(
                anfrage.DispositionslisteCsvPfad, anfrage.LieferantenstammdatenCsvPfad, anfrage.MailAdressenCsvPfad));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lieferantenassistent-Import fehlgeschlagen.");
            return StatusCode(500, "Der Import ist fehlgeschlagen. Bitte Dateipfade und Format prüfen.");
        }
    }
}
