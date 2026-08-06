using Illig_AI_Platform.Shared.Lieferantenassistent;
using Xunit;

namespace Illig_AI_Platform.Tests.Lieferantenassistent;

public class LieferantEmailAdresseCsvParserTests
{
    private const string Csv =
        "Adressnummer;E-Mail-Adresse;Standard-Adr.\n" +
        "88805;heike.mueller2@boschrexroth.de;X\n" +
        "93279;auftrag@naegele-mechanik.de;X\n" +
        "93279;michael.gessler@naegele-mechanik.de;\n" +
        "297665;bestellung@weber-verzahnungstechnik.de;X\n" +
        "297665;anfrage@weber-verzahnungstechnik.de;\n";

    [Fact]
    public void Parse_LiefertEineZeileProEmailAdresse()
    {
        var ergebnis = LieferantEmailAdresseCsvParser.Parse(Csv);

        Assert.Equal(5, ergebnis.Count);
    }

    [Fact]
    public void Parse_MapptAdressNummerUndStandardFlag()
    {
        var ergebnis = LieferantEmailAdresseCsvParser.Parse(Csv);

        var standardAdresse = ergebnis[0];
        Assert.Equal(88805, standardAdresse.AdressNummer);
        Assert.Equal("heike.mueller2@boschrexroth.de", standardAdresse.EmailAdresse);
        Assert.True(standardAdresse.IstStandard);

        var nichtStandardAdresse = ergebnis[2];
        Assert.Equal(93279, nichtStandardAdresse.AdressNummer);
        Assert.False(nichtStandardAdresse.IstStandard);
    }

    [Fact]
    public void Parse_ErlaubtMehrereEmailsProAdressNummer()
    {
        var ergebnis = LieferantEmailAdresseCsvParser.Parse(Csv);

        Assert.Equal(2, ergebnis.Count(e => e.AdressNummer == 93279));
        Assert.Equal(2, ergebnis.Count(e => e.AdressNummer == 297665));
    }

    [Fact]
    public void Parse_UeberspringtUngueltigeAdressNummer()
    {
        const string csv =
            "Adressnummer;E-Mail-Adresse;Standard-Adr.\n" +
            "ungueltig;test@example.com;X\n";

        Assert.Empty(LieferantEmailAdresseCsvParser.Parse(csv));
    }
}
