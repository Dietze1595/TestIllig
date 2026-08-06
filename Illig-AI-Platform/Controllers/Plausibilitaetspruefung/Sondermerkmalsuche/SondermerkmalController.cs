using Asp.Versioning;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Illig_AI_Platform.Controllers.Plausibilitaetspruefung.Sondermerkmalsuche;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sondermerkmal")]
[ApiExplorerSettings(GroupName = "app")]
[Tags("Sondermerkmalsuche")]
[Authorize(Roles = AppRoles.PlausibilityCheck)]
public class SondermerkmalController(ISondermerkmalService service) : ControllerBase
{
    // Auftragsnummer als Query-Parameter statt Pfadsegment: eine Linie ("11055627 / 40") enthält
    // einen Schrägstrich, der als %2F im Pfad von Kestrel standardmäßig abgewiesen wird.
    [HttpGet("analyze")]
    [ProducesResponseType(typeof(StuecklisteAnalyse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StuecklisteAnalyse>> Analyze([FromQuery] string auftragsnummer)
    {
        if (string.IsNullOrWhiteSpace(auftragsnummer))
            return BadRequest("Auftragsnummer fehlt.");

        var analyse = await service.AnalyzeAsync(auftragsnummer);
        if (analyse is null)
            return NotFound($"Kein Auftrag '{auftragsnummer}' gefunden.");

        return Ok(analyse);
    }

    [HttpPost("search")]
    [ProducesResponseType(typeof(IReadOnlyList<ReferenzTreffer>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ReferenzTreffer>>> Search([FromBody] SearchRequest request)
    {
        if (request is null || request.Merkmalsnummern.Count == 0)
            return BadRequest("Keine Merkmalsnummern übergeben.");

        return Ok(await service.SearchAsync(request));
    }

    [HttpGet("detail/{auftragsnummer}")]
    [ProducesResponseType(typeof(StuecklisteDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StuecklisteDetail>> Detail(string auftragsnummer)
    {
        var detail = await service.GetDetailAsync(auftragsnummer);
        if (detail is null)
            return NotFound($"Kein Auftrag '{auftragsnummer}' gefunden.");

        return Ok(detail);
    }

    [HttpGet("detail/{auftragsnummer}/dokument")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Dokument(string auftragsnummer)
    {
        var dokument = await service.GetDokumentAsync(auftragsnummer, HttpContext.RequestAborted);
        return dokument is null
            ? NotFound($"Kein Dokument für Auftrag '{auftragsnummer}' gefunden.")
            : File(dokument.Inhalt, "application/pdf", dokument.Dateiname);
    }
}
