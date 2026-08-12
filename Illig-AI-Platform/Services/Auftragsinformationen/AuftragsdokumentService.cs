using Illig_AI_Platform.Shared.Auftragsinformationen;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;

namespace Illig_AI_Platform.Services.Auftragsinformationen;

public sealed class AuftragsdokumentService(
    AppDbContext db,
    ISharePointDokumentClient sharePoint)
{
    public async Task<IReadOnlyList<AuftragsdokumentUebersicht>> ListeAsync(
        CancellationToken cancellationToken = default) =>
        await db.StuecklistenpruefungVerlaufEintraege.AsNoTracking()
            .Where(d => d.Quelle == AuftragsdokumentQuelle.SharePoint
                && d.GeloeschtAm == null && d.AnalyseStatus == AuftragsdokumentAnalyseStatus.Erfolgreich)
            .OrderByDescending(d => d.Datum)
            .ThenByDescending(d => d.SharePointGeaendertAm)
            .Take(200)
            .Select(d => new AuftragsdokumentUebersicht(
                d.Id, d.Dateiname, d.Auftragsnummer, d.Kundennummer, d.Maschinentyp,
                d.Datum, d.SharePointGeaendertAm!.Value))
            .ToListAsync(cancellationToken);

    public async Task<AuftragsdokumentDetail?> DetailAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var dokument = await db.StuecklistenpruefungVerlaufEintraege.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == id && d.Quelle == AuftragsdokumentQuelle.SharePoint
                && d.GeloeschtAm == null
                && d.AnalyseStatus == AuftragsdokumentAnalyseStatus.Erfolgreich, cancellationToken);
        if (dokument is null)
            return null;

        var merkmale = await db.VerlaufMerkmale.AsNoTracking()
            .Where(m => m.VerlaufEintragId == id)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);

        return new AuftragsdokumentDetail(
            dokument.Id, dokument.Dateiname, dokument.WebUrl ?? "",
            dokument.Auftragsnummer, dokument.Kundennummer, dokument.Kundenname,
            dokument.Kundenadresse, dokument.Datum, dokument.Maschinentyp,
            merkmale.Where(m => m.Kategorie == MerkmalKategorie.Merkmal)
                .Select(m => new ErkanntesMerkmal(m.Position, m.Merkmalsnummer, m.Beschreibung)).ToList(),
            merkmale.Where(m => m.Kategorie == MerkmalKategorie.Sonderoption)
                .Select(m => new ErkanntesMerkmal(m.Position, m.Merkmalsnummer, m.Beschreibung)).ToList());
    }

    public async Task<(string Dateiname, Stream Inhalt)?> DokumentAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var dokument = await db.StuecklistenpruefungVerlaufEintraege.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == id && d.Quelle == AuftragsdokumentQuelle.SharePoint
                && d.GeloeschtAm == null
                && d.AnalyseStatus == AuftragsdokumentAnalyseStatus.Erfolgreich, cancellationToken);
        if (dokument is null)
            return null;

        var inhalt = await sharePoint.OeffnenAsync(
            dokument.SharePointDriveId!, dokument.SharePointItemId!, cancellationToken);
        return (dokument.Dateiname, inhalt);
    }
}
