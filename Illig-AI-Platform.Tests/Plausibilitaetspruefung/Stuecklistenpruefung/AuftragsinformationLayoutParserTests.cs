using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class AuftragsinformationLayoutParserTests
{
    [Fact]
    public void ErzeugePositionsText_VerbindetSpaltenAnhandDerYKoordinate()
    {
        // Koordinaten und absichtlich abweichende Lesereihenfolge entsprechen der echten
        // Azure-Antwort fuer Download (2).pdf. 012516 kommt dort vor 40/140 im Lesetext.
        IReadOnlyList<AuftragsinformationLayoutZeile> seite4 =
        [
            Zeile("40/70", 0.69f, 8.029f, 1.04f, 8.168f),
            Zeile("010770 Schmierung fuer Transportketten automatisch Heizungsregelung-Varianten:", 1.40f, 8.030f, 6.0f, 8.168f),
            Zeile("40/80", 0.69f, 8.697f, 1.04f, 8.835f),
            Zeile("40/90", 0.69f, 10.032f, 1.04f, 10.168f),
            Zeile("017364 Unterheizung mit Laengsreihenregelung", 1.40f, 8.697f, 5.0f, 8.835f),
            Zeile("010745 Aktive Strahlerfunktionskontrolle", 1.40f, 10.032f, 5.0f, 10.171f)
        ];
        IReadOnlyList<AuftragsinformationLayoutZeile> seite5 =
        [
            Zeile("012516", 1.40f, 8.606f, 1.87f, 8.745f),
            Zeile("Anschluss fuer Temperaturfuehler im Werkzeugblock", 1.40f, 8.82f, 5.2f, 8.96f),
            Zeile("40/140", 0.69f, 8.605f, 1.12f, 8.744f),
            Zeile("40/150", 0.69f, 9.440f, 1.12f, 9.577f),
            Zeile("020578 Vakuumeinrichtung Einbau vorbereitet", 1.40f, 9.440f, 5.2f, 9.577f)
        ];

        var layoutText = AuftragsinformationLayoutParser.ErzeugePositionsText([seite4, seite5]);
        var ergebnis = AuftragsinformationParser.Parse(
            "Pos.\nVertriebsmerkmale\n40/30\n024929 Grundmaschine",
            tabellenLayoutContent: layoutText);

        Assert.Equal("024929", Assert.Single(ergebnis.Merkmale, m => m.Position == "40/30").Merkmalsnummer);
        var position70 = Assert.Single(ergebnis.Merkmale, m => m.Position == "40/70");
        Assert.Equal("010770", position70.Merkmalsnummer);
        Assert.Equal("Schmierung fuer Transportketten automatisch", position70.Beschreibung);
        Assert.Equal("017364", Assert.Single(ergebnis.Merkmale, m => m.Position == "40/80").Merkmalsnummer);
        Assert.Equal("010745", Assert.Single(ergebnis.Merkmale, m => m.Position == "40/90").Merkmalsnummer);
        var position140 = Assert.Single(ergebnis.Merkmale, m => m.Position == "40/140");
        Assert.Equal("012516", position140.Merkmalsnummer);
        Assert.Contains("Temperaturfuehler", position140.Beschreibung);
        Assert.Equal("020578", Assert.Single(ergebnis.Merkmale, m => m.Position == "40/150").Merkmalsnummer);
    }

    private static AuftragsinformationLayoutZeile Zeile(
        string content, float minX, float minY, float maxX, float maxY) =>
        new(content, [minX, minY, maxX, minY, maxX, maxY, minX, maxY]);
}
