namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream inhalt, string dateiname, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string blobPfad, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobPfad, CancellationToken cancellationToken = default);
}
