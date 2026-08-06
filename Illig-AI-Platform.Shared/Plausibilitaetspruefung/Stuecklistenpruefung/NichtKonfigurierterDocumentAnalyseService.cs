namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public class NichtKonfigurierterDocumentAnalyseService : IDocumentAnalyseService
{
    public Task<DokumentAnalyseErgebnis> AnalyzeAsync(Stream pdfStream, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Document Intelligence ist nicht konfiguriert (AzureDocumentIntelligence:Endpoint/ApiKey fehlt).");
}
