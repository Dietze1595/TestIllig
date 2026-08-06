namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public interface IDocumentAnalyseService
{
    Task<DokumentAnalyseErgebnis> AnalyzeAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}
