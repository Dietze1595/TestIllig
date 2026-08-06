using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class NichtKonfigurierterDocumentAnalyseServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_WirftAussagekraeftigeException()
    {
        var service = new NichtKonfigurierterDocumentAnalyseService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync(Stream.Null));

        Assert.Contains("DocumentIntelligence", ex.Message);
    }
}
