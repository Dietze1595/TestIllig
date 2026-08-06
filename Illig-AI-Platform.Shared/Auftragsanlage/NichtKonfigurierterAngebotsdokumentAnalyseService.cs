namespace Illig_AI_Platform.Shared.Auftragsanlage;

public class NichtKonfigurierterAngebotsdokumentAnalyseService : IAngebotsdokumentAnalyseService
{
    public Task<ExtrahierteAngebotsdaten> AnalyzeAsync(Stream pdfStream, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Document Intelligence ist nicht konfiguriert (AzureDocumentIntelligence:Endpoint/ApiKey fehlt).");
}
