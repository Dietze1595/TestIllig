using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// TXT-Parser für das RDM76Kb-Format. Die Maximalstückliste nutzt hier das gleiche Spaltenlayout
/// wie RDM75 (Kurztext = 17, Menge = 18, Komp.-ME = 22, Hierarchie 0–16); bewusst als eigene
/// Klasse gehalten, damit Anpassungen für RDM76Kb die anderen Formate nicht berühren.
/// </summary>
public class MaximalstuecklisteRdm76kbTxtParserTests
{
    private static string Zeile(int hierarchieSpalte, string token, string? kurztext = null,
        string? menge = null, string? bedMenge = null, string? einheit = null)
    {
        var felder = new string[38];
        Array.Fill(felder, "");
        felder[hierarchieSpalte] = token;
        if (kurztext is not null) felder[17] = kurztext;
        if (menge is not null) felder[18] = menge;
        if (bedMenge is not null) felder[21] = bedMenge;
        if (einheit is not null) felder[22] = einheit;
        return string.Join('\t', felder);
    }

    private static readonly string Auszug = string.Join('\n',
        Zeile(1, "9268197 0001 1 01", "RDM 76Kb_konf_ab      (876)"),
        Zeile(2, "0100 L 9237787", "Folieneinlauf_FB_200-900_konf", "        1", "    1,000", "ST"),
        Zeile(3, "9237787 0001 1 01", "Folieneinlauf_FB_200-900_konf"), // Selbstdeklaration
        Zeile(4, "0010 L 9000843", "Konsole", "        1", "    1,000", "ST"));

    [Fact]
    public void Parse_ErkenntWurzelposition()
    {
        var wurzel = MaximalstuecklisteRdm76kbTxtParser.Parse(Auszug);

        Assert.Equal("9268197", wurzel.Artikelnummer);
        Assert.Contains("RDM 76Kb", wurzel.Bezeichnung);
    }

    [Fact]
    public void Parse_ErkenntDirekteKindpositionMitMengeUndEinheit()
    {
        var wurzel = MaximalstuecklisteRdm76kbTxtParser.Parse(Auszug);

        var kind = Assert.Single(wurzel.Kinder);
        Assert.Equal("9237787", kind.Artikelnummer);
        Assert.Equal(1m, kind.Menge);
        Assert.Equal("ST", kind.Einheit);
    }

    [Fact]
    public void Parse_UeberspringtSelbstdeklarationUndDocktEnkelBeimRichtigenElternteilAn()
    {
        var wurzel = MaximalstuecklisteRdm76kbTxtParser.Parse(Auszug);

        var enkel = Assert.Single(wurzel.Kinder.Single().Kinder);
        Assert.Equal("9000843", enkel.Artikelnummer);
    }
}
