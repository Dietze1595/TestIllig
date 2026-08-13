using Illig_AI_Platform.Shared.Plausibilitaetspruefung;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Services.Auftragsinformationen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public class StuecklistenpruefungVerlaufService(
    AppDbContext db,
    IBlobStorageService blobStorage,
    KundenstammService? kundenstamm = null,
    ILogger<StuecklistenpruefungVerlaufService>? logger = null,
    ISharePointDokumentClient? sharePointClient = null)
{
    public async Task<int> SpeichernAsync(
        Guid userProfileId, string dateiname, Stream pdfInhalt, DokumentAnalyseErgebnis ergebnis,
        CancellationToken cancellationToken = default)
    {
        var blobPfad = await blobStorage.UploadAsync(pdfInhalt, dateiname, cancellationToken);
        var kunde = kundenstamm is null
            ? null
            : await kundenstamm.FindeOderErstelleAsync(
                ergebnis.Kundenname, ergebnis.Kundenadresse, ergebnis.Kundennummer, cancellationToken);

        var id = await InsertEintragAsync(userProfileId, dateiname, blobPfad, kunde?.Id, ergebnis, cancellationToken);

        if (kunde is not null && kundenstamm is not null)
            await kundenstamm.RegistriereQuelleAsync(
                kunde, KundenQuelltyp.Auftragsinformation, id,
                ergebnis.Kundenname, ergebnis.Kundenadresse, ergebnis.Kundennummer, cancellationToken);

        return id;
    }

    // Legt Zeile + VerlaufMerkmale an. Blob-Upload und Kundenstamm-Registrierung liegen bewusst
    // NICHT hier — so kann der Duplikat-Pfad (fremder Auftrag) einen bereits vorhandenen Blob und
    // dieselbe Kunden-Zuordnung wiederverwenden, ohne das Dokument/den Kunden erneut abzulegen.
    private async Task<int> InsertEintragAsync(
        Guid userProfileId, string dateiname, string blobPfad, int? kundeId,
        DokumentAnalyseErgebnis ergebnis, CancellationToken cancellationToken)
    {
        var eintrag = new StuecklistenpruefungVerlaufEintrag
        {
            KundeId = kundeId,
            UserProfileId = userProfileId,
            Dateiname = dateiname,
            BlobPfad = blobPfad,
            Auftragsnummer = ergebnis.Auftragsnummer,
            Kundennummer = ergebnis.Kundennummer,
            Kundenname = ergebnis.Kundenname,
            Kundenadresse = ergebnis.Kundenadresse,
            Datum = ergebnis.Datum,
            Maschinentyp = ergebnis.Maschinentyp,
            ErstelltAm = DateTime.UtcNow,
            ErreichterSchritt = 2,
        };
        db.StuecklistenpruefungVerlaufEintraege.Add(eintrag);
        await db.SaveChangesAsync(cancellationToken);

        db.VerlaufMerkmale.AddRange(
            ergebnis.Merkmale.Select(m => NeuesVerlaufMerkmal(eintrag.Id, MerkmalKategorie.Merkmal, m))
                .Concat(ergebnis.Sonderoptionen.Select(m => NeuesVerlaufMerkmal(eintrag.Id, MerkmalKategorie.Sonderoption, m))));
        await db.SaveChangesAsync(cancellationToken);

        return eintrag.Id;
    }

    /// <summary>
    /// Duplikat-bewusster Einstieg für den Upload. Schlüssel ist (Auftragsnummer, Kundennummer):
    /// <list type="number">
    /// <item>Der aktuelle User hat selbst schon einen Eintrag → nichts anlegen, gespeicherten Stand
    /// (weitest fortgeschritten) als <see cref="VerlaufDetail"/> zurückgeben.</item>
    /// <item>Ein anderer User hat einen Eintrag → eigener Eintrag für den aktuellen User, aber Blob
    /// und Kunden-Zuordnung vom ältesten Treffer wiederverwenden (Dokument/Kunde nicht erneut ablegen),
    /// Merkmale aus dem frischen Parse, Schritt 2.</item>
    /// <item>Kein Treffer → voller Speicherpfad wie bisher (<see cref="SpeichernAsync"/>).</item>
    /// </list>
    /// </summary>
    public async Task<(int VerlaufId, VerlaufDetail? BestehenderEintrag)> SpeichernOderOeffnenAsync(
        Guid userProfileId, string dateiname, Stream pdfInhalt, DokumentAnalyseErgebnis ergebnis,
        CancellationToken cancellationToken = default)
    {
        // Nur Drag&Drop-Einträge sind für die Duplikaterkennung relevant. SharePoint-synchronisierte
        // Einträge haben keinen Blob (Original liegt in SharePoint) und keinen Uploader — würden sie
        // hier als Blob-/Kunden-Quelle (Fall 2) dienen, entstünde ein Drag&Drop-Eintrag ohne Blob,
        // dessen hochgeladenes Dokument verloren ginge. Beide Wege sollen unabhängig funktionieren:
        // Der SharePoint-Sync läuft nur täglich, ein Drag&Drop-Upload desselben Auftrags muss trotzdem
        // ein eigenes, abrufbares Dokument anlegen (Fall 3).
        var treffer = await db.StuecklistenpruefungVerlaufEintraege
            .Where(e => e.Quelle == AuftragsdokumentQuelle.DragAndDrop && e.GeloeschtAm == null
                && e.Auftragsnummer == ergebnis.Auftragsnummer && e.Kundennummer == ergebnis.Kundennummer)
            .ToListAsync(cancellationToken);

        // Fall 1: eigener Eintrag vorhanden -> öffnen, nichts anlegen.
        var eigener = treffer
            .Where(e => e.UserProfileId == userProfileId)
            .OrderByDescending(e => e.ErreichterSchritt)
            .ThenByDescending(e => e.ErstelltAm)
            .FirstOrDefault();
        if (eigener is not null)
            return (eigener.Id, await BaueDetailAsync(eigener, cancellationToken));

        // Fall 2: fremder Eintrag vorhanden -> eigener Eintrag, Blob/Kunde vom ältesten Treffer übernehmen.
        var quelle = treffer.OrderBy(e => e.ErstelltAm).FirstOrDefault();
        if (quelle is not null)
        {
            var id = await InsertEintragAsync(
                userProfileId, dateiname, quelle.BlobPfad, quelle.KundeId, ergebnis, cancellationToken);
            return (id, null);
        }

        // Fall 3: ganz neu.
        var neueId = await SpeichernAsync(userProfileId, dateiname, pdfInhalt, ergebnis, cancellationToken);
        return (neueId, null);
    }

    public async Task<bool> StandAktualisierenAsync(
        Guid userProfileId, int id, VerlaufStandAktualisieren stand,
        CancellationToken cancellationToken = default)
    {
        // SharePoint-synchronisierte Einträge haben zunächst keine UserProfileId. Sobald ein User
        // an einem solchen Eintrag arbeitet (z. B. einen SAP-Stücklistenvergleich durchführt), wird
        // die UserProfileId hier hinterlegt — der Eintrag "gehört" ab dann diesem User und taucht
        // fortan auch unter "Alle"/"Meine" auf, statt nur unter "SharePoint".
        var eintrag = await db.StuecklistenpruefungVerlaufEintraege
            .FirstOrDefaultAsync(
                e => e.Id == id && (e.UserProfileId == userProfileId || e.UserProfileId == null),
                cancellationToken);
        if (eintrag is null)
            return false;

        var vorhandeneMerkmale = await db.VerlaufMerkmale
            .Where(m => m.VerlaufEintragId == id)
            .ToListAsync(cancellationToken);
        db.VerlaufMerkmale.RemoveRange(vorhandeneMerkmale);
        db.VerlaufMerkmale.AddRange(
            stand.Merkmale.Select(m => NeuesVerlaufMerkmal(id, MerkmalKategorie.Merkmal, m))
                .Concat(stand.Sonderoptionen.Select(m => NeuesVerlaufMerkmal(id, MerkmalKategorie.Sonderoption, m))));

        eintrag.ErreichterSchritt = Math.Clamp(stand.Schritt, 2, 4);
        eintrag.StuecklisteJson = stand.Stueckliste is null
            ? null
            : JsonSerializer.Serialize(stand.Stueckliste);

        if (eintrag.ErreichterSchritt >= 4)
        {
            eintrag.VergleichsErgebnisJson = stand.VergleichsErgebnis is null
                ? null
                : JsonSerializer.Serialize(stand.VergleichsErgebnis);
            eintrag.SapDateiname = stand.SapDateiname;

            eintrag.UserProfileId ??= userProfileId;
        }
        else
        {
            eintrag.VergleichsErgebnisJson = null;
            eintrag.SapDateiname = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> NeuEinlesenAsync(
        Guid userProfileId,
        int id,
        string dateiname,
        Stream pdfInhalt,
        DokumentAnalyseErgebnis ergebnis,
        CancellationToken cancellationToken = default)
    {
        var eintrag = await db.StuecklistenpruefungVerlaufEintraege
            .FirstOrDefaultAsync(e => e.Id == id && e.UserProfileId == userProfileId, cancellationToken);
        if (eintrag is null)
            return false;

        if (!string.Equals(eintrag.Auftragsnummer, ergebnis.Auftragsnummer, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(eintrag.Kundennummer, ergebnis.Kundennummer, StringComparison.OrdinalIgnoreCase))
            return false;

        var alterBlobPfad = eintrag.BlobPfad;
        var neuerBlobPfad = await blobStorage.UploadAsync(pdfInhalt, dateiname, cancellationToken);

        try
        {
            var kunde = kundenstamm is null
                ? null
                : await kundenstamm.FindeOderErstelleAsync(
                    ergebnis.Kundenname, ergebnis.Kundenadresse, ergebnis.Kundennummer, cancellationToken);

            var vorhandeneMerkmale = await db.VerlaufMerkmale
                .Where(m => m.VerlaufEintragId == id)
                .ToListAsync(cancellationToken);
            db.VerlaufMerkmale.RemoveRange(vorhandeneMerkmale);
            db.VerlaufMerkmale.AddRange(
                ergebnis.Merkmale.Select(m => NeuesVerlaufMerkmal(id, MerkmalKategorie.Merkmal, m))
                    .Concat(ergebnis.Sonderoptionen.Select(m =>
                        NeuesVerlaufMerkmal(id, MerkmalKategorie.Sonderoption, m))));

            eintrag.KundeId = kunde?.Id ?? eintrag.KundeId;
            eintrag.Dateiname = dateiname;
            eintrag.BlobPfad = neuerBlobPfad;
            eintrag.Kundenname = ergebnis.Kundenname;
            eintrag.Kundenadresse = ergebnis.Kundenadresse;
            eintrag.Datum = ergebnis.Datum;
            eintrag.Maschinentyp = ergebnis.Maschinentyp;
            eintrag.ErstelltAm = DateTime.UtcNow;
            eintrag.ErreichterSchritt = 2;
            eintrag.StuecklisteJson = null;
            eintrag.VergleichsErgebnisJson = null;
            eintrag.SapDateiname = null;

            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await LoescheBlobStillAsync(neuerBlobPfad, cancellationToken);
            throw;
        }

        var alterBlobWirdNochVerwendet = await db.StuecklistenpruefungVerlaufEintraege
            .AnyAsync(e => e.Id != id && e.BlobPfad == alterBlobPfad, cancellationToken);
        if (!alterBlobWirdNochVerwendet && alterBlobPfad != neuerBlobPfad)
            await LoescheBlobStillAsync(alterBlobPfad, cancellationToken);

        return true;
    }

    private async Task LoescheBlobStillAsync(string blobPfad, CancellationToken cancellationToken)
    {
        try
        {
            await blobStorage.DeleteAsync(blobPfad, cancellationToken);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Blob {BlobPfad} konnte nicht bereinigt werden.", blobPfad);
        }
    }

    private static VerlaufMerkmal NeuesVerlaufMerkmal(int eintragId, MerkmalKategorie kategorie, ErkanntesMerkmal merkmal) => new()
    {
        VerlaufEintragId = eintragId,
        Kategorie = kategorie,
        Position = merkmal.Position,
        Merkmalsnummer = merkmal.Merkmalsnummer,
        Beschreibung = merkmal.Beschreibung,
    };

    public async Task<IReadOnlyList<VerlaufEintragUebersicht>> ListeAsync(
        Guid? nurUserProfileId = null, CancellationToken cancellationToken = default)
    {
        // SharePoint-synchronisierte Einträge haben keine UserProfileId und werden bereits über den
        // separaten SharePoint-Endpunkt (AuftragsdokumentService) angezeigt. Ohne diesen Ausschluss
        // würden sie bei "Alle" doppelt auftauchen (hier + in der SharePoint-Sektion).
        var query = db.StuecklistenpruefungVerlaufEintraege
            .AsNoTracking()
            .Where(e => e.UserProfileId != null);
        if (nurUserProfileId is { } userProfileId)
            query = query.Where(e => e.UserProfileId == userProfileId);

        return await (
            from eintrag in query
            join profil in db.UserProfiles.AsNoTracking()
                on eintrag.UserProfileId equals profil.Id into profile
            from profil in profile.DefaultIfEmpty()
            orderby eintrag.ErstelltAm descending
            select new VerlaufEintragUebersicht(
                eintrag.Id,
                eintrag.Dateiname,
                eintrag.Auftragsnummer,
                eintrag.Maschinentyp,
                eintrag.ErstelltAm,
                profil == null
                    ? null
                    : profil.DisplayName != ""
                        ? profil.DisplayName
                        : profil.FullName != ""
                            ? profil.FullName
                            : profil.Email))
            .ToListAsync(cancellationToken);
    }

    public async Task<VerlaufDetail?> DetailAsync(
        Guid userProfileId, int id, CancellationToken cancellationToken = default)
    {
        var eintrag = await db.StuecklistenpruefungVerlaufEintraege
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.UserProfileId == userProfileId, cancellationToken);
        return eintrag is null ? null : await BaueDetailAsync(eintrag, cancellationToken);
    }

    public async Task<VerlaufDetail?> DetailAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var eintrag = await db.StuecklistenpruefungVerlaufEintraege
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return eintrag is null ? null : await BaueDetailAsync(eintrag, cancellationToken);
    }

    /// <summary>
    /// Sucht nach Auftragsnummer über beide Workflows hinweg (SharePoint-Sync und
    /// Drag&amp;Drop-Upload teilen sich seit der Konsolidierung dieselbe Tabelle). Bei mehreren
    /// Treffern wird der SharePoint-Eintrag bevorzugt (maßgebliche Quelle), sonst der am weitesten
    /// fortgeschrittene bzw. neueste Drag&amp;Drop-Eintrag — dieselbe Priorisierung wie in
    /// <see cref="SpeichernOderOeffnenAsync"/>.
    /// </summary>
    public async Task<VerlaufDetail?> SucheNachAuftragsnummerAsync(
        string auftragsnummer, CancellationToken cancellationToken = default)
    {
        var gesucht = auftragsnummer.Trim();
        var treffer = await db.StuecklistenpruefungVerlaufEintraege
            .AsNoTracking()
            .Where(e => e.Auftragsnummer.ToUpper() == gesucht.ToUpper())
            .ToListAsync(cancellationToken);

        var bester = treffer
            .Where(e => e.Quelle == AuftragsdokumentQuelle.SharePoint)
            .OrderByDescending(e => e.SharePointGeaendertAm)
            .FirstOrDefault()
            ?? treffer
                .OrderByDescending(e => e.ErreichterSchritt)
                .ThenByDescending(e => e.ErstelltAm)
                .FirstOrDefault();

        return bester is null ? null : await BaueDetailAsync(bester, cancellationToken);
    }

    private async Task<VerlaufDetail> BaueDetailAsync(
        StuecklistenpruefungVerlaufEintrag eintrag, CancellationToken cancellationToken)
    {
        var merkmale = await db.VerlaufMerkmale
            .AsNoTracking()
            .Where(m => m.VerlaufEintragId == eintrag.Id)
            .ToListAsync(cancellationToken);

        return new VerlaufDetail(
            eintrag.Id,
            eintrag.Dateiname,
            eintrag.Quelle,
            eintrag.Auftragsnummer,
            eintrag.Kundennummer,
            eintrag.Datum,
            eintrag.Maschinentyp,
            merkmale.Where(m => m.Kategorie == MerkmalKategorie.Merkmal)
                .Select(m => new ErkanntesMerkmal(m.Position, m.Merkmalsnummer, m.Beschreibung)).ToList(),
            merkmale.Where(m => m.Kategorie == MerkmalKategorie.Sonderoption)
                .Select(m => new ErkanntesMerkmal(m.Position, m.Merkmalsnummer, m.Beschreibung)).ToList(),
            eintrag.ErreichterSchritt,
            string.IsNullOrWhiteSpace(eintrag.StuecklisteJson)
                ? null
                : JsonSerializer.Deserialize<StuecklistenKnoten>(eintrag.StuecklisteJson),
            string.IsNullOrWhiteSpace(eintrag.VergleichsErgebnisJson)
                ? null
                : JsonSerializer.Deserialize<VergleichsErgebnis>(eintrag.VergleichsErgebnisJson),
            eintrag.SapDateiname,
            WebUrl: eintrag.WebUrl);
    }

    public async Task<(string Dateiname, Stream Inhalt)?> DokumentAsync(
        Guid userProfileId, int id, CancellationToken cancellationToken = default)
    {
        var eintrag = await db.StuecklistenpruefungVerlaufEintraege
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.UserProfileId == userProfileId, cancellationToken);
        if (eintrag is null)
            return null;

        if (eintrag.Quelle == AuftragsdokumentQuelle.SharePoint)
        {
            if (sharePointClient is null)
                throw new InvalidOperationException("SharePointClient ist für Referenzdokumente nicht konfiguriert.");

            try
            {
                var spStream = await sharePointClient.OeffnenAsync(eintrag.WebUrl!, cancellationToken);
                return (eintrag.Dateiname, spStream);
            }
            catch (Exception)
            {
                return null;
            }
        }

        var stream = await blobStorage.OpenReadAsync(eintrag.BlobPfad, cancellationToken);
        return (eintrag.Dateiname, stream);
    }

    public async Task<(string Dateiname, Stream Inhalt)?> DokumentAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var eintrag = await db.StuecklistenpruefungVerlaufEintraege
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (eintrag is null)
            return null;

        if (eintrag.Quelle == AuftragsdokumentQuelle.SharePoint)
        {
            if (sharePointClient is null)
                throw new InvalidOperationException("SharePointClient ist für Referenzdokumente nicht konfiguriert.");

            try
            {
                var spStream = await sharePointClient.OeffnenAsync(eintrag.WebUrl!, cancellationToken);
                return (eintrag.Dateiname, spStream);
            }
            catch (Exception)
            {
                return null;
            }
        }

        var stream = await blobStorage.OpenReadAsync(eintrag.BlobPfad, cancellationToken);
        return (eintrag.Dateiname, stream);
    }
}
