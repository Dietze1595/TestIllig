using Illig_AI_Platform.Shared.Lieferantenassistent;
using Xunit;

namespace Illig_AI_Platform.Tests.Lieferantenassistent;

public class LieferantenstammdatenCsvParserTests
{
    private const string Csv =
        "Kreditor;Land;Name 1;Ort;Postleitzahl;Straße;Adresse\n" +
        "2707;DE;Bosch Rexroth AG Electric Drives;Fellbach;70736;Siemensstrasse 1;88805\n" +
        "2906;DE;Nägele Mechanik GmbH;Murr;71711;Gottlieb-Daimler-Straße 72;93279\n" +
        "5683;DE;Weber Verzahnungstechnik GmbH;Kirchardt - Berwangen;74912;Am Bruchgraben 14;297665\n";

    [Fact]
    public void Parse_LiefertEineZeileProLieferant()
    {
        var ergebnis = LieferantenstammdatenCsvParser.Parse(Csv);

        Assert.Equal(3, ergebnis.Count);
    }

    [Fact]
    public void Parse_MapptAlleFelder()
    {
        var ergebnis = LieferantenstammdatenCsvParser.Parse(Csv);

        var erster = ergebnis[0];
        Assert.Equal(2707, erster.Kreditor);
        Assert.Equal("DE", erster.Land);
        Assert.Equal("Bosch Rexroth AG Electric Drives", erster.Name);
        Assert.Equal("Fellbach", erster.Ort);
        Assert.Equal(70736, erster.Postleitzahl);
        Assert.Equal("Siemensstrasse 1", erster.Strasse);
        Assert.Equal(88805, erster.AdressNummer);
    }

    [Fact]
    public void Parse_ParstFuehrendeNullenUndLeereOptionaleZahlenfelder()
    {
        const string csv =
            "Kreditor;Land;Name 1;Ort;Postleitzahl;Straße;Adresse\n" +
            "00002707;DE;Test GmbH;Berlin;;;\n";

        var lieferant = Assert.Single(LieferantenstammdatenCsvParser.Parse(csv));

        Assert.Equal(2707, lieferant.Kreditor);
        Assert.Null(lieferant.Postleitzahl);
        Assert.Null(lieferant.AdressNummer);
    }

    [Fact]
    public void Parse_UeberspringtUngueltigenKreditor()
    {
        const string csv =
            "Kreditor;Land;Name 1;Ort;Postleitzahl;Straße;Adresse\n" +
            "kein-kreditor;DE;Test GmbH;Berlin;10115;;12345\n";

        Assert.Empty(LieferantenstammdatenCsvParser.Parse(csv));
    }
}
