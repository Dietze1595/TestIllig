using ClosedXML.Excel;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Eigener Parser für das RDM73K-Format der Umsetzungsmatrix. Strukturell dem RDM75-Format sehr
/// ähnlich (gleiches "Umsetztabelle"-Blatt, B-basierte Hierarchie, Varianten ab Spalte N,
/// "Merkmalskombination"-Spalte, "siehe Komb."-Verweise), bewusst als separate Klasse gehalten,
/// damit RDM73K-spezifische Anpassungen die anderen Formate nicht berühren. Die Fixtures bilden
/// die real beobachteten Zeilen der Datei UMSE_RDM73K_V5_9254662 nach: Wurzel 9254662 in Spalte B
/// ab Zeile 8, mehrzeiliger Kopf (Merkmalskombination in Zeile 1+2).
/// </summary>
public class UmsetzungsmatrixRdm73kXlsxParserTests
{
    private static Stream MiniMatrix()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 73K");

        // Wurzel: Artikelnummer in Spalte B (Tiefe 0), Daten beginnen ab Zeile 8 wie im echten Export.
        ws.Cell(8, 2).Value = "9254662";
        ws.Cell(8, 3).Value = "RDM_73K_konf";

        // Tiefe 1: Artikelnummer in Spalte C, Bezeichnung in D.
        ws.Cell(10, 3).Value = "9237787";
        ws.Cell(10, 4).Value = "Folieneinlauf_FB_200-900_konf";

        // Tiefe 2 darunter: Artikelnummer in D, Bezeichnung in E, Bedingung in einer Varianten-Spalte (O = 15).
        ws.Cell(11, 4).Value = "9237831";
        ws.Cell(11, 5).Value = "Lichtleiter_BGR";
        ws.Cell(11, 15).Value = "020113 / 020114";

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void Parse_LiefertPfadUndBedingungFuerZeileMitBedingung()
    {
        using var stream = MiniMatrix();

        var zeile = Assert.Single(UmsetzungsmatrixRdm73kXlsxParser.Parse(stream));

        Assert.Equal(["9254662", "9237787", "9237831"], zeile.Pfad);
        Assert.Equal("020113 / 020114", zeile.Bedingung);
    }

    [Fact]
    public void Parse_LiefertNichtsFuerZeilenOhneBedingung()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 73K");
        ws.Cell(8, 2).Value = "9254662";
        ws.Cell(10, 3).Value = "9237787";
        ws.Cell(10, 4).Value = "Folieneinlauf_FB_200-900_konf"; // keine Bedingung in einer Varianten-Spalte

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        Assert.Empty(UmsetzungsmatrixRdm73kXlsxParser.Parse(stream));
    }

    [Fact]
    public void Parse_LoestSieheKombUeberMerkmalskombinationsSpalteAuf()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 73K");
        // Kopf der Merkmalskombinationsspalte steht im echten RDM73K-Export in Zeile 1 UND 2 (verbundene Zelle).
        ws.Cell(1, 16).Value = "Merkmalskombination";
        ws.Cell(2, 16).Value = "Merkmalskombination";

        ws.Cell(8, 2).Value = "9254662";
        ws.Cell(11, 4).Value = "9237831";
        ws.Cell(11, 14).Value = "siehe Komb.";
        ws.Cell(11, 15).Value = "siehe Komb.";
        ws.Cell(11, 16).Value = "020113 oder 020114 oder 017361";

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        var zeile = Assert.Single(UmsetzungsmatrixRdm73kXlsxParser.Parse(stream));
        Assert.Equal("020113 oder 020114 oder 017361", zeile.Bedingung);
    }
}
