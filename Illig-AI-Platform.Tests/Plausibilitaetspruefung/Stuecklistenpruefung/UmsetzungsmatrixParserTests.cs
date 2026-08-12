using ClosedXML.Excel;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Die Format-Weiche wählt anhand des beim Import gewählten <see cref="StuecklistenImportFormat"/>
/// den passenden Parser. Da sich die Formate äußerlich kaum unterscheiden, wird das Format explizit
/// übergeben statt automatisch erkannt.
/// </summary>
public class UmsetzungsmatrixParserTests
{
    private static Stream Rdm75Matrix()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle_TEST");
        ws.Cell(5, 2).Value = "9209307"; // Wurzel ab Zeile 5 (RDM75-Format)
        ws.Cell(7, 3).Value = "9237787";
        ws.Cell(7, 4).Value = "Folieneinlauf";
        ws.Cell(7, 14).Value = "020113 / 020114";
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static Stream Rdm73kMatrix()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 73K");
        ws.Cell(8, 2).Value = "9254662"; // Wurzel ab Zeile 8 (RDM73K-Format)
        ws.Cell(10, 3).Value = "9237787";
        ws.Cell(11, 4).Value = "9237831";
        ws.Cell(11, 15).Value = "021127 / 021128";
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static Stream Rdk80Matrix()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetzmatrix_V05_V06");
        ws.Cell(8, 12).Value = "Nachfolgende Stücklisten gelten nur für die RDK 80";
        ws.Cell(9, 12).Value = "9209711";
        ws.Cell(9, 23).Value = "014590";
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void Parse_MitRdm75Format_LiefertGeparsteZeile()
    {
        using var stream = Rdm75Matrix();

        var zeile = Assert.Single(UmsetzungsmatrixParser.Parse(stream, StuecklistenImportFormat.Rdm75Kc));

        Assert.Equal(["9209307", "9237787"], zeile.Pfad);
        Assert.Equal("020113 / 020114", zeile.Bedingung);
    }

    [Fact]
    public void Parse_MitRdm73kFormat_LiefertGeparsteZeile()
    {
        using var stream = Rdm73kMatrix();

        var zeile = Assert.Single(UmsetzungsmatrixParser.Parse(stream, StuecklistenImportFormat.Rdm73k));

        Assert.Equal(["9254662", "9237787", "9237831"], zeile.Pfad);
        Assert.Equal("021127 / 021128", zeile.Bedingung);
    }

    [Fact]
    public void Parse_MitRdk80Format_VerwendetEigenenParser()
    {
        using var stream = Rdk80Matrix();

        var zeile = Assert.Single(UmsetzungsmatrixParser.Parse(stream, StuecklistenImportFormat.Rdk80k));

        Assert.Equal(["9209425", "9209711"], zeile.Pfad);
        Assert.Equal("014590", zeile.Bedingung);
    }

    [Fact]
    public void Parse_MitUnbekanntemFormat_WirftArgumentException()
    {
        using var stream = Rdm73kMatrix();

        Assert.ThrowsAny<ArgumentException>(
            () => UmsetzungsmatrixParser.Parse(stream, (StuecklistenImportFormat)999));
    }
}
