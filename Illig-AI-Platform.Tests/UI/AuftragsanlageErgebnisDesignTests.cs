using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class AuftragsanlageErgebnisDesignTests
{
    [Fact]
    public void Ergebnisansicht_NutztDesignmusterVonStuecklisteUndSondermerkmalen()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor"));

        Assert.Contains("sm-detail__head", page);
        Assert.Contains("<dl class=\"sm-info\">", page);
        Assert.Contains("sm-comparison__summary", page);
        Assert.Contains("sm-feature-group", page);
        Assert.Contains("<table class=\"sm-feature-table\">", page);
        Assert.Contains("sm-feature-table-wrap", page);
        Assert.Contains("AbweichungsKategorie(abweichung)", page);
        Assert.Contains("_antwort.Uebereinstimmungen", page);
        Assert.Contains("Bestätigte Übereinstimmungen", page);
        Assert.Contains("AbweichungsKategorie(uebereinstimmung)", page);
        Assert.Contains("sm-feature-group sm-feature-group--match", page);
        Assert.Contains("sm-feature-group--missing", page);
        Assert.Contains("Aus dem Angebot ausgelesene Daten", page);
        Assert.Contains("Nicht erkannt", page);
        Assert.Contains("<SeitenKopf Titel=\"Bestätigungsabgleich\"", page);
        Assert.Contains("<div class=\"aa-innendienst-content\">", page);
        Assert.True(
            page.IndexOf("<SeitenKopf Titel=\"Bestätigungsabgleich\"", StringComparison.Ordinal) <
            page.IndexOf("<div class=\"aa-innendienst-content\">", StringComparison.Ordinal));
        Assert.DoesNotContain("aa-ergebnis__kopf", page);
    }

    [Fact]
    public void Ergebnisansicht_VerwendetIlligFarbenUndResponsiveLayout()
    {
        var styles = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor.css"));

        Assert.Contains("#00457b", styles, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#9bc832", styles, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".sm-info", styles);
        Assert.Contains(".sm-comparison__summary", styles);
        Assert.Contains(".sm-feature-group", styles);
        Assert.Contains(".sm-feature-table th", styles);
        Assert.Contains(".sm-feature-table td", styles);
        Assert.Contains("border-left: 3px solid transparent", styles);
        Assert.DoesNotContain("padding-left: 7.5rem", styles);
        Assert.Contains(".aa-innendienst-content", styles);
        Assert.Contains("padding-inline: 4rem", styles);
        Assert.Contains("padding-inline: 0", styles);
        Assert.Contains(".su-main--verlauf-offen", styles);
        Assert.Contains("padding: .55rem .75rem", styles);
        Assert.Contains("padding: .7rem .75rem", styles);
        Assert.Contains("background: color-mix(in srgb, #00457b 8%, var(--color-bg-panel))", styles);
        Assert.Contains("@media (max-width: 760px)", styles);
        Assert.Contains("grid-template-columns: 1fr", styles);
    }

    [Fact]
    public void Vertriebsansicht_NutztDasselbeDesignsystemWieInnendienst()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));
        var styles = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor.css"));
        var checklistStyles = File.ReadAllText(ClientFile(
            "Components", "ChecklistItem.razor.css"));

        Assert.Contains("sm-detail__head", page);
        Assert.Contains("<dl class=\"sm-info\">", page);
        Assert.Contains("Aus dem Angebot ausgelesene Daten", page);
        Assert.Contains("sm-feature-group", page);
        Assert.Contains("<table class=\"sm-feature-table\">", page);
        Assert.Contains("sm-feature-table-wrap", page);
        Assert.True(
            page.LastIndexOf("<SapBestaetigungen", StringComparison.Ordinal) <
            page.LastIndexOf("<div class=\"aa-status-legende\"", StringComparison.Ordinal));
        Assert.True(
            page.LastIndexOf("<div class=\"sm-actions\"", StringComparison.Ordinal) <
            page.LastIndexOf("aa-freigabe-status", StringComparison.Ordinal));
        Assert.Contains("role=\"status\"", page);
        Assert.True(
            page.IndexOf("<div class=\"su-main", StringComparison.Ordinal) <
            page.IndexOf("<SeitenKopf Titel=\"Angebotsprüfung\"", StringComparison.Ordinal));
        Assert.Contains("<div class=\"aa-vertrieb-content\">", page);
        Assert.True(
            page.IndexOf("<SeitenKopf Titel=\"Angebotsprüfung\"", StringComparison.Ordinal) <
            page.IndexOf("<div class=\"aa-vertrieb-content\">", StringComparison.Ordinal));
        Assert.Contains("#00457b", styles, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#9bc832", styles, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".aa-checkliste", styles);
        Assert.Contains("border-left: 3px solid #9ca9b1", styles);
        Assert.Contains("box-sizing: border-box", styles);
        Assert.Contains("min-width: 0", styles);
        Assert.DoesNotContain("padding-left: 7.5rem", styles);
        Assert.Contains(".aa-vertrieb-content", styles);
        Assert.Contains("padding-inline: 4rem", styles);
        Assert.Contains("padding-inline: 0", styles);
        Assert.Contains(".su-main--verlauf-offen", styles);
        Assert.Contains(".sm-feature-table th", styles);
        Assert.Contains(".sm-feature-table td", styles);
        Assert.Contains("border-left: 3px solid transparent", styles);
        Assert.Contains("border-radius: 0", styles);
        Assert.Contains("padding: .55rem .75rem", styles);
        Assert.Contains("padding: .7rem .75rem", styles);
        Assert.Contains("background: color-mix(in srgb, #00457b 8%, var(--color-bg-panel))", styles);
        Assert.Contains("@media (max-width: 760px)", styles);
        Assert.Contains("border-left: 3px solid transparent", checklistStyles);
        Assert.Contains(".aa-prueffeld--ok", checklistStyles);
        Assert.Contains(".aa-prueffeld--fehlt", checklistStyles);
    }

    [Fact]
    public void AusgeleseneZahlungsbedingung_ZeigtKeinenInternAbgeleitetenCode()
    {
        var vertrieb = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));
        var innendienst = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor"));

        Assert.DoesNotContain("Zahlungsbedingung (A14/A30/ALC)", vertrieb);
        Assert.DoesNotContain("ZahlungsbedingungCode", vertrieb);
        Assert.DoesNotContain("Zahlungsbedingung (A14/A30/ALC)", innendienst);
        Assert.DoesNotContain("ZahlungsbedingungCode", innendienst);
    }

    [Fact]
    public void Angebotscheckliste_BenenntDasAusgeleseneTransportfeldAlsVersandart()
    {
        var vertrieb = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));
        var innendienst = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor"));

        Assert.Contains("Label=\"Versandart\"", vertrieb);
        Assert.DoesNotContain("Label=\"Versandbedingung\"", vertrieb);
        Assert.Contains("Label=\"Versandart\"", innendienst);
        Assert.DoesNotContain("Label=\"Versandbedingung\"", innendienst);
    }

    [Fact]
    public void Vertriebsansicht_KennzeichnetSapBestaetigungenAlsManuellePruefung()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));
        var styles = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor.css"));

        Assert.Contains("Manuelle Prüfung erforderlich", page);
        Assert.Contains("mit den in SAP hinterlegten Daten", page);
        Assert.True(
            page.IndexOf("aa-sap-pruefhinweis", StringComparison.Ordinal) <
            page.IndexOf("<SapBestaetigungen", StringComparison.Ordinal));
        Assert.Contains(".aa-sap-pruefhinweis", styles);
    }

    [Fact]
    public void Vertriebsansicht_ZeigtGespeicherteSapBestaetigungenInDerHistorie()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));

        Assert.Contains("Sparte=\"@(_detail.SapSparteBestaetigt == true)\"", page);
        Assert.Contains("Fuehrend=\"@(_detail.SapFuehrendBestaetigt == true)\"", page);
        Assert.Contains("Disabled=\"true\"", page);
        Assert.True(
            page.IndexOf("_detail.SapSparteBestaetigt", StringComparison.Ordinal) <
            page.IndexOf("<div class=\"aa-status-legende\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Vertriebsansicht_HaeltAngebotsdetailsFestUndScrolltNurPruefdaten()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));
        var styles = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor.css"));
        var appStyles = File.ReadAllText(ClientFile("wwwroot", "app.css"));

        Assert.Contains("PrueflayoutAktiv ? \"aa-vertrieb-prueflayout-aktiv\"", page);
        Assert.Equal(2, Count(page, "class=\"aa-vertrieb-prueflayout\""));
        Assert.Equal(2, Count(page, "aa-vertrieb-prueflayout__kopf"));
        Assert.Equal(2, Count(page, "aa-vertrieb-prueflayout__inhalt"));
        Assert.Contains("@media (min-width: 901px)", styles);
        Assert.Contains(".aa-vertrieb-prueflayout-aktiv", styles);
        Assert.Contains("flex: 1 1 auto", styles);
        Assert.Contains("grid-template-rows: max-content minmax(0, 1fr)", styles);
        Assert.Contains(".aa-vertrieb-prueflayout__inhalt", styles);
        Assert.Contains("overflow-y: auto", styles);
        Assert.Contains("overflow-anchor: none", styles);
        Assert.Contains("overscroll-behavior: contain", styles);
        Assert.Contains("body:has(.aa-vertrieb-prueflayout-aktiv)", appStyles);
        Assert.Contains("height: 100dvh", appStyles);
        Assert.Contains("overflow: hidden", appStyles);
    }

    [Fact]
    public void SapCheckboxen_BehaltenBeimFokusIhreSichtbarePosition()
    {
        var styles = File.ReadAllText(ClientFile(
            "Components", "Auftragsanlage", "SapBestaetigungen.razor.css"));

        Assert.Contains(".aa-sap-bestaetigung", styles);
        Assert.Contains("position: relative", styles);
        Assert.Contains(".aa-sap-bestaetigung__input", styles);
        Assert.Contains("top: 50%", styles);
        Assert.Contains("left: .85rem", styles);
        Assert.Contains("width: 1.45rem", styles);
        Assert.Contains("height: 1.45rem", styles);
        Assert.Contains("opacity: 0", styles);
        Assert.DoesNotContain("clip: rect(0, 0, 0, 0)", styles);
        Assert.DoesNotContain("clip-path: inset(50%)", styles);
    }

    [Fact]
    public void Vertriebsansicht_ZeigtInfoIconsAuchImLesemodus()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));
        var leseModusStart = page.IndexOf("@if (_readOnly && _detail is not null)", StringComparison.Ordinal);
        var leseModusEnde = page.IndexOf("<section class=\"sm-card aa-vertrieb-prueflayout__inhalt\">", leseModusStart, StringComparison.Ordinal);
        var angebotsdetails = page[leseModusStart..leseModusEnde];

        Assert.Equal(4, Count(angebotsdetails, "class=\"aa-info-icon\""));
        Assert.Contains("Angebotsnummer", angebotsdetails);
        Assert.Contains("Version", angebotsdetails);
        Assert.Contains("Status", angebotsdetails);
        Assert.Contains("Gesamtpreis", angebotsdetails);
    }

    [Fact]
    public void Vertriebsansicht_InfoIconsFolgenDerThemeTextfarbe()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));
        var styles = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor.css"));
        var checklist = File.ReadAllText(ClientFile("Components", "ChecklistItem.razor"));

        Assert.Contains(".aa-info-icon", styles);
        Assert.Contains("stroke: currentColor", styles);
        Assert.DoesNotContain("stroke: white", styles);
        Assert.Contains("<ChecklistItem", page);
        Assert.Contains("StatusSymbolAnzeigen=\"false\"", page);
        Assert.Contains("StatusSymbolAnzeigen { get; set; } = true", checklist);
        Assert.DoesNotContain("sm-feature-group__icon", page);
        Assert.DoesNotContain(".sm-feature-group__icon", styles);
        Assert.Contains("<h3>Erkannte Positionen</h3>", page);
    }

    [Fact]
    public void Innendienstansicht_InfoIconsFolgenDerThemeTextfarbe()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor"));
        var styles = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor.css"));
        var checklist = File.ReadAllText(ClientFile("Components", "ChecklistItem.razor"));

        Assert.Contains(".aa-info-icon", styles);
        Assert.Contains("stroke: currentColor", styles);
        Assert.DoesNotContain("stroke: white", styles);
        Assert.Contains("<ChecklistItem", page);
        Assert.Contains("StatusSymbolAnzeigen=\"false\"", page);
        Assert.Contains("if (StatusSymbolAnzeigen)", checklist);
        Assert.Contains("StatusSymbolAnzeigen { get; set; } = true", checklist);
        Assert.DoesNotContain("sm-feature-group__icon", page);
        Assert.DoesNotContain(".sm-feature-group__icon", styles);
        Assert.DoesNotContain("? \"✓\" : \"!\"", page);
        Assert.Contains("Angebotsstatus: Freigegeben", page);
        Assert.Contains("Liefertermin: Identisch", page);
        Assert.DoesNotContain("<dl class=\"sm-info aa-vergleich-werte\">", page);
        Assert.DoesNotContain("Die Liefertermine stimmen überein.", page);
        Assert.DoesNotContain(".aa-vergleich-werte", styles);
    }

    [Fact]
    public void Innendienstansicht_ZeigtFreigabedetailsInEinemPopoverAmStatus()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor"));
        var styles = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor.css"));

        Assert.Contains("class=\"aa-vertriebsstatus", page);
        Assert.Contains("angebotStatus.FreigegebenAm", page);
        Assert.Contains("angebotStatus.FreigegebenVon", page);
        Assert.Contains("class=\"aa-vertriebsstatus__info\"", page);
        Assert.Contains("class=\"aa-vertriebsstatus__trigger\"", page);
        Assert.Contains("class=\"aa-vertriebsstatus__popover\"", page);
        Assert.Contains("Details zur Freigabe", page);
        Assert.Contains("Freigegeben von", page);
        Assert.DoesNotContain("<p class=\"aa-freigabe-meta\">", page);
        Assert.DoesNotContain("aa-vertriebsstatus__meta", page);
        Assert.DoesNotContain("<details class=\"aa-vertriebsstatus__info\">", page);
        Assert.Contains(".aa-vertriebsstatus__trigger", styles);
        Assert.Contains("color: inherit", styles);
        Assert.Contains(".aa-vertriebsstatus__popover", styles);
        Assert.Contains(".aa-vertriebsstatus__info:hover .aa-vertriebsstatus__popover", styles);
        Assert.Contains(".aa-vertriebsstatus__info:focus-within .aa-vertriebsstatus__popover", styles);
    }

    [Fact]
    public void Innendienstansicht_HaeltAngebotsdetailsFestUndScrolltNurDenUnterenBereich()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor"));
        var styles = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor.css"));
        var appStyles = File.ReadAllText(ClientFile("wwwroot", "app.css"));

        Assert.Contains("PrueflayoutAktiv ? \"sm-page--dokument-aktiv aa-innendienst-prueflayout-aktiv\"", page);
        Assert.Contains("class=\"aa-innendienst-prueflayout\"", page);
        Assert.Contains("aa-innendienst-prueflayout__kopf", page);
        Assert.Contains("class=\"sm-card aa-innendienst-prueflayout__inhalt\"", page);
        Assert.Contains("class=\"aa-innendienst-prueflayout__scroll\"", page);
        Assert.Equal(2, Count(page, "aa-innendienst-scrolltitel"));
        Assert.Contains("class=\"aa-innendienst-scrollsektion\"", page);
        Assert.Contains("class=\"aa-innendienst-vergleich\"", page);
        Assert.DoesNotContain("<section class=\"sm-card mt-4\">", page);
        Assert.Contains("@media (min-width: 901px)", styles);
        Assert.Contains(".aa-innendienst-prueflayout__inhalt", styles);
        Assert.Contains(".aa-innendienst-prueflayout__scroll", styles);
        Assert.Contains("overflow: hidden", styles);
        Assert.Contains("overflow-y: auto", styles);
        Assert.Contains("overscroll-behavior: contain", styles);
        Assert.Contains(".aa-innendienst-scrolltitel", styles);
        Assert.Contains("position: sticky", styles);
        Assert.Contains("top: 0", styles);
        Assert.Contains("--aa-innendienst-scrolltitel-hoehe", styles);
        Assert.Contains("height: var(--aa-innendienst-scrolltitel-hoehe)", styles);
        Assert.Contains("class=\"aa-checkliste-tabelle\"", page);
        Assert.True(
            page.IndexOf("class=\"aa-checkliste-tabelle\"", StringComparison.Ordinal) <
            page.IndexOf("class=\"aa-checkliste-kopf\"", StringComparison.Ordinal));
        Assert.True(
            page.IndexOf("</ul>", page.IndexOf("class=\"aa-checkliste-tabelle\"", StringComparison.Ordinal), StringComparison.Ordinal) <
            page.IndexOf("class=\"aa-status-legende\"", StringComparison.Ordinal));
        Assert.Contains(".aa-checkliste-tabelle", styles);
        Assert.Contains(".aa-checkliste-kopf", styles);
        Assert.Contains("top: var(--aa-innendienst-scrolltitel-hoehe)", styles);
        Assert.Equal(2, Count(page, "aa-innendienst-sticky-tabelle"));
        Assert.Contains(".aa-innendienst-sticky-tabelle .sm-feature-table thead th", styles);
        Assert.Contains(".aa-innendienst-vergleich .sm-feature-table-wrap", styles);
        Assert.Contains(".aa-innendienst-scrollsektion", styles);
        Assert.Contains(".aa-innendienst-vergleich", styles);
        Assert.Contains("body:has(.aa-innendienst-prueflayout-aktiv)", appStyles);
    }

    [Fact]
    public void Innendienstansicht_ZeigtManuelleSapPruefungBeiDenAusgelesenenDaten()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor"));
        var component = File.ReadAllText(ClientFile(
            "Components", "Auftragsanlage", "SapBestaetigungen.razor"));

        Assert.Contains("aa-innendienst-sapstatus", page);
        Assert.Contains("Vom Vertrieb manuell geprüft", page);
        Assert.Contains("vom Vertrieb mit den Daten in SAP abgeglichen", page);
        Assert.True(
            page.IndexOf("aa-innendienst-prueflayout__scroll", StringComparison.Ordinal) <
            page.IndexOf("<SapBestaetigungen", StringComparison.Ordinal));
        Assert.DoesNotContain("Kompakt=", page);
        Assert.DoesNotContain("Kompakt", component);
    }

    [Fact]
    public void Innendienstansicht_ZeigtFalscheDokumentartAlsEinzelnenUploadHinweis()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageInnendienst.razor"));
        var client = File.ReadAllText(ClientFile(
            "Services", "Auftragsanlage", "AuftragsanlageClient.cs"));

        Assert.Contains("_uploadHinweis", page);
        Assert.Contains("role=\"alert\"", page);
        Assert.Contains("HinweisMeldung", page);
        Assert.Contains("HttpStatusCode.UnprocessableEntity", client);
    }

    [Fact]
    public void Vertriebsansicht_InteraktiveFelderSindImDarkmodeKlarErkennbar()
    {
        var appStyles = File.ReadAllText(ClientFile("wwwroot", "app.css"));
        var checklistStyles = File.ReadAllText(ClientFile("Components", "ChecklistItem.razor.css"));
        var checkboxStyles = File.ReadAllText(ClientFile(
            "Components", "Auftragsanlage", "SapBestaetigungen.razor.css"));

        Assert.Contains("--color-control-bg: #1b2944", appStyles);
        Assert.Contains("--color-control-border: rgba(200, 211, 217, 0.48)", appStyles);
        Assert.Contains("background: var(--color-control-bg)", checklistStyles);
        Assert.Contains("border: 1px solid var(--color-control-border)", checklistStyles);
        Assert.Contains(".aa-kommentar__input:hover:not(:disabled)", checklistStyles);
        Assert.Contains("background: var(--color-control-bg)", checkboxStyles);
        Assert.Contains("border: 1.5px solid var(--color-control-border)", checkboxStyles);
        Assert.Contains(":not(.aa-sap-bestaetigung--gesperrt):hover", checkboxStyles);
        Assert.Contains(
            ".aa-sap-bestaetigung__input:checked + .aa-sap-bestaetigung__check",
            checkboxStyles);
        Assert.Contains(
            ".aa-sap-bestaetigung--aktiv:not(.aa-sap-bestaetigung--gesperrt):hover",
            checkboxStyles);
    }

    [Fact]
    public void Header_VerlinktUnterseitenMitDerAuftragsanlageAuswahl()
    {
        var layout = File.ReadAllText(ClientFile("Layout", "MainLayout.razor"));

        Assert.Contains("Prüfung der Auftragsanlage · Angebotsprüfung", layout);
        Assert.Contains("Prüfung der Auftragsanlage · Bestätigungsabgleich", layout);
        Assert.Contains("? AppRoutes.Auftragsanlage", layout);
        Assert.Contains("href=\"@breadcrumbHref\"", layout);
    }

    [Fact]
    public void Vertriebsansicht_ZeigtAdressvergleichKompaktUntereinander()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));
        var styles = File.ReadAllText(ClientFile(
            "Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor.css"));
        var checklist = File.ReadAllText(ClientFile("Components", "ChecklistItem.razor"));

        Assert.Equal(2, Count(page, "Label=\"Adressvergleich\""));
        Assert.Equal(2, Count(page, "class=\"aa-adressvergleich\""));
        Assert.Contains("Kundenadresse:", page);
        Assert.Contains("Lieferadresse:", page);
        Assert.Contains("FormatKurzadresse", page);
        Assert.Contains("display: grid", styles);
        Assert.Contains("public RenderFragment? WertInhalt", checklist);
    }

    private static string ClientFile(params string[] parts)
    {
        var all = new[] { TestSourceDirectory(), "..", "..", "Illig-AI-Platform.Client" }
            .Concat(parts)
            .ToArray();
        return Path.GetFullPath(Path.Combine(all));
    }

    private static int Count(string value, string search)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }

    private static string TestSourceDirectory([CallerFilePath] string sourceFile = "") =>
        Path.GetDirectoryName(sourceFile)!;
}
