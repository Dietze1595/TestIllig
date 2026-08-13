namespace Illig_AI_Platform.Services.Auftragsinformationen;

public record SharePointQuelle(string DriveId, string OrdnerItemId);

public record SharePointAenderung(
    string DriveId,
    string ItemId,
    string Dateiname,
    string ETag,
    string WebUrl,
    DateTime ErstelltAm,
    DateTime GeaendertAm,
    bool IstDatei,
    bool IstGeloescht);

public record SharePointAenderungsseite(
    IReadOnlyList<SharePointAenderung> Eintraege,
    string? NaechsteSeite,
    string? DeltaLink);

public interface ISharePointDokumentClient
{
    Task<SharePointQuelle> QuelleAufloesenAsync(CancellationToken cancellationToken = default);

    Task<SharePointAenderungsseite> AenderungsseiteAsync(
        SharePointQuelle quelle,
        string? fortsetzungsUrl,
        CancellationToken cancellationToken = default);

    Task<Stream> OeffnenAsync(
        string webUrl,
        CancellationToken cancellationToken = default);
}
