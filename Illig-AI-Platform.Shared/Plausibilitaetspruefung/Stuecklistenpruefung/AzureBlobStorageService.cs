using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public class AzureBlobStorageService(BlobContainerClient containerClient) : IBlobStorageService
{
    public async Task<string> UploadAsync(Stream inhalt, string dateiname, CancellationToken cancellationToken = default)
    {
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobPfad = Guid.NewGuid().ToString();
        var blobClient = containerClient.GetBlobClient(blobPfad);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" }
        };
        await blobClient.UploadAsync(inhalt, uploadOptions, cancellationToken);
        return blobPfad;
    }

    public async Task<Stream> OpenReadAsync(string blobPfad, CancellationToken cancellationToken = default)
    {
        var blobClient = containerClient.GetBlobClient(blobPfad);
        var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return download.Value.Content;
    }

    public async Task DeleteAsync(string blobPfad, CancellationToken cancellationToken = default)
    {
        var blobClient = containerClient.GetBlobClient(blobPfad);
        // Ein ersetztes Dokument darf auch dann nicht als aktiver Blob zurückbleiben,
        // wenn für den Container Snapshots aktiviert sind.
        await blobClient.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken);
    }
}
