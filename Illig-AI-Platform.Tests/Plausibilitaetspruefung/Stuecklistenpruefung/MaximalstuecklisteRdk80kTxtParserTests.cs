using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// TXT-Parser für das RDK80k-Format: Hierarchie in Spalten 0–18, Kurztext in 19,
/// Menge in 20 und Einheit in 24. Anders als die bisherigen Fixtures enthält der echte
/// Export auch alphanumerische und mehrteilige Material-/Dokumentnummern.
/// </summary>
public class MaximalstuecklisteRdk80kTxtParserTests
{
    private static string Zeile(
        int hierarchieSpalte,
        string token,
        string? kurztext = null,
        string? menge = null,
        string? einheit = null)
    {
        var felder = new string[40];
        Array.Fill(felder, "");
        felder[hierarchieSpalte] = token;
        if (kurztext is not null) felder[19] = kurztext;
        if (menge is not null) felder[20] = menge;
        if (einheit is not null) felder[24] = einheit;
        return string.Join('\t', felder);
    }

    private static readonly string Auszug = string.Join('\n',
        Zeile(1, "9209425 0001 1 01", "RDK 80k_Siemens_konf_ab_01.2013"),
        Zeile(2, "0005 N DOKU_RDK80K_72K", "Dokument", "1", "ST"),
        Zeile(3, "DOKU_RDK80K_72K 0001 1 01", "Dokument"),
        Zeile(4, "0020 D ZTE P 561 300 022 000 B", "Strahlerplan", "1", "ST"),
        Zeile(2, "0100 L 9209711", "Formmaschine", "3.654,900", "ST"));

    [Fact]
    public void Parse_LiestRdk80WurzelKurztextUndSpaltenlayout()
    {
        var wurzel = MaximalstuecklisteRdk80kTxtParser.Parse(Auszug);

        Assert.Equal("9209425", wurzel.Artikelnummer);
        Assert.Equal("RDK 80k_Siemens_konf_ab_01.2013", wurzel.Bezeichnung);
    }

    [Fact]
    public void Parse_ExtrahiertAlphanumerischeUndMehrteiligeMaterialnummern()
    {
        var wurzel = MaximalstuecklisteRdk80kTxtParser.Parse(Auszug);

        var dokument = wurzel.Kinder[0];
        Assert.Equal("DOKU_RDK80K_72K", dokument.Artikelnummer);
        var zeichnung = Assert.Single(dokument.Kinder);
        Assert.Equal("ZTE P 561 300 022 000 B", zeichnung.Artikelnummer);
    }

    [Fact]
    public void Parse_UeberspringtSelbstdeklarationUndErhaeltBaumstruktur()
    {
        var wurzel = MaximalstuecklisteRdk80kTxtParser.Parse(Auszug);

        Assert.Equal(2, wurzel.Kinder.Count);
        Assert.Single(wurzel.Kinder[0].Kinder);
        Assert.Equal("9209711", wurzel.Kinder[1].Artikelnummer);
    }

    [Fact]
    public void Parse_LiestDeutscheMengeMitTausendertrennzeichen()
    {
        var wurzel = MaximalstuecklisteRdk80kTxtParser.Parse(Auszug);

        var formmaschine = wurzel.Kinder[1];
        Assert.Equal(3654.900m, formmaschine.Menge);
        Assert.Equal("ST", formmaschine.Einheit);
    }

    [Fact]
    public void Parse_OhneGueltigeWurzel_WirftFormatException()
    {
        var inhalt = string.Join('\n',
            "Produktstruktur: Gültigkeitsdatum 08.07.2026",
            Zeile(0, "Produktstruktur", "Kurztext"));

        Assert.Throws<FormatException>(() => MaximalstuecklisteRdk80kTxtParser.Parse(inhalt));
    }
}
