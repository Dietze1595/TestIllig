using ClosedXML.Excel;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class UmsetzungsmatrixXlsxParserTests
{
    private static Stream MiniMatrix()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle_TEST");

        // Wurzel: Artikelnummer in Spalte B (Tiefe 0)
        ws.Cell(5, 2).Value = "9209307";
        ws.Cell(5, 3).Value = "RDM 75Kc";

        // Tiefe 1: Artikelnummer in Spalte C, Bezeichnung in D
        ws.Cell(7, 3).Value = "9237787";
        ws.Cell(7, 4).Value = "Folieneinlauf_FB_200-900_konf";

        // Tiefe 2 unter obiger Position: Artikelnummer in D, Bezeichnung in E, Bedingung in Spalte N
        ws.Cell(8, 4).Value = "9237831";
        ws.Cell(8, 5).Value = "Lichtleiter_BGR";
        ws.Cell(8, 14).Value = "020113 / 020114";

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void Parse_LiefertPfadUndBedingungFuerJedeZeileMitBedingung()
    {
        using var stream = MiniMatrix();
        var zeilen = UmsetzungsmatrixXlsxParser.Parse(stream);

        var zeile = Assert.Single(zeilen);
        Assert.Equal(["9209307", "9237787", "9237831"], zeile.Pfad);
        Assert.Equal("020113 / 020114", zeile.Bedingung);
    }

    [Fact]
    public void Parse_LiefertNichtsFuerZeilenOhneBedingung()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle_TEST");
        ws.Cell(5, 2).Value = "9209307";
        ws.Cell(5, 3).Value = "RDM 75Kc";
        ws.Cell(7, 3).Value = "9237787";
        ws.Cell(7, 4).Value = "Folieneinlauf_FB_200-900_konf"; // keine Bedingung in Spalte N+

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        var zeilen = UmsetzungsmatrixXlsxParser.Parse(stream);
        Assert.Empty(zeilen);
    }

    [Fact]
    public void Parse_LoestSieheKombUeberMerkmalskombinationsSpalteAuf()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle_TEST");
        ws.Cell(5, 2).Value = "9209307";
        ws.Cell(7, 3).Value = "9290576";
        ws.Cell(7, 4).Value = "Druckregelventil";
        ws.Cell(2, 14).Value = "Kopf A";
        ws.Cell(2, 15).Value = "Kopf B";
        ws.Cell(2, 16).Value = "Merkmalskombination";
        ws.Cell(7, 14).Value = "siehe Komb.";
        ws.Cell(7, 15).Value = "siehe Komb.";
        ws.Cell(7, 16).Value = "017367 oder 011847 oder 017369";

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        var zeile = Assert.Single(UmsetzungsmatrixXlsxParser.Parse(stream));
        Assert.Equal("017367 oder 011847 oder 017369", zeile.Bedingung);
    }
}
