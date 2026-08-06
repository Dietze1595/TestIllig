using System.Text;
using System.Text.RegularExpressions;
using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Illig_AI_Platform.Shared.Kunden;

public sealed class KundenstammService(AppDbContext db)
{
    private static readonly string[] Rechtsformen =
        ["GMBH", "AG", "KG", "OHG", "INC", "LTD", "LIMITED", "LLC", "BV", "NV", "SAS", "SRL"];

    public async Task<Kunde?> FindeOderErstelleAsync(
        string? kundenname,
        string? kundenadresse,
        string? kundennummer,
        CancellationToken cancellationToken = default)
    {
        kundenname = Bereinige(kundenname);
        kundenadresse = Bereinige(kundenadresse);
        kundennummer = Bereinige(kundennummer);
        if (kundenname is null && kundenadresse is null && kundennummer is null)
            return null;

        Kunde? kunde = null;
        if (kundennummer is not null)
            kunde = await db.Kunden.FirstOrDefaultAsync(k => k.Kundennummer == kundennummer, cancellationToken);

        var normalisierterName = NormalisiereName(kundenname);
        var normalisierteAdresse = NormalisiereFuerIndex(kundenadresse);
        if (kunde is null && normalisierterName.Length > 0)
        {
            kunde = await db.Kunden.FirstOrDefaultAsync(
                k => k.NormalisierterName == normalisierterName
                     && normalisierteAdresse.Length > 0
                     && k.NormalisierteAdresse == normalisierteAdresse,
                cancellationToken);

            if (kunde is null && Postleitzahl(kundenadresse) is { } plz)
            {
                var kandidaten = await db.Kunden
                    .Where(k => k.NormalisierterName == normalisierterName)
                    .ToListAsync(cancellationToken);
                kunde = kandidaten.FirstOrDefault(k => Postleitzahl(k.Adresse) == plz);
            }
        }

        if (kunde is null)
        {
            kunde = new Kunde
            {
                Kundennummer = kundennummer,
                Name = kundenname,
                Adresse = kundenadresse,
                NormalisierterName = normalisierterName,
                NormalisierteAdresse = normalisierteAdresse,
                Status = kundennummer is not null && kundenname is not null
                    ? KundeStatus.Bestaetigt
                    : KundeStatus.Vorlaeufig,
                ErstelltAm = DateTime.UtcNow,
                AktualisiertAm = DateTime.UtcNow,
            };
            db.Kunden.Add(kunde);
        }
        else
        {
            kunde.Kundennummer ??= kundennummer;
            kunde.Name ??= kundenname;
            kunde.Adresse ??= kundenadresse;
            kunde.NormalisierterName = NormalisiereName(kunde.Name);
            kunde.NormalisierteAdresse = NormalisiereFuerIndex(kunde.Adresse);
            if (kunde.Kundennummer is not null && kunde.Name is not null)
                kunde.Status = KundeStatus.Bestaetigt;
            kunde.AktualisiertAm = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return kunde;
    }

    public async Task RegistriereQuelleAsync(
        Kunde kunde,
        KundenQuelltyp quelltyp,
        int quellId,
        string? kundenname,
        string? kundenadresse,
        string? kundennummer,
        CancellationToken cancellationToken = default)
    {
        var quelle = await db.KundenQuellen
            .FirstOrDefaultAsync(q => q.Quelltyp == quelltyp && q.QuellId == quellId, cancellationToken);
        if (quelle is null)
        {
            db.KundenQuellen.Add(new KundenQuelle
            {
                KundeId = kunde.Id,
                Quelltyp = quelltyp,
                QuellId = quellId,
                Kundenname = Bereinige(kundenname),
                Kundenadresse = Bereinige(kundenadresse),
                Kundennummer = Bereinige(kundennummer),
                ErfasstAm = DateTime.UtcNow,
            });
        }
        else
        {
            quelle.KundeId = kunde.Id;
            quelle.Kundenname = Bereinige(kundenname);
            quelle.Kundenadresse = Bereinige(kundenadresse);
            quelle.Kundennummer = Bereinige(kundennummer);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SynchronisierenAsync(CancellationToken cancellationToken = default)
    {
        var angebote = await db.Angebote.OrderBy(a => a.HochgeladenAm).ToListAsync(cancellationToken);
        foreach (var angebot in angebote)
        {
            var kunde = angebot.KundeId is int id
                ? await db.Kunden.FindAsync([id], cancellationToken)
                : await FindeOderErstelleAsync(angebot.Kundenname, angebot.Kundenadresse, null, cancellationToken);
            if (kunde is null)
                continue;
            angebot.KundeId = kunde.Id;
            await db.SaveChangesAsync(cancellationToken);
            await RegistriereQuelleAsync(
                kunde, KundenQuelltyp.Angebot, angebot.Id,
                angebot.Kundenname, angebot.Kundenadresse, null, cancellationToken);
        }

        var bestaetigungen = await db.Auftragsbestaetigungen
            .OrderBy(b => b.HochgeladenAm)
            .ToListAsync(cancellationToken);
        foreach (var bestaetigung in bestaetigungen)
        {
            var angebotKundeId = angebote.FirstOrDefault(a => a.Id == bestaetigung.AngebotId)?.KundeId;
            var kunde = bestaetigung.KundeId is int id
                ? await db.Kunden.FindAsync([id], cancellationToken)
                : angebotKundeId is int angebotId
                    ? await db.Kunden.FindAsync([angebotId], cancellationToken)
                    : await FindeOderErstelleAsync(
                        bestaetigung.Kundenname, bestaetigung.Kundenadresse, null, cancellationToken);
            if (kunde is null)
                continue;
            bestaetigung.KundeId = kunde.Id;
            await db.SaveChangesAsync(cancellationToken);
            await RegistriereQuelleAsync(
                kunde, KundenQuelltyp.Kundenbestellung, bestaetigung.Id,
                bestaetigung.Kundenname, bestaetigung.Kundenadresse, null, cancellationToken);
        }

        var auftragsinformationen = await db.StuecklistenpruefungVerlaufEintraege
            .OrderBy(e => e.ErstelltAm)
            .ToListAsync(cancellationToken);
        foreach (var eintrag in auftragsinformationen)
        {
            var kunde = eintrag.KundeId is int id
                ? await db.Kunden.FindAsync([id], cancellationToken)
                : await FindeOderErstelleAsync(
                    eintrag.Kundenname, eintrag.Kundenadresse, eintrag.Kundennummer, cancellationToken);
            if (kunde is null)
                continue;
            eintrag.KundeId = kunde.Id;
            await db.SaveChangesAsync(cancellationToken);
            await RegistriereQuelleAsync(
                kunde, KundenQuelltyp.Auftragsinformation, eintrag.Id,
                eintrag.Kundenname, eintrag.Kundenadresse, eintrag.Kundennummer, cancellationToken);
        }

        await AktualisiereKundenstatusAsync(angebote, bestaetigungen, cancellationToken);
    }

    public async Task<IReadOnlyList<KundenUebersicht>> ListeAsync(
        string? suche = null,
        CancellationToken cancellationToken = default)
    {
        await SynchronisierenAsync(cancellationToken);
        var kunden = await db.Kunden.AsNoTracking().ToListAsync(cancellationToken);
        var angebote = await db.Angebote.AsNoTracking().Where(a => a.KundeId != null).ToListAsync(cancellationToken);
        var bestaetigungen = await db.Auftragsbestaetigungen.AsNoTracking()
            .Where(b => b.KundeId != null).ToListAsync(cancellationToken);
        var auftragsinformationen = await db.StuecklistenpruefungVerlaufEintraege.AsNoTracking()
            .Where(e => e.KundeId != null).ToListAsync(cancellationToken);

        var suchwert = Normalisiere(suche);
        return kunden
            .Where(k => suchwert.Length == 0
                        || Normalisiere(k.Name).Contains(suchwert)
                        || Normalisiere(k.Kundennummer).Contains(suchwert)
                        || Normalisiere(k.Adresse).Contains(suchwert))
            .Select(k =>
            {
                var kundenAngebote = angebote.Where(a => a.KundeId == k.Id).ToList();
                var kundenBestaetigungen = bestaetigungen.Where(b => b.KundeId == k.Id).ToList();
                var kundenAuftraege = auftragsinformationen.Where(e => e.KundeId == k.Id).ToList();
                var aktivitaeten = kundenAngebote.Select(a => a.HochgeladenAm)
                    .Concat(kundenBestaetigungen.Select(b => b.HochgeladenAm))
                    .Concat(kundenAuftraege.Select(e => e.ErstelltAm))
                    .DefaultIfEmpty(k.AktualisiertAm);
                return new KundenUebersicht(
                    k.Id,
                    Anzeigename(k),
                    k.Kundennummer,
                    k.Adresse,
                    k.Status,
                    kundenAngebote.Select(a => a.Angebotsnummer).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    kundenBestaetigungen.Select(b => b.Nummer).Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    kundenAuftraege.Select(e => ZerlegeAuftragsnummer(e.Auftragsnummer).Basis)
                        .Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    kundenAuftraege.Select(e => NormalisiereAuftragsposition(e.Auftragsnummer))
                        .Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    aktivitaeten.Max());
            })
            .OrderByDescending(k => k.LetzteAktivitaet)
            .ThenBy(k => k.Anzeigename)
            .ToList();
    }

    public async Task<KundenDetail?> DetailAsync(int id, CancellationToken cancellationToken = default)
    {
        await SynchronisierenAsync(cancellationToken);
        var kunde = await db.Kunden.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (kunde is null)
            return null;

        var angebote = await db.Angebote.AsNoTracking().Where(a => a.KundeId == id).ToListAsync(cancellationToken);
        var bestaetigungen = await db.Auftragsbestaetigungen.AsNoTracking()
            .Where(b => b.KundeId == id).ToListAsync(cancellationToken);
        var eintraege = await db.StuecklistenpruefungVerlaufEintraege.AsNoTracking()
            .Where(e => e.KundeId == id).ToListAsync(cancellationToken);
        var quellen = await db.KundenQuellen.AsNoTracking()
            .Where(q => q.KundeId == id).OrderByDescending(q => q.ErfasstAm).ToListAsync(cancellationToken);

        var auftraege = eintraege
            .GroupBy(e => ZerlegeAuftragsnummer(e.Auftragsnummer).Basis, StringComparer.OrdinalIgnoreCase)
            .Select(g => new KundenAuftrag(
                g.Key,
                g.Where(e => e.Datum is not null).Select(e => e.Datum).Max(),
                g.GroupBy(e => NormalisiereAuftragsposition(e.Auftragsnummer), StringComparer.OrdinalIgnoreCase)
                    .Select(pg =>
                    {
                        var neuester = pg.OrderByDescending(e => e.ErstelltAm).First();
                        var zerlegt = ZerlegeAuftragsnummer(neuester.Auftragsnummer);
                        return new KundenAuftragPosition(
                            zerlegt.Position ?? "—",
                            neuester.Auftragsnummer,
                            neuester.Maschinentyp,
                            neuester.Datum);
                    })
                    .OrderBy(p => p.Positionsnummer)
                    .ToList()))
            .OrderByDescending(a => a.Datum)
            .ToList();

        var dokumente = angebote
            .Select(a => new KundenDokument(
                KundenQuelltyp.Angebot,
                a.Id,
                $"Angebot {a.Angebotsnummer} · Version {a.Version}",
                a.Dateiname,
                a.HochgeladenAm))
            .Concat(bestaetigungen.Select(b => new KundenDokument(
                KundenQuelltyp.Kundenbestellung,
                b.Id,
                string.IsNullOrWhiteSpace(b.Nummer)
                    ? "Kundenbestellung"
                    : $"Kundenbestellung {b.Nummer}",
                b.Dateiname,
                b.HochgeladenAm)))
            // Denselben Auftrag prüfen ggf. mehrere Nutzer (je eigene Verlaufszeile, siehe
            // SpeichernOderOeffnenAsync); als Dokument zählt die Auftragsinformation nur einmal.
            .Concat(eintraege
                .GroupBy(e => e.Auftragsnummer, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(e => e.ErstelltAm).First())
                .Select(e => new KundenDokument(
                    KundenQuelltyp.Auftragsinformation,
                    e.Id,
                    $"Auftragsinformation {e.Auftragsnummer}",
                    e.Dateiname,
                    e.ErstelltAm)))
            .OrderByDescending(d => d.ErfasstAm)
            .ToList();

        return new KundenDetail(
            kunde.Id,
            Anzeigename(kunde),
            kunde.Kundennummer,
            kunde.Adresse,
            kunde.Status,
            angebote.Select(a => a.Angebotsnummer).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(),
            bestaetigungen.Select(b => b.Nummer).Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(),
            auftraege,
            quellen,
            dokumente);
    }

    public static (string Basis, string? Position) ZerlegeAuftragsnummer(string auftragsnummer)
    {
        var teile = auftragsnummer.Split('/', 2, StringSplitOptions.TrimEntries);
        return (teile[0].Trim(), teile.Length > 1 && teile[1].Length > 0 ? teile[1].Trim() : null);
    }

    private static string NormalisiereAuftragsposition(string auftragsnummer)
    {
        var (basis, position) = ZerlegeAuftragsnummer(auftragsnummer);
        return position is null ? basis : $"{basis}/{position}";
    }

    private async Task AktualisiereKundenstatusAsync(
        IReadOnlyCollection<Angebot> angebote,
        IReadOnlyCollection<Auftragsbestaetigung> bestaetigungen,
        CancellationToken cancellationToken)
    {
        var freigegebeneAngebote = angebote
            .Where(a => a.Freigegeben && a.KundeId is not null)
            .ToDictionary(a => a.Id);
        var neukundenIds = bestaetigungen
            .Where(b => b.KundeId is int
                        && freigegebeneAngebote.TryGetValue(b.AngebotId, out var angebot)
                        && angebot.KundeId == b.KundeId)
            .Select(b => b.KundeId!.Value)
            .ToHashSet();

        var kunden = await db.Kunden.ToListAsync(cancellationToken);
        var geaendert = false;
        foreach (var kunde in kunden)
        {
            var status = !string.IsNullOrWhiteSpace(kunde.Kundennummer)
                         && !string.IsNullOrWhiteSpace(kunde.Name)
                ? KundeStatus.Bestaetigt
                : neukundenIds.Contains(kunde.Id)
                    ? KundeStatus.Neukunde
                    : KundeStatus.Vorlaeufig;

            if (kunde.Status == status)
                continue;

            kunde.Status = status;
            kunde.AktualisiertAm = DateTime.UtcNow;
            geaendert = true;
        }

        if (geaendert)
            await db.SaveChangesAsync(cancellationToken);
    }

    private static string Anzeigename(Kunde kunde) =>
        !string.IsNullOrWhiteSpace(kunde.Name)
            ? kunde.Name
            : !string.IsNullOrWhiteSpace(kunde.Kundennummer)
                ? $"Kunde {kunde.Kundennummer}"
                : $"Kunde #{kunde.Id}";

    private static string? Bereinige(string? wert) =>
        string.IsNullOrWhiteSpace(wert) ? null : Regex.Replace(wert.Trim(), @"\s+", " ");

    private static string NormalisiereName(string? wert)
    {
        var normalisiert = Normalisiere(wert);
        foreach (var rechtsform in Rechtsformen)
            normalisiert = Regex.Replace(normalisiert, $@"\b{rechtsform}\b", " ");
        return Begrenze(Regex.Replace(normalisiert, @"\s+", " ").Trim(), 255);
    }

    private static string Normalisiere(string? wert)
    {
        if (string.IsNullOrWhiteSpace(wert))
            return "";
        var formD = wert.Trim().Replace("ß", "SS", StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var ohneAkzente = new string(formD.Where(c =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
            != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(ohneAkzente, @"[^A-Z0-9]+", " ").Trim();
    }

    private static string NormalisiereFuerIndex(string? wert) => Begrenze(Normalisiere(wert), 255);

    private static string Begrenze(string wert, int laenge) =>
        wert.Length <= laenge ? wert : wert[..laenge];

    private static string? Postleitzahl(string? adresse)
    {
        var match = Regex.Match(adresse ?? "", @"\b\d{5}\b");
        return match.Success ? match.Value : null;
    }
}
