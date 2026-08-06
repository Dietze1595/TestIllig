using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class KundenuebersichtDesignTests
{
    [Fact]
    public void Uebersicht_ZeigtGeschaeftskennzahlenUndSuche()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Kunden", "Kunden.razor"));
        var css = File.ReadAllText(ClientFile("Pages", "Kunden", "Kunden.razor.css"));

        Assert.Contains("@page \"/illig-gpt/kunden\"", page);
        Assert.Contains("Name, Kundennummer oder Adresse suchen", page);
        Assert.Contains("Angebote", page);
        Assert.Contains("Bestellungen", page);
        Assert.Contains("Aufträge", page);
        Assert.Contains("Maschinen", page);
        Assert.Contains("<SeitenKopf", page);
        Assert.Contains("KundeStatus.Neukunde", page);
        Assert.Contains("kunden-summary", page);
        Assert.Contains("KundenFilter.Neukunden", page);
        Assert.Contains("KundenFilter.Vertriebsvorgaenge", page);
        Assert.Contains("aria-pressed", page);
        Assert.Contains("GefilterteKunden", page);
        Assert.Contains(".kunden-summary__tile.is-active", css);
        Assert.DoesNotContain("kunden-hero", page);
        Assert.Contains("#00457b", css);
        Assert.Contains("#9bc832", css);
        Assert.Contains("border: 1px solid rgba(98, 144, 255, .28);", css);
        Assert.Contains("border-color: rgba(98, 144, 255, .42);", css);
        Assert.Contains("background: #00457b;", css);
        Assert.Contains(".kunden-kpis", css);
    }

    [Fact]
    public void Detail_ZeigtAuftraegeMitMaschinenpositionen()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Kunden", "KundenDetail.razor"));
        var css = File.ReadAllText(ClientFile("Pages", "Kunden", "KundenDetail.razor.css"));

        Assert.Contains("@page \"/illig-gpt/kunden/detail\"", page);
        Assert.Contains("Aufträge und Maschinen", page);
        Assert.Contains("auftrag.Positionen", page);
        Assert.Contains("Position @position.Positionsnummer", page);
        Assert.Contains("<PdfViewerPanel", page);
        Assert.Contains("DokumentOeffnenAsync", page);
        Assert.Contains("KundenQuelltyp.Angebot", page);
        Assert.Contains("KundenQuelltyp.Kundenbestellung", page);
        Assert.Contains("KundenQuelltyp.Auftragsinformation", page);
        Assert.Contains("kunden-detail-main--pdf-offen", css);
        Assert.Contains(".kunden-document-link", css);
    }

    [Fact]
    public void Unternehmenssuche_OeffnetKundenuebersichtDirekt()
    {
        var home = File.ReadAllText(ClientFile("Pages", "Home.razor"));
        var page = File.ReadAllText(ClientFile("Pages", "IlligGpt.razor"));
        var uebersicht = File.ReadAllText(ClientFile("Pages", "Kunden", "Kunden.razor"));
        var detail = File.ReadAllText(ClientFile("Pages", "Kunden", "KundenDetail.razor"));

        Assert.Contains("AppRoutes.Kunden", home);
        Assert.Contains("AppRoutes.Kunden", page);
        Assert.DoesNotContain("enterprise-search-intro", page);
        Assert.Contains("AppClientRoles.SearchSystem", uebersicht);
        Assert.Contains("AppClientRoles.SearchSystem", detail);
        Assert.DoesNotContain("AppClientRoles.PlausibilityCheck", page);
    }

    private static string ClientFile(params string[] parts)
    {
        var all = new[] { TestSourceDirectory(), "..", "..", "Illig-AI-Platform.Client" }
            .Concat(parts).ToArray();
        return Path.GetFullPath(Path.Combine(all));
    }

    private static string TestSourceDirectory([CallerFilePath] string sourceFile = "") =>
        Path.GetDirectoryName(sourceFile)!;
}
