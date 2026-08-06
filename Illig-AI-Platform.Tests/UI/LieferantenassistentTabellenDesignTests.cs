using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class LieferantenassistentTabellenDesignTests
{
    [Fact]
    public void Tabelle_ZeigtMaterialnummerUndMaterialbezeichnungInGetrenntenSpalten()
    {
        var clientPfad = ClientPfad();
        var seite = File.ReadAllText(Path.Combine(
            clientPfad, "Pages", "Lieferantenassistent", "Lieferantenassistent.razor"));
        var styles = File.ReadAllText(Path.Combine(
            clientPfad, "Pages", "Lieferantenassistent", "Lieferantenassistent.razor.css"));

        Assert.Contains("<th>Materialnummer</th>", seite);
        Assert.Contains("<th>Materialbezeichnung</th>", seite);
        Assert.Contains("<td class=\"la-materialnummer\">@position.Material</td>", seite);
        Assert.Contains("<td>@position.Kurztext</td>", seite);
        Assert.DoesNotContain("@position.Material – @position.Kurztext", seite);
        Assert.Contains("colspan=\"9\"", seite);
        Assert.Contains(".la-materialnummer", styles);
        Assert.Contains("white-space: nowrap", styles);
        Assert.Contains(
            "<th class=\"la-mengenspalte\">Offen / Gesamt</th>",
            seite);
        Assert.Contains(
            "<td class=\"la-mengenspalte\">@($\"{position.NochZuLiefernMenge:0} / {position.Bestellmenge:0}\")</td>",
            seite);
        Assert.Contains(".la-mengenspalte", styles);
    }

    private static string ClientPfad([CallerFilePath] string testDateiPfad = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(testDateiPfad)!,
            "..",
            "..",
            "Illig-AI-Platform.Client"));
}
