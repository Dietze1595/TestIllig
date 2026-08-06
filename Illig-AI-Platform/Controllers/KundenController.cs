using Asp.Versioning;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Illig_AI_Platform.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/kunden")]
[ApiExplorerSettings(GroupName = "app")]
[Tags("Kunden")]
[Authorize(Roles = AppRoles.SearchSystem)]
public class KundenController(
    KundenstammService service,
    AppDbContext db,
    IBlobStorageService stuecklistenStorage,
    [FromKeyedServices("auftragsanlage")] IBlobStorageService auftragsanlageStorage) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<KundenUebersicht>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KundenUebersicht>>> Liste([FromQuery] string? suche) =>
        Ok(await service.ListeAsync(suche, HttpContext.RequestAborted));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(KundenDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KundenDetail>> Detail(int id)
    {
        var detail = await service.DetailAsync(id, HttpContext.RequestAborted);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("{id:int}/dokument/{quelltyp:int}/{quellId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Dokument(int id, KundenQuelltyp quelltyp, int quellId)
    {
        var gehoertZumKunden = await db.KundenQuellen.AsNoTracking().AnyAsync(
            q => q.KundeId == id && q.Quelltyp == quelltyp && q.QuellId == quellId,
            HttpContext.RequestAborted);
        if (!gehoertZumKunden)
            return NotFound();

        string? dateiname;
        string? blobPfad;
        IBlobStorageService storage;

        switch (quelltyp)
        {
            case KundenQuelltyp.Angebot:
            {
                var dokument = await db.Angebote.AsNoTracking()
                    .Where(a => a.Id == quellId && a.KundeId == id)
                    .Select(a => new { a.Dateiname, a.BlobPfad })
                    .FirstOrDefaultAsync(HttpContext.RequestAborted);
                if (dokument is null)
                    return NotFound();
                dateiname = dokument.Dateiname;
                blobPfad = dokument.BlobPfad;
                storage = auftragsanlageStorage;
                break;
            }
            case KundenQuelltyp.Kundenbestellung:
            {
                var dokument = await db.Auftragsbestaetigungen.AsNoTracking()
                    .Where(b => b.Id == quellId && b.KundeId == id)
                    .Select(b => new { b.Dateiname, b.BlobPfad })
                    .FirstOrDefaultAsync(HttpContext.RequestAborted);
                if (dokument is null)
                    return NotFound();
                dateiname = dokument.Dateiname;
                blobPfad = dokument.BlobPfad;
                storage = auftragsanlageStorage;
                break;
            }
            case KundenQuelltyp.Auftragsinformation:
            {
                var dokument = await db.StuecklistenpruefungVerlaufEintraege.AsNoTracking()
                    .Where(e => e.Id == quellId && e.KundeId == id)
                    .Select(e => new { e.Dateiname, e.BlobPfad })
                    .FirstOrDefaultAsync(HttpContext.RequestAborted);
                if (dokument is null)
                    return NotFound();
                dateiname = dokument.Dateiname;
                blobPfad = dokument.BlobPfad;
                storage = stuecklistenStorage;
                break;
            }
            default:
                return NotFound();
        }

        var inhalt = await storage.OpenReadAsync(blobPfad, HttpContext.RequestAborted);
        return File(inhalt, "application/pdf", dateiname);
    }
}
