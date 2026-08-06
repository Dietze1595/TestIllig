namespace Illig_AI_Platform.Shared.Auftragsanlage;

public interface IAngebotsdokumentAnalyseService
{
    Task<ExtrahierteAngebotsdaten> AnalyzeAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}
