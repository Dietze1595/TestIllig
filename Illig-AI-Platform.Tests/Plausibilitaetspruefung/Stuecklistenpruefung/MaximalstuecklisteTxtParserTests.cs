using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class MaximalstuecklisteTxtParserTests
{
    // Wörtlicher Auszug aus Maximalstückliste_RDM75Kc_SAP_EXPORT.txt, Zeilen 5/7/9/11 (Header-
    // Zeilen 1-3 bereits entfernt). Spalten 0-16 = Hierarchie, 17 = Kurztext, 18 = Menge,
    // 19 = Status, 21 = BedMenge, 22 = Komp.-ME. Tab-Anzahl exakt wie im Original geprüft
    // (Python: line.split("\t"), siehe Design-Spec).
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
        Zeile(1, "9209307 0001 1 01", "RDM 75Kc_konf_ab      (877)"),
        Zeile(2, "0100 L 9237787", "Folieneinlauf_FB_200-900_konf", "        1", "    1,000", "ST"),
        Zeile(3, "9237787 0001 1 01", "Folieneinlauf_FB_200-900_konf"), // Selbstdeklaration: keine Menge
        Zeile(4, "0010 L 9000843", "Konsole", "        1", "    1,000", "ST"));

    [Fact]
    public void Parse_ErkenntWurzelposition()
    {
        var wurzel = MaximalstuecklisteTxtParser.Parse(Auszug);

        Assert.Equal("9209307", wurzel.Artikelnummer);
        Assert.Contains("RDM 75Kc", wurzel.Bezeichnung);
    }

    [Fact]
    public void Parse_ErkenntDirekteKindposition()
    {
        var wurzel = MaximalstuecklisteTxtParser.Parse(Auszug);

        var kind = Assert.Single(wurzel.Kinder);
        Assert.Equal("9237787", kind.Artikelnummer);
        Assert.Equal("Folieneinlauf_FB_200-900_konf", kind.Bezeichnung);
        Assert.Equal(1m, kind.Menge);
        Assert.Equal("ST", kind.Einheit);
    }

    [Fact]
    public void Parse_UeberspringtSelbstdeklarationUndDocktEnkelBeimRichtigenElternteilAn()
    {
        var wurzel = MaximalstuecklisteTxtParser.Parse(Auszug);

        // "Konsole" ist Kind von "Folieneinlauf" (nicht von der übersprungenen
        // Selbstdeklarationszeile), obwohl zwei Hierarchie-Spalten dazwischen liegen.
        var enkel = Assert.Single(wurzel.Kinder.Single().Kinder);
        Assert.Equal("9000843", enkel.Artikelnummer);
        Assert.Equal("Konsole", enkel.Bezeichnung);
    }

    [Fact]
    public void Parse_ErkenntAuftragsbezogeneWurzelzeile()
    {
        // Wörtlicher Auszug aus CSKB_11055894_40_SAP_EXPORT.txt: auftragsbezogene Stücklisten
        // haben ein anderes Wurzel-Token-Format ("<Auftragsnr> / <Position> <Artikel> <Menge>")
        // als die Maximalstückliste ("<Artikel> 0001 1 01") — beide müssen erkannt werden.
        var auszug = string.Join('\n',
            Zeile(1, "11055894 / 40 9209307 1", "RDM 75Kc_konf_ab      (877)"),
            Zeile(2, "0100 L 9237787", "Folieneinlauf_FB_200-900_konf", "        1", "    1,000", "ST"));

        var wurzel = MaximalstuecklisteTxtParser.Parse(auszug);

        var kind = Assert.Single(wurzel.Kinder);
        Assert.Equal("9237787", kind.Artikelnummer);
    }
}
