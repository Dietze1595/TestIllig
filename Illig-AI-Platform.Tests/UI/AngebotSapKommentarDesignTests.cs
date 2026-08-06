using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class AngebotSapKommentarDesignTests
{
    [Fact]
    public void SapBestaetigungen_UnterstuetztKommentarProPunkt()
    {
        var component = File.ReadAllText(ClientFile(
            "Components", "Auftragsanlage", "SapBestaetigungen.razor"));

        Assert.Contains("SparteKommentar", component);
        Assert.Contains("FuehrendKommentar", component);
        Assert.Contains("SparteKommentarChanged", component);
        Assert.Contains("FuehrendKommentarChanged", component);
    }

    [Fact]
    public void Vertrieb_BindetSapKommentareImEditierbarenBlock()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));

        Assert.Contains("@bind-SparteKommentar=\"_sapSparteKommentar\"", page);
        Assert.Contains("@bind-FuehrendKommentar=\"_sapFuehrendKommentar\"", page);
    }

    [Fact]
    public void Vertrieb_ZeigtGespeicherteSapKommentareInDerHistorie()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));

        Assert.Contains("SparteKommentar=\"@_detail.SapSparteKommentar\"", page);
        Assert.Contains("FuehrendKommentar=\"@_detail.SapFuehrendKommentar\"", page);
    }

    [Fact]
    public void Innendienst_ZeigtGespeicherteSapKommentare()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor"));

        Assert.Contains("SparteKommentar=\"@angebotStatus.SapSparteKommentar\"", page);
        Assert.Contains("FuehrendKommentar=\"@angebotStatus.SapFuehrendKommentar\"", page);
    }

    private static string ClientFile(params string[] parts)
    {
        var all = new[] { TestSourceDirectory(), "..", "..", "Illig-AI-Platform.Client" }
            .Concat(parts)
            .ToArray();
        return Path.GetFullPath(Path.Combine(all));
    }

    private static string TestSourceDirectory([CallerFilePath] string sourceFile = "") =>
        Path.GetDirectoryName(sourceFile)!;
}
