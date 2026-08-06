using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// TXT-Parser für das RDM73K-Format. Wie beim RDM75-Format ist es ein tab-getrennter SAP-Export,
/// aber die Wert-Spalten sind um zwei nach rechts verschoben: Kurztext = Spalte 19 (statt 17),
/// Menge = 20 (statt 18), Komp.-ME/Einheit = 24 (statt 22); Hierarchie damit in Spalten 0–18.
/// Tokens (Wurzel "&lt;Artikel&gt; 0001 1 01", Position "0100 L 9237787" / "0010 T") sind identisch.
/// </summary>
public class MaximalstuecklisteRdm73kTxtParserTests
{
    // Spaltenlayout des echten RDM73K-Exports: 19 = Kurztext, 20 = Menge, 23 = BedMenge, 24 = Komp.-ME.
    private static string Zeile(int hierarchieSpalte, string token, string? kurztext = null,
        string? menge = null, string? bedMenge = null, string? einheit = null)
    {
        var felder = new string[40];
        Array.Fill(felder, "");
        felder[hierarchieSpalte] = token;
        if (kurztext is not null) felder[19] = kurztext;
        if (menge is not null) felder[20] = menge;
        if (bedMenge is not null) felder[23] = bedMenge;
        if (einheit is not null) felder[24] = einheit;
        return string.Join('\t', felder);
    }

    private static readonly string Auszug = string.Join('\n',
        Zeile(1, "9254662 0001 1 01", "RDM 73K_konf_ab      (873)"),
        Zeile(2, "0100 L 9237787", "Folieneinlauf_FB_200-900_konf", "        1", "    1,000", "ST"),
        Zeile(3, "9237787 0001 1 01", "Folieneinlauf_FB_200-900_konf"), // Selbstdeklaration: keine Menge
        Zeile(4, "0010 L 9000843", "Konsole", "        1", "    1,000", "ST"));

    [Fact]
    public void Parse_ErkenntWurzelposition()
    {
        var wurzel = MaximalstuecklisteRdm73kTxtParser.Parse(Auszug);

        Assert.Equal("9254662", wurzel.Artikelnummer);
        Assert.Contains("RDM 73K", wurzel.Bezeichnung);
    }

    [Fact]
    public void Parse_ErkenntDirekteKindpositionMitMengeUndEinheit()
    {
        var wurzel = MaximalstuecklisteRdm73kTxtParser.Parse(Auszug);

        var kind = Assert.Single(wurzel.Kinder);
        Assert.Equal("9237787", kind.Artikelnummer);
        Assert.Equal("Folieneinlauf_FB_200-900_konf", kind.Bezeichnung);
        Assert.Equal(1m, kind.Menge);
        Assert.Equal("ST", kind.Einheit);
    }

    [Fact]
    public void Parse_UeberspringtSelbstdeklarationUndDocktEnkelBeimRichtigenElternteilAn()
    {
        var wurzel = MaximalstuecklisteRdm73kTxtParser.Parse(Auszug);

        var enkel = Assert.Single(wurzel.Kinder.Single().Kinder);
        Assert.Equal("9000843", enkel.Artikelnummer);
        Assert.Equal("Konsole", enkel.Bezeichnung);
    }
}
