using Illig_AI_Platform.Shared.Auftragsinformationen;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Illig_AI_Platform.Services.Auftragsinformationen;

public record AuftragsinformationenImportErgebnis(
    int Verarbeitet,
    int Uebersprungen,
    int Geloescht,
    int Fehlgeschlagen,
    DateTime BeendetAm);

public sealed class AuftragsinformationenImportService(
    AppDbContext db,
    ISharePointDokumentClient sharePoint,
    IDocumentAnalyseService analyse,
    KundenstammService kundenstamm,
    IOptions<SharePointAuftragsinformationenOptions> options,
    ILogger<AuftragsinformationenImportService> logger)
{
    private enum EintragErgebnis { Verarbeitet, Uebersprungen, Fehlgeschlagen }

    private static readonly SemaphoreSlim ImportSperre = new(1, 1);
    private readonly SharePointAuftragsinformationenOptions _options = options.Value;

    public async Task<AuftragsinformationenImportErgebnis> SynchronisierenAsync(
        CancellationToken cancellationToken = default)
    {
        if (!await ImportSperre.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("Die SharePoint-Synchronisation laeuft bereits.");

        try
        {
            return await SynchronisierenKernAsync(cancellationToken);
        }
        finally
        {
            ImportSperre.Release();
        }
    }

    private async Task<AuftragsinformationenImportErgebnis> SynchronisierenKernAsync(
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("Der SharePoint-Auftragsinformationen-Import ist deaktiviert.");

        var stand = await db.SharePointSynchronisationsstaende
            .SingleOrDefaultAsync(s => s.Quelle == SharePointSynchronisationsstand.AuftragsinformationenQuelle, cancellationToken);
        if (stand is null)
        {
            stand = new SharePointSynchronisationsstand();
            db.SharePointSynchronisationsstaende.Add(stand);
        }

        stand.LetzterVersuchAm = DateTime.UtcNow;
        stand.LetzterFehler = null;
        await db.SaveChangesAsync(cancellationToken);

        var quelle = await sharePoint.QuelleAufloesenAsync(cancellationToken);
        if ((!string.IsNullOrWhiteSpace(stand.DriveId)
             && !string.Equals(stand.DriveId, quelle.DriveId, StringComparison.Ordinal))
            || (!string.IsNullOrWhiteSpace(stand.OrdnerItemId)
                && !string.Equals(stand.OrdnerItemId, quelle.OrdnerItemId, StringComparison.Ordinal)))
        {
            // Bei einer bewusst geaenderten Quelle ist ein alter Delta-Cursor ungueltig.
            stand.DeltaLink = null;
        }
        stand.DriveId = quelle.DriveId;
        stand.OrdnerItemId = quelle.OrdnerItemId;
        await db.SaveChangesAsync(cancellationToken);

        var verarbeitet = 0;
        var uebersprungen = 0;
        var geloescht = 0;
        var fehlgeschlagen = 0;
        var istErstimport = string.IsNullOrWhiteSpace(stand.DeltaLink);
        var stichtag = DateTime.UtcNow.AddYears(-Math.Max(1, _options.HistoricalYears));

        try
        {
            // Abgebrochene und fehlgeschlagene Analysen erneut versuchen, auch wenn der Delta-Cursor
            // den betreffenden SharePoint-Eintrag bereits passiert hat.
            var wiederholungen = await db.StuecklistenpruefungVerlaufEintraege.AsNoTracking()
                .Where(d => d.Quelle == AuftragsdokumentQuelle.SharePoint && d.GeloeschtAm == null
                    && (d.AnalyseStatus == AuftragsdokumentAnalyseStatus.Fehlgeschlagen
                        || (d.AnalyseStatus == AuftragsdokumentAnalyseStatus.InBearbeitung
                            && d.VerarbeitetAm < DateTime.UtcNow.AddHours(-2))))
                .OrderBy(d => d.VerarbeitetAm)
                .Take(50)
                .Select(d => new SharePointAenderung(
                    d.SharePointDriveId!, d.SharePointItemId!, d.Dateiname, d.ETag!, d.WebUrl!,
                    d.SharePointErstelltAm!.Value, d.SharePointGeaendertAm!.Value, true, false))
                .ToListAsync(cancellationToken);

            foreach (var wiederholung in wiederholungen)
            {
                var ergebnis = await VerarbeitenAsync(wiederholung, analyseErzwingen: true, cancellationToken);
                Zaehlen(ergebnis, ref verarbeitet, ref uebersprungen, ref fehlgeschlagen);
                db.ChangeTracker.Clear();
            }

            string? seitenUrl = stand.DeltaLink;
            string? neuerDeltaLink = null;
            do
            {
                var seite = await sharePoint.AenderungsseiteAsync(quelle, seitenUrl, cancellationToken);
                foreach (var eintrag in seite.Eintraege)
                {
                    if (eintrag.IstGeloescht)
                    {
                        if (await AlsGeloeschtMarkierenAsync(eintrag, cancellationToken))
                            geloescht++;
                        else
                            uebersprungen++;
                    }
                    else if (!eintrag.IstDatei
                             || !eintrag.Dateiname.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                             || (istErstimport && eintrag.GeaendertAm < stichtag))
                    {
                        uebersprungen++;
                    }
                    else
                    {
                        var ergebnis = await VerarbeitenAsync(eintrag, analyseErzwingen: false, cancellationToken);
                        Zaehlen(ergebnis, ref verarbeitet, ref uebersprungen, ref fehlgeschlagen);
                    }

                    db.ChangeTracker.Clear();
                }

                seitenUrl = seite.NaechsteSeite;
                neuerDeltaLink = seite.DeltaLink ?? neuerDeltaLink;
            } while (seitenUrl is not null);

            if (string.IsNullOrWhiteSpace(neuerDeltaLink))
                throw new InvalidOperationException("Microsoft Graph hat nach der Delta-Synchronisation keinen deltaLink geliefert.");

            db.ChangeTracker.Clear();
            stand = await db.SharePointSynchronisationsstaende
                .SingleAsync(s => s.Quelle == SharePointSynchronisationsstand.AuftragsinformationenQuelle, cancellationToken);
            stand.DeltaLink = neuerDeltaLink;
            stand.LetzterErfolgAm = DateTime.UtcNow;
            stand.LetzterFehler = null;
            await db.SaveChangesAsync(cancellationToken);

            return new AuftragsinformationenImportErgebnis(
                verarbeitet, uebersprungen, geloescht, fehlgeschlagen, DateTime.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            stand = await db.SharePointSynchronisationsstaende
                .SingleAsync(s => s.Quelle == SharePointSynchronisationsstand.AuftragsinformationenQuelle, cancellationToken);
            stand.LetzterFehler = Begrenzen(ex.Message, 4000);
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<EintragErgebnis> VerarbeitenAsync(
        SharePointAenderung eintrag,
        bool analyseErzwingen,
        CancellationToken cancellationToken)
    {
        var dokument = await db.StuecklistenpruefungVerlaufEintraege
            .SingleOrDefaultAsync(d => d.Quelle == AuftragsdokumentQuelle.SharePoint
                && d.SharePointDriveId == eintrag.DriveId
                && d.SharePointItemId == eintrag.ItemId, cancellationToken);

        if (!analyseErzwingen && dokument is not null
            && dokument.AnalyseStatus is AuftragsdokumentAnalyseStatus.Erfolgreich
                or AuftragsdokumentAnalyseStatus.Ignoriert
            && dokument.GeloeschtAm is null
            && string.Equals(dokument.ETag, eintrag.ETag, StringComparison.Ordinal))
            return EintragErgebnis.Uebersprungen;

        dokument ??= new StuecklistenpruefungVerlaufEintrag
        {
            Quelle = AuftragsdokumentQuelle.SharePoint,
            SharePointDriveId = eintrag.DriveId,
            SharePointItemId = eintrag.ItemId,
            ErstelltAm = DateTime.UtcNow,
        };
        if (dokument.Id == 0)
            db.StuecklistenpruefungVerlaufEintraege.Add(dokument);

        MetadatenUebernehmen(dokument, eintrag);
        dokument.AnalyseStatus = AuftragsdokumentAnalyseStatus.InBearbeitung;
        dokument.AnalyseFehler = null;
        dokument.VerarbeitetAm = DateTime.UtcNow;
        dokument.GeloeschtAm = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await using var inhalt = await sharePoint.OeffnenAsync(eintrag.WebUrl, cancellationToken);
            var ergebnis = await analyse.AnalyzeAsync(inhalt, cancellationToken);

            if (!ergebnis.IstAuftragsinformation)
            {
                dokument.AnalyseStatus = AuftragsdokumentAnalyseStatus.Ignoriert;
                dokument.AnalyseFehler = "Dokument wurde nicht als Auftragsinformation erkannt.";
                dokument.VerarbeitetAm = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                return EintragErgebnis.Uebersprungen;
            }

            if (string.IsNullOrWhiteSpace(ergebnis.Auftragsnummer))
                throw new InvalidOperationException("Auftragsinformation erkannt, aber keine Auftragsnummer extrahiert.");

            dokument.Auftragsnummer = ergebnis.Auftragsnummer;
            dokument.Kundennummer = ergebnis.Kundennummer;
            dokument.Kundenname = ergebnis.Kundenname;
            dokument.Kundenadresse = ergebnis.Kundenadresse;
            dokument.Datum = ergebnis.Datum;
            dokument.Maschinentyp = ergebnis.Maschinentyp;
            dokument.AnalyseStatus = AuftragsdokumentAnalyseStatus.Erfolgreich;
            dokument.AnalyseFehler = null;
            dokument.VerarbeitetAm = DateTime.UtcNow;

            // Kundenstamm genau wie beim Drag&Drop-Upload speisen (FindeOderErstelle + Quelle
            // registrieren), damit beide Wege dasselbe Ergebnis liefern. Beides ist idempotent, der
            // wiederkehrende SharePoint-Sync erzeugt also keine Dubletten.
            var kunde = await kundenstamm.FindeOderErstelleAsync(
                ergebnis.Kundenname, ergebnis.Kundenadresse, ergebnis.Kundennummer, cancellationToken);
            dokument.KundeId = kunde?.Id;

            var vorhandeneMerkmale = await db.VerlaufMerkmale
                .Where(m => m.VerlaufEintragId == dokument.Id)
                .ToListAsync(cancellationToken);
            db.VerlaufMerkmale.RemoveRange(vorhandeneMerkmale);
            db.VerlaufMerkmale.AddRange(
                ergebnis.Merkmale.Select(m => NeuesMerkmal(dokument.Id, MerkmalKategorie.Merkmal, m))
                    .Concat(ergebnis.Sonderoptionen.Select(m => NeuesMerkmal(dokument.Id, MerkmalKategorie.Sonderoption, m))));
            await db.SaveChangesAsync(cancellationToken);

            if (kunde is not null)
                await kundenstamm.RegistriereQuelleAsync(
                    kunde, KundenQuelltyp.Auftragsinformation, dokument.Id,
                    ergebnis.Kundenname, ergebnis.Kundenadresse, ergebnis.Kundennummer, cancellationToken);

            return EintragErgebnis.Verarbeitet;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            dokument.AnalyseStatus = AuftragsdokumentAnalyseStatus.Fehlgeschlagen;
            dokument.AnalyseFehler = Begrenzen(ex.Message, 4000);
            dokument.VerarbeitetAm = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning(ex,
                "SharePoint-Auftragsinformation {DriveId}/{ItemId} ({Dateiname}) konnte nicht verarbeitet werden.",
                eintrag.DriveId, eintrag.ItemId, eintrag.Dateiname);
            return EintragErgebnis.Fehlgeschlagen;
        }
    }

    private async Task<bool> AlsGeloeschtMarkierenAsync(
        SharePointAenderung eintrag,
        CancellationToken cancellationToken)
    {
        var dokument = await db.StuecklistenpruefungVerlaufEintraege
            .SingleOrDefaultAsync(d => d.Quelle == AuftragsdokumentQuelle.SharePoint
                && d.SharePointDriveId == eintrag.DriveId
                && d.SharePointItemId == eintrag.ItemId, cancellationToken);
        if (dokument is null || dokument.GeloeschtAm is not null)
            return false;

        dokument.GeloeschtAm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void MetadatenUebernehmen(StuecklistenpruefungVerlaufEintrag dokument, SharePointAenderung eintrag)
    {
        dokument.ETag = eintrag.ETag;
        dokument.Dateiname = eintrag.Dateiname;
        dokument.WebUrl = eintrag.WebUrl;
        dokument.SharePointErstelltAm = eintrag.ErstelltAm;
        dokument.SharePointGeaendertAm = eintrag.GeaendertAm;
    }

    private static VerlaufMerkmal NeuesMerkmal(
        int verlaufEintragId,
        MerkmalKategorie kategorie,
        ErkanntesMerkmal merkmal) => new()
    {
        VerlaufEintragId = verlaufEintragId,
        Kategorie = kategorie,
        Position = merkmal.Position,
        Merkmalsnummer = merkmal.Merkmalsnummer,
        Beschreibung = merkmal.Beschreibung,
    };

    private static string Begrenzen(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength];

    private static void Zaehlen(
        EintragErgebnis ergebnis,
        ref int verarbeitet,
        ref int uebersprungen,
        ref int fehlgeschlagen)
    {
        switch (ergebnis)
        {
            case EintragErgebnis.Verarbeitet:
                verarbeitet++;
                break;
            case EintragErgebnis.Uebersprungen:
                uebersprungen++;
                break;
            case EintragErgebnis.Fehlgeschlagen:
                fehlgeschlagen++;
                break;
        }
    }
}
