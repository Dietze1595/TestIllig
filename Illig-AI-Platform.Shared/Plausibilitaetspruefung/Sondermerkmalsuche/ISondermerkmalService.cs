namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;

public interface ISondermerkmalService
{
    Task<StuecklisteAnalyse?> AnalyzeAsync(string auftragsnummer);

    Task<IReadOnlyList<ReferenzTreffer>> SearchAsync(SearchRequest request);

    Task<StuecklisteDetail?> GetDetailAsync(string auftragsnummer);

    Task<ReferenzDokument?> GetDokumentAsync(
        string auftragsnummer,
        CancellationToken cancellationToken = default);
}
