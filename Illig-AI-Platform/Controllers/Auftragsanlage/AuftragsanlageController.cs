using Asp.Versioning;
using Illig_AI_Platform.Services;
using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Illig_AI_Platform.Controllers.Auftragsanlage;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auftragsanlage")]
[ApiExplorerSettings(GroupName = "app")]
[Tags("Auftragsanlage")]
public class AuftragsanlageController(
    IAngebotsdokumentAnalyseService analyseService,
    AngebotsService angebotsService,
    IAngebotsvergleichLlmService vergleichService,
    IVersandartLlmService versandartLlmService,
    ILogger<AuftragsanlageController> logger) : ControllerBase
{
    private const long MaxDateiBytes = 20 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonWeb = new(JsonSerializerDefaults.Web);

    [HttpPost("vertrieb/angebot/analyse")]
    [Authorize(Roles = AppRoles.OrderCreationSales)]
    [ProducesResponseType(typeof(AngebotAnalyseAntwort), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AngebotAnalyseAntwort>> AngebotAnalysieren([FromForm] IFormFile datei)
    {
        var validierungsFehler = ValidiereDatei(datei);
        if (validierungsFehler is not null)
            return BadRequest(validierungsFehler);

        try
        {
            using var puffer = await InPufferLesenAsync(datei);
            var daten = await analyseService.AnalyzeAsync(puffer);

            if (!AngebotsdokumentErkennung.IstAngebot(daten))
                return UnprocessableEntity(
                    "Kein Angebot erkannt. Bitte lade ein gültiges ILLIG-Angebot hoch.");

            var vertriebsbedingungen = VertriebsbedingungenParser.Parse(daten.Volltext);
            var versandart = await ErmittleVersandartAsync(
                daten.Volltext, vertriebsbedingungen.Versandbedingung);
            daten = daten with { Versandart = versandart };

            var checkliste = AngebotsCheckliste.Berechnen(
                kundenname: daten.Kundenname,
                kundenadresse: daten.Kundenadresse,
                nummer: daten.Nummer,
                zahlungsbedingungen: vertriebsbedingungen.Zahlungsbedingungen ?? daten.Zahlungsbedingungen,
                incoterm: vertriebsbedingungen.Incoterm,
                incotermOrt: vertriebsbedingungen.IncotermOrt,
                versandbedingung: versandart,
                zahlungsbedingungCode: vertriebsbedingungen.ZahlungsbedingungCode ?? daten.ZahlungsbedingungCode,
                zahlungsplan: vertriebsbedingungen.Zahlungsplan ?? daten.Zahlungsplan,
                verkaeufer: vertriebsbedingungen.Verkaeufer ?? daten.Verkaeufer,
                liefertermin: vertriebsbedingungen.Liefertermin ?? daten.Liefertermin,
                gueltigBis: vertriebsbedingungen.GueltigBis ?? daten.GueltigBis,
                lieferadresse: daten.Lieferadresse);

            var vorhandeneVersionen = string.IsNullOrWhiteSpace(daten.Nummer)
                ? (IReadOnlyList<int>)[]
                : await angebotsService.VorhandeneVersionenAsync(daten.Nummer);

            return Ok(new AngebotAnalyseAntwort(
                checkliste, daten,
                vertriebsbedingungen.Incoterm, vertriebsbedingungen.IncotermOrt, versandart,
                vorhandeneVersionen));
        }
        catch (Azure.RequestFailedException ex)
        {
            logger.LogWarning(ex, "Document Intelligence konnte Datei {Dateiname} nicht verarbeiten.", datei.FileName);
            return UnprocessableEntity("Das Dokument konnte nicht ausgelesen werden. Bitte ein gültiges PDF hochladen.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler bei der Angebotsanalyse für Datei {Dateiname}.", datei.FileName);
            return StatusCode(500, "Die Angebotsprüfung ist fehlgeschlagen. Bitte versuche es erneut.");
        }
    }

    [HttpPost("vertrieb/angebot/speichern")]
    [Authorize(Roles = AppRoles.OrderCreationSales)]
    [ProducesResponseType(typeof(AngebotSpeichernAntwort), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AngebotSpeichernAntwort>> AngebotSpeichern(
        [FromForm] IFormFile datei, [FromForm] string datenJson, [FromForm] string? konfliktStrategie,
        [FromForm] string? versionsKommentar = null)
    {
        var validierungsFehler = ValidiereDatei(datei);
        if (validierungsFehler is not null)
            return BadRequest(validierungsFehler);

        ExtrahierteAngebotsdaten? daten;
        try
        {
            daten = JsonSerializer.Deserialize<ExtrahierteAngebotsdaten>(datenJson, JsonWeb);
        }
        catch (JsonException)
        {
            return BadRequest("Ungültige Angebotsdaten.");
        }

        if (daten is null || string.IsNullOrWhiteSpace(daten.Nummer))
            return BadRequest("Ohne Angebotsnummer kann kein Angebot gespeichert werden.");

        if (!AngebotsdokumentErkennung.IstAngebot(daten))
            return UnprocessableEntity(
                "Kein Angebot erkannt. Bitte lade ein gültiges ILLIG-Angebot hoch.");

        var strategie = konfliktStrategie?.ToLowerInvariant() switch
        {
            "ueberschreiben" => KonfliktStrategie.Ueberschreiben,
            "neueversion" => KonfliktStrategie.NeueVersion,
            _ => KonfliktStrategie.Keine,
        };

        var vertriebsbedingungen = VertriebsbedingungenParser.Parse(daten.Volltext);
        if (!string.IsNullOrWhiteSpace(daten.Versandart))
            vertriebsbedingungen = vertriebsbedingungen with { Versandbedingung = daten.Versandart.Trim() };

        try
        {
            using var puffer = await InPufferLesenAsync(datei);
            var ergebnis = await angebotsService.SpeichernAsync(
                daten, datei.FileName, puffer, strategie, vertriebsbedingungen, User.GetUserId(),
                versionsKommentar);

            if (ergebnis.Konflikt)
                return Ok(new AngebotSpeichernAntwort(
                    false, true, daten.Nummer, null, ergebnis.VorhandeneVersionen, null));

            if (ergebnis.Angebot is null)
                throw new InvalidOperationException(
                    "AngebotsService.SpeichernAsync hat weder einen Konflikt gemeldet noch ein gespeichertes Angebot zurückgegeben.");

            return Ok(new AngebotSpeichernAntwort(
                true, false, ergebnis.Angebot.Angebotsnummer, ergebnis.Angebot.Version, [], ergebnis.Angebot.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Speichern des Angebots {Dateiname}.", datei.FileName);
            return StatusCode(500, "Das Speichern ist fehlgeschlagen. Bitte versuche es erneut.");
        }
    }

    private async Task<string?> ErmittleVersandartAsync(string volltext, string? deterministischerWert)
    {
        if (AngebotsCheckliste.IstBekannteVersandkategorie(deterministischerWert))
            return deterministischerWert;

        try
        {
            return await versandartLlmService.ErmittleAsync(volltext) ?? deterministischerWert;
        }
        catch (Exception ex)
        {
            // Ein optionaler semantischer Fallback darf die ansonsten erfolgreiche
            // Dokumentanalyse nicht abbrechen.
            logger.LogWarning(ex, "LLM-Fallback für die Versandart ist fehlgeschlagen.");
            return deterministischerWert;
        }
    }

    [HttpPost("vertrieb/angebot/{id:int}/freigeben")]
    [Authorize(Roles = AppRoles.OrderCreationSales)]
    [ProducesResponseType(typeof(AngebotFreigabeAntwort), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AngebotFreigabeAntwort>> AngebotFreigeben(int id, [FromBody] AngebotFreigebenAnfrage anfrage)
    {
        var detail = await angebotsService.DetailAsync(id);
        if (detail is null)
            return NotFound($"Angebot mit Id {id} wurde nicht gefunden.");

        // Nur der Owner (Uploader) darf freigeben — Altbestände ohne Owner (null) sind für
        // niemanden freigebbar. Die UI blendet den Button für Fremdangebote aus; diese Prüfung
        // sichert den Endpunkt zusätzlich gegen direkte API-Aufrufe ab.
        if (detail.UserProfileId is not { } owner || owner != User.GetUserId())
            return Forbid();

        var freigabefehler = ErmittleFreigabefehler(detail.Checkliste, anfrage);
        if (freigabefehler.Count > 0)
            return BadRequest($"Freigabe nicht möglich: {string.Join("; ", freigabefehler)}.");

        var angebot = await angebotsService.FreigebenAsync(
            id,
            kundeKommentar: anfrage.KundeKommentar,
            zahlungsbedingungenKommentar: anfrage.ZahlungsbedingungenKommentar,
            incotermKommentar: anfrage.IncotermKommentar,
            versandbedingungKommentar: anfrage.VersandbedingungKommentar,
            zahlungsplanKommentar: anfrage.ZahlungsplanKommentar,
            verkaeuferKommentar: anfrage.VerkaeuferKommentar,
            lieferterminKommentar: anfrage.LieferterminKommentar,
            gueltigkeitsdatumKommentar: anfrage.GueltigkeitsdatumKommentar,
            sapSparteBestaetigt: anfrage.SapSparteBestaetigt,
            sapFuehrendBestaetigt: anfrage.SapFuehrendBestaetigt,
            freigegebenVonUserProfileId: User.GetUserId(),
            lieferadresseKommentar: anfrage.LieferadresseKommentar,
            sapSparteKommentar: anfrage.SapSparteKommentar,
            sapFuehrendKommentar: anfrage.SapFuehrendKommentar);

        if (angebot is null)
            return NotFound($"Angebot mit Id {id} wurde nicht gefunden.");

        return Ok(new AngebotFreigabeAntwort(angebot.FreigegebenAm!.Value));
    }

    [HttpGet("dashboard/stats")]
    [Authorize(Roles = AppRoles.OrderCreation + "," + AppRoles.Admin)]
    [ProducesResponseType(typeof(DashboardStatsAntwort), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DashboardStatsAntwort>> GetDashboardStats([FromQuery] int jahr)
    {
        try
        {
            var stats = await angebotsService.GetDashboardStatsAsync(jahr, HttpContext.RequestAborted);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Laden der Dashboard-Statistiken für das Jahr {Jahr}.", jahr);
            return StatusCode(500, "Die Statistiken konnten nicht geladen werden. Bitte versuche es erneut.");
        }
    }

    [HttpGet("vertrieb/angebote")]
    [Authorize(Roles = AppRoles.OrderCreationSales)]
    [ProducesResponseType(typeof(IReadOnlyList<AngebotUebersicht>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AngebotUebersicht>>> Angebote([FromQuery] bool nurMeine)
    {
        var liste = await angebotsService.ListeAsync(nurMeine ? User.GetUserId() : null);
        return Ok(liste);
    }

    [HttpGet("vertrieb/angebot/{id:int}")]
    [Authorize(Roles = AppRoles.OrderCreationSales)]
    [ProducesResponseType(typeof(AngebotDetailAntwort), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AngebotDetailAntwort>> AngebotDetail(int id)
    {
        var detail = await angebotsService.DetailAsync(id);
        if (detail is null)
            return NotFound($"Angebot mit Id {id} wurde nicht gefunden.");

        return Ok(detail);
    }

    [HttpGet("innendienst/bestaetigungen")]
    [Authorize(Roles = AppRoles.OrderCreationBackoffice)]
    [ProducesResponseType(typeof(IReadOnlyList<AngebotUebersicht>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AngebotUebersicht>>> Bestaetigungen([FromQuery] bool nurMeine)
    {
        var liste = await angebotsService.BestaetigungenListeAsync(nurMeine ? User.GetUserId() : null);
        return Ok(liste);
    }

    [HttpGet("innendienst/bestaetigung/{id:int}")]
    [Authorize(Roles = AppRoles.OrderCreationBackoffice)]
    [ProducesResponseType(typeof(BestaetigungDetailAntwort), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BestaetigungDetailAntwort>> BestaetigungDetail(int id)
    {
        var detail = await angebotsService.BestaetigungDetailAsync(id);
        if (detail is null)
            return NotFound($"Bestätigungsabgleich mit Id {id} wurde nicht gefunden.");

        return Ok(detail);
    }

    [HttpGet("innendienst/bestaetigung/{id:int}/dokument")]
    [Authorize(Roles = AppRoles.OrderCreationBackoffice)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BestaetigungDokument(int id)
    {
        try
        {
            var dokument = await angebotsService.BestaetigungDokumentAsync(id);
            return dokument is null ? NotFound() : File(dokument.Value.Inhalt, "application/pdf", dokument.Value.Dateiname);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Laden des Bestätigungs-Dokuments {Id}.", id);
            return StatusCode(500, "Das Dokument konnte nicht geladen werden. Bitte versuche es erneut.");
        }
    }

    [HttpPost("innendienst/bestaetigung")]
    [Authorize(Roles = AppRoles.OrderCreationBackoffice)]
    [ProducesResponseType(typeof(BestaetigungVergleichAntwort), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BestaetigungVergleichAntwort>> BestaetigungHochladen([FromForm] IFormFile datei)
    {
        var validierungsFehler = ValidiereDatei(datei);
        if (validierungsFehler is not null)
            return BadRequest(validierungsFehler);

        try
        {
            using var puffer = await InPufferLesenAsync(datei);
            var daten = await analyseService.AnalyzeAsync(puffer);

            var zuordnung = await angebotsService.FindeAngebotDurchVolltextsucheAsync(daten.Volltext);

            if (zuordnung.Status == AngebotZuordnungStatus.NichtGefunden)
                return NotFound("Kein Angebot gefunden, das in diesem Dokument referenziert wird. Bitte zuerst das Angebot über den Vertrieb hochladen.");

            if (zuordnung.Status == AngebotZuordnungStatus.Mehrdeutig)
                return Ok(new BestaetigungVergleichAntwort(
                    AngebotZuordnungStatus.Mehrdeutig, null, null, null, zuordnung.KandidatenNummern, null, null, false, [], [], null));

            if (zuordnung.Angebot is null)
                throw new InvalidOperationException(
                    "FindeAngebotDurchVolltextsucheAsync hat Status Gefunden ohne zugehöriges Angebot zurückgegeben.");

            var angebot = zuordnung.Angebot;
            var vergleich = await vergleichService.VergleicheAsync(angebot.Volltext, daten.Volltext);

            if (!vergleich.IstBestelldokument)
                return UnprocessableEntity(
                    string.IsNullOrWhiteSpace(vergleich.DokumentartHinweis)
                        ? "Das hochgeladene Dokument wurde nicht als Kundenbestellung erkannt. Bitte lade eine Bestellung oder eine eindeutig unterschriebene Angebotsannahme hoch."
                        : $"Das hochgeladene Dokument wurde nicht als Kundenbestellung erkannt. {vergleich.DokumentartHinweis}");

            puffer.Position = 0;
            var bestaetigungId = await angebotsService.BestaetigungSpeichernAsync(angebot.Id, daten, datei.FileName, puffer, vergleich, User.GetUserId());
            var angebotStatus = await angebotsService.ErstelleAngebotStatusAsync(angebot);

            var vorhandeneVersionen = await angebotsService.VorhandeneVersionenAsync(angebot.Angebotsnummer);

            return Ok(new BestaetigungVergleichAntwort(
                AngebotZuordnungStatus.Gefunden, angebot.Angebotsnummer, angebot.Version, angebot.Id, [],
                vergleich.LieferterminAngebot, vergleich.LieferterminBestaetigung,
                vergleich.LieferterminIdentisch, vergleich.Uebereinstimmungen,
                vergleich.SonstigeAbweichungen, angebotStatus, bestaetigungId, vorhandeneVersionen));
        }
        catch (Azure.RequestFailedException ex)
        {
            logger.LogWarning(ex, "Document Intelligence konnte Datei {Dateiname} nicht verarbeiten.", datei.FileName);
            return UnprocessableEntity("Das Dokument konnte nicht ausgelesen werden. Bitte ein gültiges PDF hochladen.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Bestätigungsabgleich für Datei {Dateiname}.", datei.FileName);
            return StatusCode(500, "Der Abgleich ist fehlgeschlagen. Bitte versuche es erneut.");
        }
    }

    [HttpPost("innendienst/bestaetigung/{id:int}/wechseln")]
    [Authorize(Roles = AppRoles.OrderCreationBackoffice)]
    [ProducesResponseType(typeof(BestaetigungVergleichAntwort), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BestaetigungVergleichAntwort>> BestaetigungWechseln(int id, [FromQuery] int version)
    {
        try
        {
            var ergebnis = await angebotsService.WechselnVersionAsync(id, version, vergleichService);
            if (ergebnis is null)
                return NotFound("Wechsel der Version fehlgeschlagen. Bestätigung, zugehöriges Angebot oder Ziel-Version nicht gefunden.");

            return Ok(ergebnis);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Wechseln der Version für Bestätigung {Id} auf Version {Version}.", id, version);
            return StatusCode(500, "Der Wechsel der Version ist fehlgeschlagen.");
        }
    }

    [HttpGet("angebot/{id:int}/dokument")]
    [Authorize(Roles = AppRoles.OrderCreation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AngebotDokument(int id)
    {
        try
        {
            var dokument = await angebotsService.AngebotDokumentAsync(id);
            return dokument is null ? NotFound() : File(dokument.Value.Inhalt, "application/pdf", dokument.Value.Dateiname);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Laden des Angebots-Dokuments {Id}.", id);
            return StatusCode(500, "Das Dokument konnte nicht geladen werden. Bitte versuche es erneut.");
        }
    }

    private static string? ValidiereDatei(IFormFile? datei)
    {
        if (datei is null || datei.Length == 0)
            return "Keine Datei hochgeladen.";
        if (datei.Length > MaxDateiBytes)
            return "Datei zu groß (max. 20 MB).";
        if (!string.Equals(datei.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return "Es werden nur PDF-Dateien unterstützt.";
        return null;
    }

    private static async Task<MemoryStream> InPufferLesenAsync(IFormFile datei)
    {
        var puffer = new MemoryStream();
        await using (var quelle = datei.OpenReadStream())
        {
            await quelle.CopyToAsync(puffer);
        }
        puffer.Position = 0;
        return puffer;
    }

    private static List<string> ErmittleFreigabefehler(
        AngebotsCheckliste checkliste, AngebotFreigebenAnfrage anfrage)
    {
        var fehler = new List<string>();
        // Haken ODER Kommentar genügt je SAP-Punkt (analog zu den übrigen Prüfpunkten).
        if (!anfrage.SapSparteBestaetigt && string.IsNullOrWhiteSpace(anfrage.SapSparteKommentar))
            fehler.Add("die richtige Sparte wurde weder in SAP bestätigt noch kommentiert");
        if (!anfrage.SapFuehrendBestaetigt && string.IsNullOrWhiteSpace(anfrage.SapFuehrendKommentar))
            fehler.Add("führendes Angebot und Zahlungsplan wurden weder in SAP bestätigt noch kommentiert");
        PruefeKommentar(checkliste.KundeVorhanden, anfrage.KundeKommentar, "Kunde/Adresse", fehler);
        PruefeKommentar(checkliste.KundenUndLieferadresseIdentisch, anfrage.LieferadresseKommentar,
            "Kundenadresse/Lieferadresse", fehler);
        PruefeKommentar(checkliste.ZahlungsbedingungenVorhanden && checkliste.ZahlungsbedingungStandardErkannt,
            anfrage.ZahlungsbedingungenKommentar, "Zahlungsbedingung", fehler);
        PruefeKommentar(checkliste.ZahlungsplanStandardErkannt, anfrage.ZahlungsplanKommentar, "Zahlungsplan", fehler);
        PruefeKommentar(checkliste.IncotermGueltig, anfrage.IncotermKommentar, "Incoterm", fehler);
        PruefeKommentar(checkliste.VersandbedingungErkannt, anfrage.VersandbedingungKommentar, "Versandart", fehler);
        PruefeKommentar(checkliste.VerkaeuferVorhanden, anfrage.VerkaeuferKommentar, "Verkäufer", fehler);
        PruefeKommentar(checkliste.LieferterminVorhanden, anfrage.LieferterminKommentar, "Liefertermin/-dauer", fehler);
        PruefeKommentar(checkliste.GueltigkeitsdatumGueltig, anfrage.GueltigkeitsdatumKommentar, "Gültigkeitsdatum", fehler);
        return fehler;
    }

    private static void PruefeKommentar(bool bestanden, string? kommentar, string bezeichnung, ICollection<string> fehler)
    {
        if (!bestanden && string.IsNullOrWhiteSpace(kommentar))
            fehler.Add($"Begründung für {bezeichnung} fehlt");
    }
}
