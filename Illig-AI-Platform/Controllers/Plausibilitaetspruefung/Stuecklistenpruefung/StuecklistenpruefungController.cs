using System.Text;
using System.Text.Json;
using Asp.Versioning;
using Illig_AI_Platform.Services;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Illig_AI_Platform.Controllers.Plausibilitaetspruefung.Stuecklistenpruefung;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/stuecklistenpruefung")]
[ApiExplorerSettings(GroupName = "app")]
[Tags("Stücklistenprüfung")]
[Authorize(Roles = AppRoles.PlausibilityCheck)]
public class StuecklistenpruefungController(
    IDocumentAnalyseService service,
    StuecklistenpruefungVerlaufService verlaufService,
    StuecklistenAufbauService aufbauService,
    StuecklistenImportService importService,
    ILogger<StuecklistenpruefungController> logger) : ControllerBase
{
    private const long MaxDateiBytes = 20 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonWeb = new(JsonSerializerDefaults.Web);

    [HttpPost("analyze")]
    [ProducesResponseType(typeof(DokumentAnalyseErgebnis), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DokumentAnalyseErgebnis>> Analyze([FromForm] IFormFile datei)
    {
        if (datei is null || datei.Length == 0)
            return BadRequest("Keine Datei hochgeladen.");

        if (datei.Length > MaxDateiBytes)
            return BadRequest("Datei zu groß (max. 20 MB).");

        if (!string.Equals(datei.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Es werden nur PDF-Dateien unterstützt.");

        try
        {
            using var puffer = new MemoryStream();
            await using (var quelle = datei.OpenReadStream())
            {
                await quelle.CopyToAsync(puffer);
            }
            puffer.Position = 0;

            var ergebnis = await service.AnalyzeAsync(puffer);

            if (!ergebnis.IstAuftragsinformation)
                return UnprocessableEntity("Das hochgeladene Dokument ist keine Auftragsinformation. Bitte die richtige Datei hochladen.");

            if (string.IsNullOrWhiteSpace(ergebnis.Auftragsnummer))
                return UnprocessableEntity("Auftragsinformation erkannt, aber die Auftragsnummer konnte nicht ausgelesen werden. Bitte das Team informieren.");

            var matrixVerfuegbarkeit =
                await aufbauService.UmsetzungsmatrixVerfuegbarkeitAsync(
                    ergebnis.Maschinentyp,
                    HttpContext.RequestAborted);
            ergebnis = ergebnis with
            {
                UmsetzungsmatrixVorhanden = matrixVerfuegbarkeit.FuerMaschinentypVorhanden,
                VerfuegbareUmsetzungsmatrizen =
                    matrixVerfuegbarkeit.VorhandeneMaschinentypSchluessel
            };

            if (User.GetUserId() is Guid userId)
            {
                try
                {
                    puffer.Position = 0;
                    var (verlaufId, bestehend) =
                        await verlaufService.SpeichernOderOeffnenAsync(userId, datei.FileName, puffer, ergebnis);
                    ergebnis = ergebnis with { VerlaufId = verlaufId, BestehenderEintrag = bestehend };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Fehler beim Speichern des Verlauf-Eintrags für Datei {Dateiname}.", datei.FileName);
                }
            }

            return Ok(ergebnis);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler bei der Dokumentanalyse für Datei {Dateiname}.", datei.FileName);
            return StatusCode(500, "Die Analyse ist fehlgeschlagen. Bitte versuche es erneut.");
        }
    }

    [HttpGet("verlauf")]
    [ProducesResponseType(typeof(IReadOnlyList<VerlaufEintragUebersicht>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<VerlaufEintragUebersicht>>> Verlauf(
        [FromQuery] bool nurMeine = false)
    {
        if (User.GetUserId() is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await verlaufService.ListeAsync(nurMeine ? userId : null));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Laden der Verlauf-Liste für User {UserId}.", userId);
            return StatusCode(500, "Die Historie konnte nicht geladen werden. Bitte versuche es erneut.");
        }
    }

    [HttpGet("verlauf/{id:int}")]
    [ProducesResponseType(typeof(VerlaufDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VerlaufDetail>> VerlaufDetail(int id)
    {
        if (User.GetUserId() is not Guid userId)
            return Unauthorized();

        try
        {
            var detail = await verlaufService.DetailAsync(id);
            if (detail is null)
                return NotFound();

            var matrixVerfuegbarkeit =
                await aufbauService.UmsetzungsmatrixVerfuegbarkeitAsync(
                    detail.Maschinentyp,
                    HttpContext.RequestAborted);
            detail = detail with
            {
                UmsetzungsmatrixVorhanden = matrixVerfuegbarkeit.FuerMaschinentypVorhanden,
                VerfuegbareUmsetzungsmatrizen =
                    matrixVerfuegbarkeit.VorhandeneMaschinentypSchluessel
            };
            return Ok(detail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Laden des Verlauf-Details {Id}.", id);
            return StatusCode(500, "Der Verlauf-Eintrag konnte nicht geladen werden. Bitte versuche es erneut.");
        }
    }

    [HttpPut("verlauf/{id:int}/stand")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerlaufStandAktualisieren(int id, VerlaufStandAktualisieren stand)
    {
        if (User.GetUserId() is not Guid userId)
            return Unauthorized();

        try
        {
            var aktualisiert = await verlaufService.StandAktualisierenAsync(userId, id, stand);
            return aktualisiert ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Aktualisieren des Verlauf-Stands {Id} fehlgeschlagen.", id);
            return StatusCode(500, "Der aktuelle Stand konnte nicht in der Historie gespeichert werden.");
        }
    }

    [HttpPut("verlauf/{id:int}/neu-einlesen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerlaufNeuEinlesen(
        int id,
        [FromForm] IFormFile datei,
        [FromForm] string ergebnisJson)
    {
        if (User.GetUserId() is not Guid userId)
            return Unauthorized();

        if (datei is null || datei.Length == 0)
            return BadRequest("Keine Datei hochgeladen.");
        if (datei.Length > MaxDateiBytes)
            return BadRequest("Datei zu groß (max. 20 MB).");
        if (!string.Equals(datei.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Es werden nur PDF-Dateien unterstützt.");

        DokumentAnalyseErgebnis? ergebnis;
        try
        {
            ergebnis = JsonSerializer.Deserialize<DokumentAnalyseErgebnis>(ergebnisJson, JsonWeb);
        }
        catch (JsonException)
        {
            return BadRequest("Die ausgelesenen Auftragsinformationen sind ungültig.");
        }

        if (ergebnis is null || string.IsNullOrWhiteSpace(ergebnis.Auftragsnummer))
            return BadRequest("Die ausgelesenen Auftragsinformationen sind unvollständig.");

        try
        {
            using var puffer = new MemoryStream();
            await datei.CopyToAsync(puffer);
            puffer.Position = 0;

            var ersetzt = await verlaufService.NeuEinlesenAsync(
                userId, id, datei.FileName, puffer, ergebnis, HttpContext.RequestAborted);
            return ersetzt ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erneutes Einlesen des Verlauf-Eintrags {Id} fehlgeschlagen.", id);
            return StatusCode(500, "Das Dokument konnte nicht neu eingelesen werden.");
        }
    }

    [HttpGet("verlauf/{id:int}/dokument")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerlaufDokument(int id)
    {
        if (User.GetUserId() is not Guid userId)
            return Unauthorized();

        try
        {
            var dokument = await verlaufService.DokumentAsync(id);
            return dokument is null ? NotFound() : File(dokument.Value.Inhalt, "application/pdf", dokument.Value.Dateiname);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Laden des Verlauf-Dokuments {Id}.", id);
            return StatusCode(500, "Das Dokument konnte nicht geladen werden. Bitte versuche es erneut.");
        }
    }

    [HttpPost("aufbauen")]
    [ProducesResponseType(typeof(StuecklistenKnoten), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StuecklistenKnoten>> Aufbauen(AufbauenAnfrage anfrage)
    {
        try
        {
            var merkmalsnummern = anfrage.Merkmalsnummern.ToHashSet();
            var baum = await aufbauService.AufbauenAsync(anfrage.Maschinentyp, merkmalsnummern);
            if (baum is null)
                return NotFound($"Für Maschinentyp '{anfrage.Maschinentyp}' ist noch keine Stücklistenprüfung hinterlegt.");
            return Ok(baum);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stücklisten-Aufbau fehlgeschlagen für Maschinentyp {Maschinentyp}.", anfrage.Maschinentyp);
            return StatusCode(500, "Der Stücklisten-Aufbau ist fehlgeschlagen. Bitte versuche es erneut.");
        }
    }

    // Nur ein Admin-Endpunkt um eine neue Umsetzungsmatrix zu hinterlegen
    [HttpPost("import")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Import(
        [FromForm] IFormFile maximalstueckliste, [FromForm] IFormFile umsetzungsmatrix,
        [FromForm] string maschinentypSchluessel, [FromForm] string format)
    {
        if (maximalstueckliste is null || maximalstueckliste.Length == 0)
            return BadRequest("Keine Maximalstückliste hochgeladen.");
        if (maximalstueckliste.Length > MaxDateiBytes)
            return BadRequest("Maximalstückliste zu groß (max. 20 MB).");

        if (umsetzungsmatrix is null || umsetzungsmatrix.Length == 0)
            return BadRequest("Keine Umsetzungsmatrix hochgeladen.");
        if (umsetzungsmatrix.Length > MaxDateiBytes)
            return BadRequest("Umsetzungsmatrix zu groß (max. 20 MB).");

        if (string.IsNullOrWhiteSpace(maschinentypSchluessel))
            return BadRequest("Maschinentyp-Schlüssel fehlt.");

        // Format wird explizit gewählt (nicht automatisch erkannt), da sich die Formate der beiden
        // Dateien äußerlich kaum unterscheiden und eine Fehlzuordnung stille Falschdaten erzeugen würde.
        if (!Enum.TryParse<StuecklistenImportFormat>(format, ignoreCase: true, out var importFormat)
            || !Enum.IsDefined(importFormat))
            return BadRequest(
                $"Ungültiges Importformat '{format}'. Gültige Werte: " +
                $"{string.Join(", ", Enum.GetNames<StuecklistenImportFormat>())}.");

        try
        {
            await using var txtStream = maximalstueckliste.OpenReadStream();
            using var reader = new StreamReader(txtStream, Encoding.GetEncoding("ISO-8859-1"));
            var wurzel = MaximalstuecklisteParser.Parse(await reader.ReadToEndAsync(), importFormat);

            await using var xlsxStream = umsetzungsmatrix.OpenReadStream();
            var matrixZeilen = UmsetzungsmatrixParser.Parse(xlsxStream, importFormat);

            await importService.ImportierenAsync(maschinentypSchluessel, wurzel.Artikelnummer, wurzel, matrixZeilen);
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stücklisten-Import fehlgeschlagen für {MaschinentypSchluessel}.", maschinentypSchluessel);
            return StatusCode(500, "Der Import ist fehlgeschlagen. Bitte Dateien und Encoding prüfen.");
        }
    }

    [HttpPost("vergleichen")]
    [ProducesResponseType(typeof(VergleichsErgebnis), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VergleichsErgebnis>> Vergleichen(
        [FromForm] IFormFile sapDatei, [FromForm] string unsereStuecklisteJson)
    {
        if (sapDatei is null || sapDatei.Length == 0)
            return BadRequest("Keine SAP-Datei hochgeladen.");
        if (sapDatei.Length > MaxDateiBytes)
            return BadRequest("SAP-Datei zu groß (max. 20 MB).");

        if (string.IsNullOrWhiteSpace(unsereStuecklisteJson))
            return BadRequest("Eigene Stückliste fehlt.");

        try
        {
            await using var stream = sapDatei.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.GetEncoding("ISO-8859-1"));
            var sapWurzel = MaximalstuecklisteTxtParser.Parse(await reader.ReadToEndAsync());

            var unsereWurzel = JsonSerializer.Deserialize<StuecklistenKnoten>(unsereStuecklisteJson)
                ?? throw new InvalidOperationException("Stückliste ist leer.");

            var ergebnis = StuecklistenDiff.Vergleiche(unsereWurzel, sapWurzel);
            return Ok(ergebnis);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stücklisten-Vergleich fehlgeschlagen für Datei {Dateiname}.", sapDatei.FileName);
            return StatusCode(500, "Der Vergleich ist fehlgeschlagen. Bitte Datei und Format prüfen.");
        }
    }
}
