namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Fallback, solange AzureBlobStorage:ConnectionString nicht konfiguriert ist — verhindert,
/// dass die restliche App wegen einer fehlenden Azure-Ressource nicht startet.
/// </summary>
public class NichtKonfigurierterBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAsync(Stream inhalt, string dateiname, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("BlobStorage ist nicht konfiguriert (AzureBlobStorage:ConnectionString fehlt).");

    public Task<Stream> OpenReadAsync(string blobPfad, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("BlobStorage ist nicht konfiguriert (AzureBlobStorage:ConnectionString fehlt).");

    public Task DeleteAsync(string blobPfad, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("BlobStorage ist nicht konfiguriert (AzureBlobStorage:ConnectionString fehlt).");
}
