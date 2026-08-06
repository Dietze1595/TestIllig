using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class StuecklistenpruefungPdfDesignTests
{
    [Fact]
    public void DuplicateUpload_OffersStoredStateOrFreshAnalysis()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));
        var client = File.ReadAllText(ClientFile(
            "Services", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenpruefungClient.cs"));

        Assert.Contains("Auftrag @_ergebnis.Auftragsnummer ist bereits vorhanden", page);
        Assert.Contains("Dokument neu auslesen", page);
        Assert.Contains("Gespeicherten Stand öffnen", page);
        Assert.Contains("DokumentNeuEinlesenAsync", page);
        Assert.Contains("BestehendenStandOeffnenAsync", page);
        Assert.Contains("VerlaufNeuEinlesenAsync", page);
        Assert.Contains("/neu-einlesen", client);
    }

    [Fact]
    public void UploadPage_BindsOrderInformationPdfToViewer()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));

        Assert.Contains("DokumentName=\"@_dokumentName\" DataUrl=\"@_pdfDataUrl\"", page);
    }

    [Fact]
    public void HistoryEntry_ShowsProgressWhileDetailLoads()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));
        var sidebar = File.ReadAllText(ClientFile(
            "Components", "Plausibilitaetspruefung", "Stuecklistenpruefung", "VerlaufSidebar.razor"));
        var sidebarCss = File.ReadAllText(ClientFile(
            "Components", "Plausibilitaetspruefung", "Stuecklistenpruefung", "VerlaufSidebar.razor.css"));

        Assert.Contains("LadenderId=\"_ladenderVerlaufId\"", page);
        Assert.Contains("_ladenderVerlaufId = eintrag.Id", page);
        Assert.Contains("await InvokeAsync(StateHasChanged)", page);
        Assert.Contains("finally", page);
        Assert.Contains("_ladenderVerlaufId = null", page);
        Assert.Contains("aria-busy=\"@wirdGeladen\"", sidebar);
        Assert.Contains("disabled=\"@(LadenderId.HasValue || LadendeSharePointId.HasValue)\"", sidebar);
        Assert.Contains("su-verlauf__ladebalken", sidebar);
        Assert.Contains("role=\"progressbar\"", sidebar);
        Assert.Contains("@keyframes su-verlauf-laden", sidebarCss);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", sidebarCss);
    }

    [Fact]
    public void ReviewAndSapComparison_UseSharedCardBackground()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));
        var css = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor.css"));
        var appCss = File.ReadAllText(ClientFile("wwwroot", "app.css"));

        Assert.Contains("class=\"su-order-overview sm-card", page);
        Assert.Contains("Auftragsinformation von", page);
        Assert.Contains(".su-order-overview__head h2", css);
        Assert.Contains("font-size: 1.3rem", css);
        Assert.Contains("su-order-overview__icon", page);
        Assert.Contains(".su-order-overview__icon", css);
        Assert.Contains("stroke: currentColor", css);
        Assert.Contains("class=\"su-review__merkmale su-step-content sm-card\"", page);
        Assert.Contains("Ausgelesene Merkmale", page);
        Assert.Contains(".su-review__content", css);
        Assert.Contains("gap: 1rem", css);
        Assert.Contains("sm-stuecklisten-merkmale-page", page);
        Assert.Contains(".sm-stuecklisten-merkmale-page .su-review__merkmale", css);
        Assert.Contains("overflow-y: auto", css);
        Assert.Contains("body:has(.sm-stuecklisten-merkmale-page)", appCss);
        Assert.Contains(".sm-stuecklisten-vergleich-page .su-review__merkmale", css);
        Assert.Contains("min-width: 0", css);
        Assert.Contains(
            ".sm-stuecklisten-vergleich-page {\n    display: flex;",
            css.Replace("\r\n", "\n"));
        Assert.Contains("overflow: visible", css);
    }

    [Fact]
    public void SapUploadStep_KeepsOrderHeaderWithoutBuildButton()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));

        Assert.Contains("@if (ZeigeAuftragskopf)", page);
        Assert.Contains(
            "ZeigeAuftragskopf => _ergebnis is not null && _angezeigterSchritt is >= 2 and <= 4",
            page);
        Assert.Contains("data-step=\"@_angezeigterSchritt\"", page);
        Assert.Contains("su-order-overview--compact", page);
        Assert.Contains("su-order-overview__build-action--hidden", page);
        Assert.Contains(
            "disabled=\"@(_angezeigterSchritt != 2 || _aufbauend || !_umsetzungsmatrixVerfuegbar)\"",
            page);
        Assert.Contains("@key=\"_angezeigterSchritt\"", page);
        Assert.Contains("_angezeigterSchritt == 4 ? \"sm-stuecklisten-vergleich-page\"", page);

        var headerPosition = page.IndexOf("@if (ZeigeAuftragskopf)", StringComparison.Ordinal);
        var buildButtonPosition = page.IndexOf(
            "SAP-Stückliste vergleichen",
            headerPosition,
            StringComparison.Ordinal);
        var sapStepPosition = page.IndexOf("case 3:", StringComparison.Ordinal);

        Assert.True(headerPosition >= 0);
        Assert.True(buildButtonPosition > headerPosition);
        Assert.True(sapStepPosition > buildButtonPosition);

        var css = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor.css"));
        Assert.Contains(".su-order-overview--compact", css);
        Assert.Contains("transition: padding 0.28s ease", css);
        Assert.Contains("padding-block: 0.55rem", css);
        Assert.Contains("margin-bottom: 0.125rem", css);
        Assert.Contains(".su-order-overview__build-action--hidden", css);
        Assert.Contains("max-height: 0", css);
        Assert.Contains(".su-order-overview__build-action .btn", css);
        Assert.Contains("white-space: nowrap", css);
        Assert.Contains("@keyframes su-step-content-enter", css);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", css);
    }

    [Fact]
    public void Stepper_RespondsToOpenPanelsButIgnoresCollapsedPdfButton()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));
        var css = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor.css"));
        var stepperCss = File.ReadAllText(ClientFile("Components", "StepIndicator.razor.css"));
        var resizeScript = File.ReadAllText(ClientFile("wwwroot", "pdfResize.js"));

        Assert.Contains(
            "su-page-head su-panel-reserve @PdfPanelKlasse @VerlaufPanelKlasse",
            page);
        Assert.Contains("su-main su-panel-reserve @PdfPanelKlasse @VerlaufPanelKlasse", page);
        Assert.Contains(".su-main {", css);
        Assert.Contains("--su-panel-gap: 4rem", css);
        Assert.Contains("padding-right: var(--su-panel-gap)", css);
        Assert.Contains(
            "padding-right: calc(min(30vw, 520px) + var(--su-panel-gap, 0rem))",
            css);
        Assert.DoesNotContain("width: calc(100% + 8rem)", css);
        Assert.DoesNotContain("margin-inline: -4rem", css);
        Assert.Contains("width: 100%", css);
        Assert.Contains("max-width: 100%", css);
        Assert.Contains("box-sizing: border-box", css);
        Assert.Contains("max-width: 100%", css);
        Assert.Contains(
            ".su-main--pdf-eingeklappt {\n  padding-right: var(--su-panel-gap, 0rem);",
            css.Replace("\r\n", "\n"));
        Assert.Contains("var(--su-panel-gap, 0px)", resizeScript);
        Assert.Contains("flex: 1 1 0; min-width: 0", stepperCss);
        Assert.Contains(".sm-stepper__item:last-child", stepperCss);
        Assert.Contains("flex: 0 0 auto", stepperCss);
        Assert.Contains("querySelectorAll(reserveSelector)", resizeScript);
        Assert.DoesNotContain(
            "transition: padding-right 0.2s ease, padding-left 0.2s ease",
            css);
        Assert.DoesNotContain(
            "transition: width 0.2s ease",
            File.ReadAllText(ClientFile(
                "Components", "Plausibilitaetspruefung", "Stuecklistenpruefung", "PdfViewerPanel.razor.css")));
    }

    [Fact]
    public void FeatureDescriptions_AreCollapsedAndCanBeExpanded()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));
        var css = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor.css"));

        Assert.Contains("BeschreibungVorschauSchwelle", page);
        Assert.Contains("su-merkmal-description__text--collapsed", page);
        Assert.Contains("su-merkmal-description--collapsed", page);
        Assert.Contains("BeschreibungUmschalten", page);
        Assert.Contains("aria-expanded=\"@istAusgeklappt\"", page);
        Assert.Contains("istAusgeklappt ? \"Weniger\" : \"Mehr\"", page);
        Assert.Contains(".su-merkmal-description__text--collapsed", css);
        Assert.Contains("-webkit-line-clamp: 2", css);
        Assert.Contains(".su-merkmal-description--collapsed .su-merkmal-description__toggle", css);
        Assert.Contains("right: 0", css);
        Assert.Contains("width: 50%", css);
        Assert.Contains("background: linear-gradient(", css);
        Assert.Contains("class=\"sm-merkmal__remove\"", page);
        Assert.Contains("M6.5 7l1 13h9l1-13", page);
        Assert.Contains(".sm-merkmal__remove svg", css);
        Assert.DoesNotContain(">Entfernen</button>", page);
        Assert.Contains("class=\"su-merkmal-table-wrap\"", page);
        Assert.Contains("sm-merkmal-table su-merkmal-table", page);
        Assert.Contains(".su-merkmal-table th", css);
        Assert.Contains("background: color-mix(in srgb, #00457b 8%, var(--color-bg-panel))", css);
        Assert.Contains("border-collapse: separate", css);
        Assert.Contains(".su-merkmal-table__number", css);
        Assert.Contains("su-feature-section--standard", page);
        Assert.Contains("su-feature-section--special", page);
        Assert.DoesNotContain("href=\"#su-sondermerkmale\"", page);
        Assert.Contains("id=\"su-sondermerkmale\"", page);
        Assert.Contains("Zu den Sondermerkmalen", page);
        Assert.Contains("@onclick=\"ZuSondermerkmalenScrollenAsync\"", page);
        Assert.Contains(".su-feature-jump:focus:not(:focus-visible)", css);
        Assert.Contains(".su-feature-jump:focus-visible", css);
        Assert.Contains("box-shadow: none", css);
        Assert.Contains("class=\"su-review__merkmale-scroll\"", page);
        Assert.Contains(".su-feature-section__head", css);
        Assert.Contains("border-left-color: #f7a600", css);
        Assert.Contains("position: sticky", css);
        Assert.Contains("top: 0", css);
        Assert.Contains("--su-feature-section-head-height: 4rem", css);
        Assert.Contains("top: calc(var(--su-feature-section-head-height) - 1px)", css);
        Assert.Contains("overflow: visible", css);
        Assert.Contains(".sm-stuecklisten-merkmale-page .su-merkmal-table-wrap", css);
        Assert.Contains("overflow-x: visible", css);
        Assert.Contains(".su-review__merkmale-scroll", css);
        Assert.Contains("scrollbar-gutter: stable", css);
        Assert.Contains("\"import\"", page);
        Assert.Contains("./pageScroll.js?v=1", page);
        Assert.Contains("export function toId", File.ReadAllText(ClientFile("wwwroot", "pageScroll.js")));
        Assert.DoesNotContain("window.suPageScroll", File.ReadAllText(ClientFile("wwwroot", "pdfResize.js")));
        Assert.Contains("class=\"su-feature-add\"", page);
        Assert.Contains("Weiteres Merkmal hinzufügen", page);
        Assert.Contains("@onclick=\"AddMerkmal\"", page);
        Assert.Contains(".su-feature-add__fields", css);
        Assert.Contains(".su-feature-add__actions", css);
        Assert.Contains("justify-content: flex-end", css);
        Assert.Contains("class=\"su-order-overview__head-actions\"", page);
        Assert.Contains(".su-order-overview__head-actions", css);
        Assert.Contains(
            "disabled=\"@(_angezeigterSchritt != 2 || _aufbauend || !_umsetzungsmatrixVerfuegbar)\"",
            page);
        Assert.Contains("Für diesen Maschinentyp ist keine Umsetzungsmatrix hinterlegt.", page);
        Assert.Contains("su-build-button--unavailable", page);
        Assert.Contains(".su-build-button--unavailable:disabled", css);
        Assert.Contains("Vorhandene Umsetzungsmatrizen", page);
        Assert.Contains("_verfuegbareUmsetzungsmatrizen", page);
        Assert.Contains("class=\"su-matrix-tooltip\"", page);
        Assert.Contains("role=\"tooltip\"", page);
        Assert.Contains("z-index: 100", css);
        Assert.Contains(
            ".sm-stuecklisten-merkmale-page .su-order-overview {\n  z-index: 30;",
            css.Replace("\r\n", "\n"));
        Assert.Contains(
            ".su-step-content {\n  position: relative;\n  z-index: 1;",
            css.Replace("\r\n", "\n"));
        Assert.Contains("private string? MatrixTooltipId", page);
        Assert.Contains("_umsetzungsmatrixVerfuegbar ? null : \"su-available-matrices\"", page);
        Assert.Contains("white-space: nowrap", css);
        Assert.Contains(".su-order-overview__build-action:hover .su-matrix-tooltip", css);
        Assert.Contains("max-height: 14rem", css);

        var orderOverviewPosition = page.IndexOf("class=\"su-order-overview sm-card", StringComparison.Ordinal);
        var nextStepButtonPosition = page.IndexOf("@onclick=\"AufbauenAsync\"", StringComparison.Ordinal);
        var featureCardPosition = page.IndexOf(
            "class=\"su-review__merkmale su-step-content sm-card\"",
            StringComparison.Ordinal);
        Assert.True(orderOverviewPosition >= 0);
        Assert.True(nextStepButtonPosition > orderOverviewPosition);
        Assert.True(featureCardPosition > nextStepButtonPosition);
    }

    [Fact]
    public void SapListBuild_OpensNextStepAndStartsComparisonWhenBothFilesAreReady()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));
        var css = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor.css"));

        Assert.DoesNotContain("Stückliste wird aufgebaut", page);
        Assert.DoesNotContain("Die Umsetzungsmatrix wird im Hintergrund verarbeitet.", page);
        Assert.Contains("_angezeigterSchritt == 4 ? \"sm-stuecklisten-vergleich-page\"", page);
        Assert.Contains("else if (_aufbauend || _stueckliste is not null)", page);
        Assert.Contains("_wartendeSapDatei = ms.ToArray()", page);
        Assert.Contains("SAP-Stückliste hochgeladen", page);
        Assert.Contains("VergleichStartenWennBereitAsync", page);
        Assert.Contains("Vergleich wird durchgeführt", page);
        Assert.DoesNotContain("@onclick=\"VergleichStarten", page);
        Assert.DoesNotContain("Vergleich starten", page);
        Assert.Contains(".su-sap-ready", css);
        Assert.Contains("_ = StuecklisteImHintergrundAufbauenAsync", page);
        Assert.Contains("await InvokeAsync(StateHasChanged)", page);
        Assert.Contains("vorgangId != _aufbauVorgangId", page);
        Assert.Contains("AufbauVorgangVerwerfen();", page);

        var buildHandlerPosition = page.IndexOf("private Task AufbauenAsync()", StringComparison.Ordinal);
        var nextStepPosition = page.IndexOf("_angezeigterSchritt = 3;", buildHandlerPosition, StringComparison.Ordinal);
        var backgroundStartPosition = page.IndexOf(
            "_ = StuecklisteImHintergrundAufbauenAsync",
            buildHandlerPosition,
            StringComparison.Ordinal);
        var serverWaitPosition = page.IndexOf(
            "await Client.AufbauenAsync",
            backgroundStartPosition,
            StringComparison.Ordinal);
        var sapUploadHandlerPosition = page.IndexOf(
            "private async Task OnSapDateiSelectedAsync",
            backgroundStartPosition,
            StringComparison.Ordinal);
        var backgroundHandler = page[backgroundStartPosition..sapUploadHandlerPosition];

        Assert.True(buildHandlerPosition >= 0);
        Assert.True(nextStepPosition > buildHandlerPosition);
        Assert.True(backgroundStartPosition > nextStepPosition);
        Assert.True(serverWaitPosition > backgroundStartPosition);
        Assert.Contains("VergleichStartenWennBereitAsync", backgroundHandler);
    }

    [Fact]
    public void ComparisonStep_FiltersByStatusColumnDropdown()
    {
        var page = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));
        var pageCss = File.ReadAllText(ClientFile(
            "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor.css"));
        var appCss = File.ReadAllText(ClientFile("wwwroot", "app.css"));
        var component = File.ReadAllText(ClientFile(
            "Components", "Plausibilitaetspruefung", "Stuecklistenpruefung", "VergleichsErgebnisTabelle.razor"));
        var componentCss = File.ReadAllText(ClientFile(
            "Components", "Plausibilitaetspruefung", "Stuecklistenpruefung", "VergleichsErgebnisTabelle.razor.css"));
        var row = File.ReadAllText(ClientFile(
            "Components", "Plausibilitaetspruefung", "Stuecklistenpruefung", "VergleichsBaumZeile.razor"));
        var viewModel = File.ReadAllText(ClientFile(
            "Models", "Plausibilitaetspruefung", "Stuecklistenpruefung", "VergleichsKnotenAnsicht.cs"));

        // Filter sitzt jetzt als Mehrfachauswahl-Chip im Status-Spaltenkopf (wie Lieferanten-Assist).
        Assert.Contains("<SpaltenFilterDropdown Titel=\"Status\"", component);
        Assert.Contains("Optionen=\"_statusFilterOptionen\"", component);
        Assert.Contains("Ausgewaehlt=\"_ausgewaehlteStatus\"", component);
        Assert.Contains("OnOptionUmgeschaltet=\"StatusFilterUmschalten\"", component);
        Assert.DoesNotContain("su-comparison-filter__button", component);
        Assert.DoesNotContain("enum VergleichsFilter", component);
        Assert.Contains("Nur in SAP vorhanden", component);
        Assert.Contains("AktiverBaumFilter", component);
        Assert.Contains("SichtbareZeilen", component);
        Assert.Contains("Keine Positionen mit dem gewählten Status.", component);
        Assert.Contains("Keine zusätzlichen Positionen in SAP vorhanden.", component);
        Assert.Contains("data-tree-depth=\"@Zeile.Tiefe\"", row);
        Assert.Contains("data-tree-group=\"@Zeile.HatSichtbareKinder", row);
        Assert.Contains("su-vergleich-zeile--gruppenpfad", row);
        Assert.Contains("EnthaeltStatus", viewModel);
        Assert.Contains("EnthaeltAbweichung", viewModel);
        Assert.Contains("overflow-x: auto", componentCss);
        Assert.Contains("sm-stuecklisten-vergleich-page", page);
        Assert.Contains(".sm-stuecklisten-vergleich-page .su-review__merkmale", pageCss);
        Assert.Contains("grid-template-rows: auto minmax(0, 1fr)", pageCss);
        Assert.Contains("height: 100%", pageCss);
        Assert.Contains("class=\"su-comparison-scroll\"", component);
        Assert.Contains(".su-comparison-scroll", componentCss);
        Assert.Contains("grid-template-rows: auto minmax(0, 1fr)", componentCss);
        Assert.Contains("overflow: auto", componentCss);
        Assert.Contains(".su-comparison-scroll .sm-merkmal-table th", componentCss);
        Assert.Contains("--su-comparison-table-head-height", componentCss);
        Assert.Contains("height: var(--su-comparison-table-head-height)", componentCss);
        Assert.Contains("top: 0", componentCss);
        Assert.Contains("position: sticky", componentCss);
        Assert.Contains("class=\"su-comparison-sticky-path\"", component);
        Assert.Contains("data-comparison-tree-table", component);
        Assert.Contains("<Virtualize", component);
        Assert.Contains("StickyPfadeVorberechnen", component);
        Assert.Contains("ItemSize=\"48\"", component);
        Assert.Contains("OverscanCount=\"2\"", component);
        Assert.Contains("Breadcrumb=\"@BreadcrumbFuer(zeile)\"", component);
        Assert.Contains("GruppenBreadcrumb=\"@GruppenBreadcrumbFuer(zeile)\"", component);
        Assert.Contains("./comparisonBreadcrumb.js?v=2", component);
        Assert.Contains("zeile.HatSichtbareKinder && zeile.Tiefe > 0", component);
        Assert.Contains("!zeile.HatSichtbareKinder || zeile.Tiefe == 0", component);
        Assert.DoesNotContain("StickyPfadAktualisieren", component);
        Assert.DoesNotContain("for (var i = ersterSichtbarerIndex - 1", component);
        Assert.Contains("data-comparison-breadcrumb=\"@Breadcrumb\"", row);
        Assert.Contains("data-comparison-group-breadcrumb=\"@GruppenBreadcrumb\"", row);
        Assert.Contains("class=\"su-vergleich-cell", row);
        Assert.Contains("text-overflow: ellipsis", File.ReadAllText(ClientFile(
            "Components", "Plausibilitaetspruefung", "Stuecklistenpruefung", "VergleichsBaumZeile.razor.css")));
        var breadcrumbScript = File.ReadAllText(ClientFile("wwwroot", "comparisonBreadcrumb.js"));
        Assert.Contains("requestAnimationFrame", breadcrumbScript);
        Assert.Contains("MutationObserver", breadcrumbScript);
        Assert.Contains("rowRect.top <= collisionBottom", breadcrumbScript);
        Assert.Contains("touchedGroupBreadcrumb", breadcrumbScript);
        Assert.Contains(
            ".su-vergleich-zeile--gruppenpfad > td",
            File.ReadAllText(ClientFile(
                "Components", "Plausibilitaetspruefung", "Stuecklistenpruefung", "VergleichsBaumZeile.razor.css")));
        Assert.Contains("body:has(.sm-stuecklisten-vergleich-page)", appCss);
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
