using Xunit;
using System.Runtime.CompilerServices;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class AzureBlobStorageServiceSourceTests
{
    [Fact]
    public void DeleteAsync_LoeschtBlobEinschliesslichVorhandenerSnapshots()
    {
        var servicePath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(TestSourcePath())!,
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Shared",
            "Plausibilitaetspruefung",
            "Stuecklistenpruefung",
            "AzureBlobStorageService.cs"));

        var service = File.ReadAllText(servicePath);

        Assert.Contains("DeleteIfExistsAsync", service);
        Assert.Contains("DeleteSnapshotsOption.IncludeSnapshots", service);
    }

    private static string TestSourcePath([CallerFilePath] string path = "") => path;
}
