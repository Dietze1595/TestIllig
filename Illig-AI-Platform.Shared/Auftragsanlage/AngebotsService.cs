using System.Text.RegularExpressions;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Illig_AI_Platform.Shared.Auftragsanlage;

public enum KonfliktStrategie
{
    /// <summary>Kein expliziter Nutzerwunsch — bei vorhandener Nummer wird ein Konflikt gemeldet.</summary>
    Keine,
    Ueberschreiben,
    NeueVersion,
}

public record AngebotSpeichernErgebnis(bool Konflikt, Angebot? Angebot, IReadOnlyList<int> VorhandeneVersionen);

public enum AngebotZuordnungStatus
{
    Gefunden,
    NichtGefunden,
    Mehrdeutig,
}

public record AngebotZuordnungErgebnis(AngebotZuordnungStatus Status, Angebot? Angebot, IReadOnlyList<string> KandidatenNummern);

// Eigener Blob-Container "invoices" statt des mit der Stücklistenprüfung geteilten
// Containers — deshalb keyed statt der Standard-DI-Registrierung (siehe ServiceCollectionExtensions).
public class AngebotsService(
    AppDbContext db,
    [FromKeyedServices("auftragsanlage")] IBlobStorageService blobStorage,
    ILogger<AngebotsService>? logger = null,
    KundenstammService? kundenstamm = null)
{
    public async Task<AngebotSpeichernErgebnis> SpeichernAsync(
        ExtrahierteAngebotsdaten daten, string dateiname, Stream pdfInhalt,
        KonfliktStrategie strategie, VertriebsbedingungenErgebnis? vertriebsbedingungen = null,
        Guid? userProfileId = null, string? versionsKommentar = null, CancellationToken cancellationToken = default)
    {
        var nummer = daten.Nummer;
        if (string.IsNullOrWhiteSpace(nummer))
            throw new ArgumentException("Ohne Angebotsnummer kann kein Angebot gespeichert werden.", nameof(daten));

        var vorhandeneVersionen = await db.Angebote
            .Where(a => a.Angebotsnummer == nummer)
            .OrderByDescending(a => a.Version)
            .ToListAsync(cancellationToken);

        if (vorhandeneVersionen.Count > 0 && strategie == KonfliktStrategie.Keine)
            return new AngebotSpeichernErgebnis(true, null, [.. vorhandeneVersionen.Select(a => a.Version)]);

        var kunde = kundenstamm is null
            ? null
            : await kundenstamm.FindeOderErstelleAsync(
                daten.Kundenname, daten.Kundenadresse, null, cancellationToken);
        var ueberschreibt = strategie == KonfliktStrategie.Ueberschreiben && vorhandeneVersionen.Count > 0;
        var alterBlobPfad = ueberschreibt ? vorhandeneVersionen[0].BlobPfad : null;

        var neuerBlobPfad = await blobStorage.UploadAsync(pdfInhalt, dateiname, cancellationToken);

        try
        {
            Angebot angebot;
            if (ueberschreibt)
            {
                // „Überschreiben" ersetzt die neueste Version in place (die Id bleibt, damit
                // verknüpfte Auftragsbestätigungen erhalten bleiben — ein Hard-Delete würde sie
                // per FK-Cascade mitreißen). Der alte Blob wird nach erfolgreichem Commit gelöscht.
                angebot = vorhandeneVersionen[0];
            }
            else
            {
                angebot = new Angebot
                {
                    Angebotsnummer = nummer,
                    Version = vorhandeneVersionen.Count > 0 ? vorhandeneVersionen[0].Version + 1 : 1,
                };
                db.Angebote.Add(angebot);
            }

            angebot.Kundenname = daten.Kundenname;
            angebot.Kundenadresse = daten.Kundenadresse;
            angebot.VersionsKommentar = versionsKommentar;
            angebot.Lieferadresse = daten.Lieferadresse ??
                                      AngebotsLayoutParser.Parse(daten.Volltext).Lieferadresse;
            angebot.KundeId = kunde?.Id;
            angebot.Zahlungsbedingungen = vertriebsbedingungen?.Zahlungsbedingungen ?? daten.Zahlungsbedingungen;
            angebot.ZahlungsbedingungCode = vertriebsbedingungen?.ZahlungsbedingungCode ?? daten.ZahlungsbedingungCode;
            angebot.Zahlungsplan = vertriebsbedingungen?.Zahlungsplan ?? daten.Zahlungsplan;
            angebot.Verkaeufer = vertriebsbedingungen?.Verkaeufer ?? daten.Verkaeufer;
            angebot.Liefertermin = vertriebsbedingungen?.Liefertermin ?? daten.Liefertermin;
            angebot.GueltigBis = vertriebsbedingungen?.GueltigBis ?? daten.GueltigBis;
            angebot.Volltext = daten.Volltext;
            angebot.Incoterm = vertriebsbedingungen?.Incoterm;
            angebot.IncotermOrt = vertriebsbedingungen?.IncotermOrt;
            angebot.Versandbedingung = vertriebsbedingungen?.Versandbedingung;
            angebot.SapSparteBestaetigt = false;
            angebot.SapFuehrendBestaetigt = false;
            angebot.Freigegeben = false;
            angebot.FreigegebenAm = null;
            angebot.KundeKommentar = null;
            angebot.LieferadresseKommentar = null;
            angebot.ZahlungsbedingungenKommentar = null;
            angebot.IncotermKommentar = null;
            angebot.VersandbedingungKommentar = null;
            angebot.ZahlungsplanKommentar = null;
            angebot.VerkaeuferKommentar = null;
            angebot.LieferterminKommentar = null;
            angebot.GueltigkeitsdatumKommentar = null;
            angebot.Dateiname = dateiname;
            angebot.BlobPfad = neuerBlobPfad;
            angebot.HochgeladenAm = DateTime.UtcNow;
            angebot.UserProfileId = userProfileId;

            await db.SaveChangesAsync(cancellationToken);

            if (kunde is not null && kundenstamm is not null)
                await kundenstamm.RegistriereQuelleAsync(
                    kunde, KundenQuelltyp.Angebot, angebot.Id,
                    angebot.Kundenname, angebot.Kundenadresse, null, cancellationToken);

            // Erst nach erfolgreichem Commit den alten Blob entfernen (best-effort).
            if (alterBlobPfad is not null && alterBlobPfad != neuerBlobPfad)
                await LoescheBlobStillAsync(alterBlobPfad, cancellationToken);

            return new AngebotSpeichernErgebnis(false, angebot, []);
        }
        catch
        {
            // Speichern fehlgeschlagen → gerade hochgeladenen Blob wieder entfernen (kein Waise).
            await LoescheBlobStillAsync(neuerBlobPfad, cancellationToken);
            throw;
        }
    }

    public Task<Angebot?> NeuesteVersionAsync(string nummer, CancellationToken cancellationToken = default) =>
        db.Angebote
            .Where(a => a.Angebotsnummer == nummer)
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<int>> VorhandeneVersionenAsync(
        string nummer, CancellationToken cancellationToken = default) =>
        await db.Angebote
            .Where(a => a.Angebotsnummer == nummer)
            .OrderByDescending(a => a.Version)
            .Select(a => a.Version)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AngebotUebersicht>> ListeAsync(
        Guid? nurUserProfileId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Angebote.AsQueryable();
        if (nurUserProfileId is { } id)
            query = query.Where(a => a.UserProfileId == id);

        return await (
            from angebot in query
            join profil in db.UserProfiles
                on angebot.UserProfileId equals (Guid?)profil.Id into profile
            from profil in profile.DefaultIfEmpty()
            orderby angebot.HochgeladenAm descending
            select new AngebotUebersicht(
                angebot.Id,
                angebot.Angebotsnummer,
                angebot.Version,
                angebot.Kundenname,
                profil == null
                    ? null
                    : profil.DisplayName != ""
                        ? profil.DisplayName
                        : profil.FullName != ""
                            ? profil.FullName
                            : profil.Email,
                angebot.HochgeladenAm,
                angebot.Freigegeben))
            .ToListAsync(cancellationToken);
    }

    public async Task<AngebotDetailAntwort?> DetailAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var angebot = await db.Angebote.FindAsync([id], cancellationToken);
        if (angebot is null)
            return null;

        var layoutDaten = AngebotsLayoutParser.Parse(angebot.Volltext);
        var lieferadresse = angebot.Lieferadresse ?? layoutDaten.Lieferadresse;
        var checkliste = AngebotsCheckliste.Berechnen(
            angebot.Kundenname, angebot.Kundenadresse, angebot.Angebotsnummer,
            angebot.Zahlungsbedingungen, angebot.Incoterm, angebot.IncotermOrt, angebot.Versandbedingung,
            angebot.ZahlungsbedingungCode, angebot.Zahlungsplan, angebot.Verkaeufer,
            angebot.Liefertermin, angebot.GueltigBis, lieferadresse);

        var daten = new ExtrahierteAngebotsdaten(
            angebot.Angebotsnummer, angebot.Kundenname, angebot.Kundenadresse,
            angebot.Zahlungsbedingungen, [], angebot.Volltext,
            angebot.ZahlungsbedingungCode, angebot.Zahlungsplan, angebot.Verkaeufer,
            angebot.Liefertermin, angebot.GueltigBis,
            layoutDaten.Gesamtpreis,
            Lieferadresse: lieferadresse);

        return new AngebotDetailAntwort(
            angebot.Id, angebot.Angebotsnummer, angebot.Version, angebot.Dateiname,
            checkliste, daten, angebot.Incoterm, angebot.IncotermOrt, angebot.Versandbedingung,
            angebot.KundeKommentar, angebot.ZahlungsbedingungenKommentar,
            angebot.IncotermKommentar, angebot.VersandbedingungKommentar,
            angebot.ZahlungsplanKommentar, angebot.VerkaeuferKommentar,
            angebot.LieferterminKommentar, angebot.GueltigkeitsdatumKommentar,
            angebot.Freigegeben, angebot.FreigegebenAm,
            angebot.SapSparteBestaetigt, angebot.SapFuehrendBestaetigt,
            angebot.LieferadresseKommentar, angebot.UserProfileId,
            angebot.SapSparteKommentar, angebot.SapFuehrendKommentar);
    }

    public async Task<(string Dateiname, Stream Inhalt)?> AngebotDokumentAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var angebot = await db.Angebote.FindAsync([id], cancellationToken);
        if (angebot is null)
            return null;

        var inhalt = await blobStorage.OpenReadAsync(angebot.BlobPfad, cancellationToken);
        return (angebot.Dateiname, inhalt);
    }

    // Die Kundenbestellung trägt die Angebotsnummer meist nur als Fließtext in einer
    // Positionsbeschreibung, nicht in einem strukturierten Nummernfeld (siehe Spec,
    // Update 2026-07-16) — deshalb Volltextsuche statt Feld-Gleichheit. (?<!\d)/(?!\d)
    // verhindert Fehltreffer, wenn eine kürzere Nummer Teil einer längeren Ziffernfolge
    // ist, lässt aber — anders als \b — auch einen direkt angrenzenden Buchstaben ohne
    // Leerzeichen zu (OCR lässt das Leerzeichen zwischen Zahl und Folgewort manchmal weg,
    // z. B. "50214045from" statt "50214045 from" in einer echten Kundenbestellung).
    public async Task<AngebotZuordnungErgebnis> FindeAngebotDurchVolltextsucheAsync(
        string volltext, CancellationToken cancellationToken = default)
    {
        var bekannteNummern = await db.Angebote
            .Select(a => a.Angebotsnummer)
            .Distinct()
            .ToListAsync(cancellationToken);

        var treffer = bekannteNummern
            .Where(nummer => Regex.IsMatch(volltext, $@"(?<!\d){Regex.Escape(nummer)}(?!\d)"))
            .ToList();

        if (treffer.Count == 0)
            return new AngebotZuordnungErgebnis(AngebotZuordnungStatus.NichtGefunden, null, []);

        if (treffer.Count > 1)
            return new AngebotZuordnungErgebnis(AngebotZuordnungStatus.Mehrdeutig, null, treffer);

        var angebot = await NeuesteVersionAsync(treffer[0], cancellationToken);
        return new AngebotZuordnungErgebnis(AngebotZuordnungStatus.Gefunden, angebot, []);
    }

    public async Task<Angebot?> FreigebenAsync(
        int angebotId, string? kundeKommentar = null, string? zahlungsbedingungenKommentar = null,
        string? incotermKommentar = null, string? versandbedingungKommentar = null,
        string? zahlungsplanKommentar = null, string? verkaeuferKommentar = null,
        string? lieferterminKommentar = null,
        string? gueltigkeitsdatumKommentar = null,
        bool sapSparteBestaetigt = false,
        bool sapFuehrendBestaetigt = false,
        Guid? freigegebenVonUserProfileId = null,
        string? lieferadresseKommentar = null,
        string? sapSparteKommentar = null,
        string? sapFuehrendKommentar = null,
        CancellationToken cancellationToken = default)
    {
        var angebot = await db.Angebote.FindAsync([angebotId], cancellationToken);
        if (angebot is null)
            return null;

        angebot.Freigegeben = true;
        angebot.FreigegebenAm = DateTime.UtcNow;
        angebot.FreigegebenVonUserProfileId = freigegebenVonUserProfileId;
        angebot.SapSparteBestaetigt = sapSparteBestaetigt;
        angebot.SapFuehrendBestaetigt = sapFuehrendBestaetigt;
        angebot.SapSparteKommentar = sapSparteKommentar;
        angebot.SapFuehrendKommentar = sapFuehrendKommentar;
        angebot.KundeKommentar = kundeKommentar;
        angebot.LieferadresseKommentar = lieferadresseKommentar;
        angebot.ZahlungsbedingungenKommentar = zahlungsbedingungenKommentar;
        angebot.IncotermKommentar = incotermKommentar;
        angebot.VersandbedingungKommentar = versandbedingungKommentar;
        angebot.ZahlungsplanKommentar = zahlungsplanKommentar;
        angebot.VerkaeuferKommentar = verkaeuferKommentar;
        angebot.LieferterminKommentar = lieferterminKommentar;
        angebot.GueltigkeitsdatumKommentar = gueltigkeitsdatumKommentar;
        await db.SaveChangesAsync(cancellationToken);

        return angebot;
    }

    public async Task<IReadOnlyList<AngebotUebersicht>> BestaetigungenListeAsync(
        Guid? nurUserProfileId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Auftragsbestaetigungen.AsQueryable();
        if (nurUserProfileId is { } id)
            query = query.Where(b => b.UserProfileId == id);

        return await (
            from bestaetigung in query
            join angebot in db.Angebote on bestaetigung.AngebotId equals angebot.Id
            join profil in db.UserProfiles
                on bestaetigung.UserProfileId equals (Guid?)profil.Id into profile
            from profil in profile.DefaultIfEmpty()
            orderby bestaetigung.HochgeladenAm descending
            select new AngebotUebersicht(
                bestaetigung.Id,
                angebot.Angebotsnummer,
                angebot.Version,
                angebot.Kundenname,
                profil == null
                    ? null
                    : profil.DisplayName != ""
                        ? profil.DisplayName
                        : profil.FullName != ""
                            ? profil.FullName
                            : profil.Email,
                bestaetigung.HochgeladenAm,
                angebot.Freigegeben))
            .ToListAsync(cancellationToken);
    }

    public async Task<BestaetigungDetailAntwort?> BestaetigungDetailAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var eintrag = await (
            from b in db.Auftragsbestaetigungen
            join a in db.Angebote on b.AngebotId equals a.Id
            where b.Id == id
            select new { Bestaetigung = b, Angebot = a })
            .SingleOrDefaultAsync(cancellationToken);

        if (eintrag is null)
            return null;

        var bestaetigung = eintrag.Bestaetigung;
        var angebot = eintrag.Angebot;
        var gespeichertePruefpunkte = bestaetigung.SonstigeAbweichungen
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        const string identischPraefix = "[IDENTISCH] ";
        var uebereinstimmungen = gespeichertePruefpunkte
            .Where(x => x.StartsWith(identischPraefix, StringComparison.Ordinal) ||
                        IstEindeutigeHistorischeUebereinstimmung(x))
            .Select(x => x.StartsWith(identischPraefix, StringComparison.Ordinal)
                ? x[identischPraefix.Length..]
                : x)
            .ToArray();
        var abweichungen = gespeichertePruefpunkte
            .Where(x => !x.StartsWith(identischPraefix, StringComparison.Ordinal) &&
                        !IstEindeutigeHistorischeUebereinstimmung(x))
            .ToArray();

        var vorhandeneVersionen = await VorhandeneVersionenAsync(angebot.Angebotsnummer, cancellationToken);
        var vergleich = new BestaetigungVergleichAntwort(
            AngebotZuordnungStatus.Gefunden,
            angebot.Angebotsnummer,
            angebot.Version,
            angebot.Id,
            [],
            bestaetigung.LieferterminAngebot,
            bestaetigung.LieferterminBestaetigung,
            bestaetigung.LieferterminIdentisch,
            uebereinstimmungen,
            abweichungen,
            await ErstelleAngebotStatusAsync(angebot, cancellationToken),
            bestaetigung.Id,
            vorhandeneVersionen);

        return new BestaetigungDetailAntwort(bestaetigung.Id, bestaetigung.Dateiname, vergleich);
    }

    private static bool IstEindeutigeHistorischeUebereinstimmung(string pruefpunkt)
    {
        var text = pruefpunkt.ToLowerInvariant();
        var positiv = text.Contains("identisch", StringComparison.Ordinal) ||
                      text.Contains("stimmt überein", StringComparison.Ordinal) ||
                      text.Contains("stimmen überein", StringComparison.Ordinal) ||
                      text.Contains("inhaltlich gleich", StringComparison.Ordinal) ||
                      text.Contains("keine abweichung erkennbar", StringComparison.Ordinal);
        if (!positiv)
            return false;

        var ohnePositiveAbweichungsformulierung = text
            .Replace("keine abweichung erkennbar", "", StringComparison.Ordinal);

        return !ohnePositiveAbweichungsformulierung.Contains("zusätzlich", StringComparison.Ordinal) &&
               !ohnePositiveAbweichungsformulierung.Contains("jedoch", StringComparison.Ordinal) &&
               !ohnePositiveAbweichungsformulierung.Contains("nicht enthalten", StringComparison.Ordinal) &&
               !ohnePositiveAbweichungsformulierung.Contains("nur im ", StringComparison.Ordinal) &&
               !ohnePositiveAbweichungsformulierung.Contains("nur in ", StringComparison.Ordinal) &&
               !ohnePositiveAbweichungsformulierung.Contains("weicht ab", StringComparison.Ordinal) &&
               !ohnePositiveAbweichungsformulierung.Contains("abweichend", StringComparison.Ordinal) &&
               !ohnePositiveAbweichungsformulierung.Contains("unterschied", StringComparison.Ordinal);
    }

    public async Task<(string Dateiname, Stream Inhalt)?> BestaetigungDokumentAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var bestaetigung = await db.Auftragsbestaetigungen.FindAsync([id], cancellationToken);
        if (bestaetigung is null)
            return null;

        var inhalt = await blobStorage.OpenReadAsync(bestaetigung.BlobPfad, cancellationToken);
        return (bestaetigung.Dateiname, inhalt);
    }

    public async Task<int> BestaetigungSpeichernAsync(
        int angebotId, ExtrahierteAngebotsdaten daten, string dateiname, Stream pdfInhalt,
        AngebotsVergleichLlmErgebnis vergleich, Guid? userProfileId = null, CancellationToken cancellationToken = default)
    {
        var blobPfad = await blobStorage.UploadAsync(pdfInhalt, dateiname, cancellationToken);
        var angebot = await db.Angebote.FindAsync([angebotId], cancellationToken);
        var kunde = angebot?.KundeId is int kundeId
            ? await db.Kunden.FindAsync([kundeId], cancellationToken)
            : kundenstamm is null
                ? null
                : await kundenstamm.FindeOderErstelleAsync(
                    daten.Kundenname, daten.Kundenadresse, null, cancellationToken);

        var bestaetigung = new Auftragsbestaetigung
        {
            AngebotId = angebotId,
            KundeId = kunde?.Id,
            Nummer = daten.Nummer ?? "",
            Kundenname = daten.Kundenname,
            Kundenadresse = daten.Kundenadresse,
            Zahlungsbedingungen = daten.Zahlungsbedingungen,
            Volltext = daten.Volltext,
            LieferterminAngebot = vergleich.LieferterminAngebot,
            LieferterminBestaetigung = vergleich.LieferterminBestaetigung,
            LieferterminIdentisch = vergleich.LieferterminIdentisch,
            // Bestehende Spalte bleibt aus Kompatibilitätsgründen erhalten. Neue
            // Übereinstimmungen tragen ein eindeutiges Präfix; alte Zeilen ohne Präfix
            // werden beim Laden weiterhin als Abweichungen behandelt.
            SonstigeAbweichungen = string.Join('\n',
                vergleich.Uebereinstimmungen.Select(x => $"[IDENTISCH] {x}")
                    .Concat(vergleich.SonstigeAbweichungen)),
            Dateiname = dateiname,
            BlobPfad = blobPfad,
            HochgeladenAm = DateTime.UtcNow,
            UserProfileId = userProfileId,
        };
        db.Auftragsbestaetigungen.Add(bestaetigung);

        await db.SaveChangesAsync(cancellationToken);

        if (kunde is not null && kundenstamm is not null)
            await kundenstamm.RegistriereQuelleAsync(
                kunde, KundenQuelltyp.Kundenbestellung, bestaetigung.Id,
                bestaetigung.Kundenname, bestaetigung.Kundenadresse, null, cancellationToken);

        return bestaetigung.Id;
    }

    public async Task<AngebotStatusAntwort> ErstelleAngebotStatusAsync(
        Angebot angebot, CancellationToken cancellationToken = default)
    {
        var lieferadresse = angebot.Lieferadresse ??
                            AngebotsLayoutParser.Parse(angebot.Volltext).Lieferadresse;
        var checkliste = AngebotsCheckliste.Berechnen(
            angebot.Kundenname, angebot.Kundenadresse, angebot.Angebotsnummer,
            angebot.Zahlungsbedingungen, angebot.Incoterm, angebot.IncotermOrt, angebot.Versandbedingung,
            angebot.ZahlungsbedingungCode, angebot.Zahlungsplan, angebot.Verkaeufer,
            angebot.Liefertermin, angebot.GueltigBis, lieferadresse);
        var freigegebenVonUserProfileId =
            angebot.FreigegebenVonUserProfileId ?? angebot.UserProfileId;
        var freigegebenVon = angebot.Freigegeben && freigegebenVonUserProfileId is { } userProfileId
            ? await db.UserProfiles
                .Where(profil => profil.Id == userProfileId)
                .Select(profil => profil.DisplayName != ""
                    ? profil.DisplayName
                    : profil.FullName != ""
                        ? profil.FullName
                        : profil.Email)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        return new AngebotStatusAntwort(
            checkliste, angebot.Kundenname, angebot.Kundenadresse, angebot.Zahlungsbedingungen,
            angebot.Incoterm, angebot.IncotermOrt, angebot.Versandbedingung,
            angebot.ZahlungsbedingungCode, angebot.Zahlungsplan, angebot.Verkaeufer,
            angebot.Liefertermin, angebot.GueltigBis,
            angebot.KundeKommentar, angebot.ZahlungsbedingungenKommentar,
            angebot.IncotermKommentar, angebot.VersandbedingungKommentar,
            angebot.ZahlungsplanKommentar, angebot.VerkaeuferKommentar,
            angebot.LieferterminKommentar, angebot.GueltigkeitsdatumKommentar,
            angebot.Freigegeben, angebot.FreigegebenAm, freigegebenVon,
            angebot.SapSparteBestaetigt, angebot.SapFuehrendBestaetigt,
            lieferadresse, angebot.LieferadresseKommentar,
            angebot.SapSparteKommentar, angebot.SapFuehrendKommentar,
            angebot.VersionsKommentar);
    }

    private async Task LoescheBlobStillAsync(string blobPfad, CancellationToken cancellationToken)
    {
        try
        {
            await blobStorage.DeleteAsync(blobPfad, cancellationToken);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Blob {BlobPfad} konnte nicht gelöscht werden.", blobPfad);
        }
    }

    public async Task<BestaetigungVergleichAntwort?> WechselnVersionAsync(
        int bestaetigungId, int targetVersion, IAngebotsvergleichLlmService vergleichService, CancellationToken cancellationToken = default)
    {
        var bestaetigung = await db.Auftragsbestaetigungen.FindAsync([bestaetigungId], cancellationToken);
        if (bestaetigung is null)
            return null;

        var currentAngebot = await db.Angebote.FindAsync([bestaetigung.AngebotId], cancellationToken);
        if (currentAngebot is null)
            return null;

        var zielAngebot = await db.Angebote
            .FirstOrDefaultAsync(a => a.Angebotsnummer == currentAngebot.Angebotsnummer && a.Version == targetVersion, cancellationToken);
        if (zielAngebot is null)
            return null;

        var vergleich = await vergleichService.VergleicheAsync(zielAngebot.Volltext, bestaetigung.Volltext);

        bestaetigung.AngebotId = zielAngebot.Id;
        bestaetigung.LieferterminAngebot = vergleich.LieferterminAngebot;
        bestaetigung.LieferterminBestaetigung = vergleich.LieferterminBestaetigung;
        bestaetigung.LieferterminIdentisch = vergleich.LieferterminIdentisch;
        bestaetigung.SonstigeAbweichungen = string.Join('\n',
            vergleich.Uebereinstimmungen.Select(x => $"[IDENTISCH] {x}")
                .Concat(vergleich.SonstigeAbweichungen));

        await db.SaveChangesAsync(cancellationToken);

        var angebotStatus = await ErstelleAngebotStatusAsync(zielAngebot, cancellationToken);
        var vorhandeneVersionen = await VorhandeneVersionenAsync(zielAngebot.Angebotsnummer, cancellationToken);

        return new BestaetigungVergleichAntwort(
            AngebotZuordnungStatus.Gefunden, zielAngebot.Angebotsnummer, zielAngebot.Version, zielAngebot.Id, [],
            vergleich.LieferterminAngebot, vergleich.LieferterminBestaetigung,
            vergleich.LieferterminIdentisch, vergleich.Uebereinstimmungen,
            vergleich.SonstigeAbweichungen, angebotStatus, bestaetigung.Id, vorhandeneVersionen);
    }
}
