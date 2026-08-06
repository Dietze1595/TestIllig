using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class NichtKonfigurierterBlobStorageServiceTests
{
    [Fact]
    public async Task UploadAsync_WirftAussagekraeftigeException()
    {
        var service = new NichtKonfigurierterBlobStorageService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadAsync(Stream.Null, "test.pdf"));

        Assert.Contains("BlobStorage", ex.Message);
    }

    [Fact]
    public async Task OpenReadAsync_WirftAussagekraeftigeException()
    {
        var service = new NichtKonfigurierterBlobStorageService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.OpenReadAsync("irgendein-pfad"));

        Assert.Contains("BlobStorage", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_WirftAussagekraeftigeException()
    {
        var service = new NichtKonfigurierterBlobStorageService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteAsync("irgendein-pfad"));

        Assert.Contains("BlobStorage", ex.Message);
    }
}
