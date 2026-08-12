using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// TXT-Format-Weiche: wählt anhand des beim Import gewählten <see cref="StuecklistenImportFormat"/>
/// den passenden Maximalstückliste-Parser (RDM75 = Spalten 17/18/22, RDM73K = 19/20/24).
/// </summary>
public class MaximalstuecklisteParserTests
{
    private static string Zeile(int hierarchieSpalte, string token, int kurztextSpalte, string kurztext)
    {
        var felder = new string[40];
        Array.Fill(felder, "");
        felder[hierarchieSpalte] = token;
        felder[kurztextSpalte] = kurztext;
        return string.Join('\t', felder);
    }

    [Fact]
    public void Parse_MitRdm75Format_LiestKurztextAusSpalte17()
    {
        var inhalt = Zeile(1, "9209307 0001 1 01", 17, "RDM 75Kc_konf");

        var wurzel = MaximalstuecklisteParser.Parse(inhalt, StuecklistenImportFormat.Rdm75Kc);

        Assert.Equal("9209307", wurzel.Artikelnummer);
        Assert.Equal("RDM 75Kc_konf", wurzel.Bezeichnung);
    }

    [Fact]
    public void Parse_MitRdm73kFormat_LiestKurztextAusSpalte19()
    {
        var inhalt = Zeile(1, "9254662 0001 1 01", 19, "RDM 73K_konf");

        var wurzel = MaximalstuecklisteParser.Parse(inhalt, StuecklistenImportFormat.Rdm73k);

        Assert.Equal("9254662", wurzel.Artikelnummer);
        Assert.Equal("RDM 73K_konf", wurzel.Bezeichnung);
    }

    [Fact]
    public void Parse_MitRdk80Format_VerwendetEigenenParser()
    {
        var inhalt = Zeile(1, "9209425 0001 1 01", 19, "RDK 80k_Siemens_konf");

        var wurzel = MaximalstuecklisteParser.Parse(inhalt, StuecklistenImportFormat.Rdk80k);

        Assert.Equal("9209425", wurzel.Artikelnummer);
        Assert.Equal("RDK 80k_Siemens_konf", wurzel.Bezeichnung);
    }

    [Fact]
    public void Parse_MitUnbekanntemFormat_WirftArgumentException()
    {
        var inhalt = Zeile(1, "9254662 0001 1 01", 19, "RDM 73K_konf");

        Assert.ThrowsAny<ArgumentException>(
            () => MaximalstuecklisteParser.Parse(inhalt, (StuecklistenImportFormat)999));
    }
}
