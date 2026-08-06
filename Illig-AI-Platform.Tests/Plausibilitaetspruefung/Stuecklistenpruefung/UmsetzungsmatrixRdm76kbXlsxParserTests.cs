using ClosedXML.Excel;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Parser für das RDM76Kb-Format der Umsetzungsmatrix. Gegenüber RDM75/RDM73K abweichend:
/// Varianten-Spalten beginnen ab Spalte 13 (M) statt 14, "Merkmalskombination"-Kopf in Zeile 3,
/// Hierarchie nur bis Spalte 9 (danach Z-Zeichnung/Materialnummer/Baugruppenkenner — Spalte 11
/// enthält reine Zahlen, die sonst als Geister-Knoten gelesen würden). Besonderheit:
/// durchgestrichene Alt-/Ersatznummern in den Hierarchiespalten werden ignoriert; gültig ist die
/// nicht durchgestrichene Artikelnummer daneben.
/// </summary>
public class UmsetzungsmatrixRdm76kbXlsxParserTests
{
    private static Stream Speichern(XLWorkbook wb)
    {
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void Parse_LiefertPfadUndBedingung_MitVariantenAbSpalte13()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(7, 2).Value = "9268197";                 // Wurzel (Tiefe 0), Daten ab Zeile 7
        ws.Cell(9, 3).Value = "9237787";                 // Tiefe 1
        ws.Cell(9, 13).Value = "020113 / 020114";        // Bedingung in erster Varianten-Spalte (M)

        var zeile = Assert.Single(UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb)));
        Assert.Equal(["9268197", "9237787"], zeile.Pfad);
        Assert.Equal("020113 / 020114", zeile.Bedingung);
    }

    [Fact]
    public void Parse_IgnoriertDurchgestricheneAltnummer()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(7, 2).Value = "9268197";                 // Wurzel
        ws.Cell(9, 3).Value = "9237787";                 // Tiefe 1

        // Tiefe 2: durchgestrichene Alt-Nummer in Spalte B, gültige Artikelnummer in Spalte D.
        ws.Cell(10, 2).Value = "9238185";
        ws.Cell(10, 2).Style.Font.Strikethrough = true;
        ws.Cell(10, 4).Value = "9290576";
        ws.Cell(10, 13).Value = "017367";

        var zeilen = UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb));

        var zeile = Assert.Single(zeilen);
        // Die durchgestrichene 9238185 (Spalte B/Tiefe 0) darf NICHT als Pfad-Wurzel erscheinen.
        Assert.Equal(["9268197", "9237787", "9290576"], zeile.Pfad);
        Assert.Equal("017367", zeile.Bedingung);
    }

    [Fact]
    public void Parse_IgnoriertNumerischeMaterialnummerInSpalte11()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(7, 2).Value = "9268197";
        // Zeile ohne echte Hierarchie-Artikelnummer, aber mit Materialnummer in Spalte 11 und Bedingung.
        ws.Cell(9, 11).Value = "50000087019";
        ws.Cell(9, 13).Value = "020113";

        var zeilen = UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb));

        // Spalte 11 darf keinen Knoten erzeugen — nur die Wurzel existiert, ohne eigene Bedingung.
        Assert.DoesNotContain(zeilen, z => z.Pfad.Contains("50000087019"));
    }

    [Fact]
    public void Parse_LoestSieheKombUeberMerkmalskombinationsSpalteAuf()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(3, 20).Value = "Merkmalskombination"; // Kopf in Zeile 3
        ws.Cell(4, 20).Value = "Merkmalskombination";
        ws.Cell(7, 2).Value = "9268197";
        ws.Cell(9, 3).Value = "9269861";
        ws.Cell(9, 13).Value = "siehe Komb.";
        ws.Cell(9, 14).Value = "siehe Komb.";
        ws.Cell(9, 20).Value = "017367 oder 017369";

        var zeile = Assert.Single(UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb)));
        Assert.Equal("017367 oder 017369", zeile.Bedingung);
    }
}
