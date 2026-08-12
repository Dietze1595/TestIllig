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
    public void Parse_VerknuepftMehrereBedingungsspaltenMitUnd()
    {
        // Echter Fall 9268432 (Zeile 208): Formluft-Variante (AF) UND NICHT Kondenswasser (AM),
        // beide Spalten gefüllt, KEIN "siehe Komb." — müssen mit UND kombiniert werden.
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(7, 2).Value = "9268197";
        ws.Cell(9, 3).Value = "9268432";
        ws.Cell(9, 13).Value = "017366 / 017367"; // erste Bedingungsspalte
        ws.Cell(9, 15).Value = "N020121";          // weitere Bedingungsspalte

        var zeile = Assert.Single(UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb)));
        Assert.Equal("(017366 / 017367) U (N020121)", zeile.Bedingung);
    }

    [Theory]
    [InlineData("s. Kombi")]  // mit Leerzeichen (echte Zeile 427)
    [InlineData("s.Kombi")]   // ohne Leerzeichen (echte Zeile 416)
    [InlineData("s.Komb.")]   // abgekürzt
    public void Parse_LoestAbgekuerzteKombiMarkerUeberKombinationsSpalteAuf(string marker)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(3, 20).Value = "Merkmalskombination"; // Kopf in Zeile 3
        ws.Cell(7, 2).Value = "9268197";
        ws.Cell(9, 3).Value = "9251322";
        ws.Cell(9, 13).Value = marker;
        ws.Cell(9, 20).Value = "014903 / 020581 / 021138";

        var zeile = Assert.Single(UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb)));
        Assert.Equal("014903 / 020581 / 021138", zeile.Bedingung);
    }

    [Fact]
    public void Parse_LoestMischzelleMitMerkmalUndKombiMarkerUeberKombinationsSpalteAuf()
    {
        // Echte Zeile 149/150: Zelle enthält Merkmalsnummer UND Marker ("022689 s.Komb."),
        // die vollständige Bedingung steht in der Kombinationsspalte.
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(3, 20).Value = "Merkmalskombination";
        ws.Cell(7, 2).Value = "9268197";
        ws.Cell(9, 3).Value = "9280490";
        ws.Cell(9, 13).Value = "022689 s.Komb.";
        ws.Cell(9, 14).Value = "siehe Komb.";
        ws.Cell(9, 20).Value = "022689 oder 022078";

        var zeile = Assert.Single(UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb)));
        Assert.Equal("022689 oder 022078", zeile.Bedingung);
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

    [Fact]
    public void Parse_ErhaeltFuehrendeNullAusZahlenformat()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(7, 2).Value = "9268197";
        ws.Cell(9, 3).Value = "9312266";
        ws.Cell(9, 13).Value = 25643;
        ws.Cell(9, 13).Style.NumberFormat.Format = "000000";

        var zeile = Assert.Single(UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb)));

        Assert.Equal("025643", zeile.Bedingung);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("X")]
    public void Parse_InterpretiertSonderspannungsmarkerAlsNegation(string marker)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(3, 13).Value = "Stromversorgung (9020016) Standard, (x) Sonderspannung";
        ws.Cell(7, 2).Value = "9268197";
        ws.Cell(9, 3).Value = "9061560";
        ws.Cell(9, 13).Value = marker;

        var zeile = Assert.Single(UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb)));

        Assert.Equal("N9020016", zeile.Bedingung);
    }

    [Fact]
    public void Parse_EntferntMaschinenhinweiseAusRdm76Bedingung()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(7, 2).Value = "9268197";
        ws.Cell(9, 3).Value = "9271839";
        ws.Cell(9, 13).Value = "75Kc";
        ws.Cell(9, 14).Value = "76K";
        ws.Cell(9, 15).Value = "020575 / 020576 / 020577";

        var zeile = Assert.Single(UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb)));

        Assert.Equal("020575 / 020576 / 020577", zeile.Bedingung);
    }

    [Fact]
    public void Parse_UnterscheidetDoppeltePfadeNachVorkommen()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Umsetztabelle RDM 76Kb");
        ws.Cell(7, 2).Value = "9268197";
        ws.Cell(9, 3).Value = "9271478";
        ws.Cell(9, 13).Value = "022073";
        ws.Cell(10, 3).Value = "9271478";
        ws.Cell(10, 14).Value = "022074";

        var zeilen = UmsetzungsmatrixRdm76kbXlsxParser.Parse(Speichern(wb));

        Assert.Collection(
            zeilen,
            erste =>
            {
                Assert.Equal(["9268197", "9271478"], erste.Pfad);
                Assert.Equal("022073", erste.Bedingung);
                Assert.Equal(0, erste.PfadVorkommen);
            },
            zweite =>
            {
                Assert.Equal(["9268197", "9271478"], zweite.Pfad);
                Assert.Equal("022074", zweite.Bedingung);
                Assert.Equal(1, zweite.PfadVorkommen);
            });
    }
}
