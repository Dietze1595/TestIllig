using Xunit;
using System.Runtime.CompilerServices;

namespace Illig_AI_Platform.Tests.UI;

public class AuftragsanlageKommentarDesignTests
{
    private static string ClientFile(params string[] parts)
    {
        var all = new[] { TestSourceDirectory(), "..", "..", "Illig-AI-Platform.Client" }
            .Concat(parts).ToArray();
        return Path.GetFullPath(Path.Combine(all));
    }

    private static string TestSourceDirectory([CallerFilePath] string sourceFile = "") =>
        Path.GetDirectoryName(sourceFile)!;

    [Fact]
    public void Innendienst_BindsSavedSalesCommentsAsValues()
    {
        var page = File.ReadAllText(
            ClientFile("Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor"));

        Assert.Contains("KommentarAnzeige=\"@angebotStatus.KundeKommentar\"", page);
        Assert.Contains("KommentarAnzeige=\"@angebotStatus.ZahlungsbedingungenKommentar\"", page);
        Assert.Contains("KommentarAnzeige=\"@angebotStatus.IncotermKommentar\"", page);
        Assert.Contains("KommentarAnzeige=\"@angebotStatus.VersandbedingungKommentar\"", page);
    }

    [Fact]
    public void VertriebHistory_BindsSavedSalesCommentsAsValues()
    {
        var page = File.ReadAllText(
            ClientFile("Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));

        Assert.Contains("KommentarAnzeige=\"@_detail.KundeKommentar\"", page);
        Assert.Contains("KommentarAnzeige=\"@_detail.ZahlungsbedingungenKommentar\"", page);
        Assert.Contains("KommentarAnzeige=\"@_detail.IncotermKommentar\"", page);
        Assert.Contains("KommentarAnzeige=\"@_detail.VersandbedingungKommentar\"", page);
    }

    [Fact]
    public void Vertrieb_BindsUploadedOrStoredPdfToViewer()
    {
        var page = File.ReadAllText(
            ClientFile("Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));

        Assert.Contains("DokumentName=\"@_pdfDokumentName\" DataUrl=\"@_pdfDataUrl\"", page);
    }

    [Fact]
    public void FileDropzone_ShowsScanIconForPdfUploads()
    {
        var component = File.ReadAllText(ClientFile("Components", "FileDropzone.razor"));

        Assert.Contains("Accept.Contains(\"pdf\"", component);
        Assert.Contains("class=\"su-dropzone__scan-icon\"", component);
    }

    [Fact]
    public void Vertrieb_KeepsDropzoneVisibleWhileAnalysisIsRunning()
    {
        var page = File.ReadAllText(
            ClientFile("Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));

        Assert.Contains("else if (_laedt || (_analyse is null && _gespeichert is null))", page);
        Assert.Contains("<FileDropzone OnChange=\"OnFileSelectedAsync\" Accept=\"application/pdf\" Busy=\"_laedt\"", page);
    }

    [Fact]
    public void Checklist_GivesExtractedValuesMoreSpaceThanComments()
    {
        var styles = File.ReadAllText(ClientFile("Components", "ChecklistItem.razor.css"));

        Assert.Contains("grid-template-columns: 1.5rem minmax(0, 1fr) minmax(180px, 32%);", styles);
        Assert.Contains("border-left: 3px solid transparent", styles);
    }
}
