using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class SondermerkmalsucheDesignTests
{
    private static string ClientFile(params string[] parts)
    {
        var all = new[] { TestSourceDirectory(), "..", "..", "Illig-AI-Platform.Client" }
            .Concat(parts).ToArray();
        return Path.GetFullPath(Path.Combine(all));
    }

    private static string TestSourceDirectory([CallerFilePath] string sourceFile = "") =>
        Path.GetDirectoryName(sourceFile)!;

    [Fact]
    public void LandingPage_HasChoiceAndRevealsSearchByModus()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Plausibilitaetspruefung.razor"));
        var css = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Plausibilitaetspruefung.razor.css"));

        Assert.Contains("@page \"/plausibilitaetspruefung\"", page);
        Assert.Contains("SupplyParameterFromQuery", page);
        Assert.Contains("modus=sondermerkmalsuche", page);
        Assert.Contains("Stücklistenprüfung", page);
        Assert.Contains("sm-choice-card", page);
        Assert.Contains("/sondermerkmale?auftragsnummer=", page);
        Assert.Contains("sm-search-card__formats", page);
        Assert.DoesNotContain("Schritt 1 · Auftrag", page);
        Assert.Contains("await Client.AnalyzeAsync(auftragsnummer)", page);
        Assert.Contains("await Client.SearchAsync", page);
        Assert.Contains("wurde in der Datenbank nicht gefunden", page);
        Assert.Contains("_checking", page);
        Assert.Contains(".sm-search-row .form-control", css);
        Assert.DoesNotContain("max-width: 64rem", css);
        Assert.Contains("min-height: 3.35rem", css);
        Assert.Contains("color: var(--color-text)", css);
        Assert.Contains("background-color: var(--color-bg-elevated)", css);
    }

    [Fact]
    public void SondermerkmalePage_ShowsInfoDraggablePositions_AndStartsSearch()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Sondermerkmale.razor"));
        var css = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Sondermerkmale.razor.css"));

        Assert.Contains("@page \"/sondermerkmale\"", page);
        Assert.Contains("SupplyParameterFromQuery", page);
        Assert.Contains("su-order-overview sm-card", page);
        Assert.Contains("Auftragsinformation von", page);
        Assert.Contains("su-order-overview__info", page);
        Assert.Contains("su-order-overview__icon", page);
        Assert.Contains("Positionen gefunden", page);
        Assert.Contains("draggable", page);
        Assert.Contains("Suchpriorität", page);
        Assert.Contains("/referenztreffer?auftragsnummer=", page);
        Assert.Contains("sm-merkmal-table-wrap", page);
        Assert.Contains("sm-merkmal-table--grouped", page);
        Assert.Contains("sm-position-group__row", page);
        Assert.Contains("Merkmal @m.Merkmalsnummer entfernen", page);
        Assert.Contains("grid-template-columns: repeat(4, minmax(0, 1fr))", css);
        Assert.Contains(".su-order-overview__head h2", css);
        Assert.Contains("font-size: 1.3rem", css);
        Assert.Contains("stroke: currentColor", css);
        Assert.Contains("flex-direction: column", css);
        Assert.Contains("width: 2.75rem", css);
        Assert.Contains("M6.5 7l1 13h9l1-13", page);
        Assert.Contains("overflow: visible", css);
        Assert.Contains("stroke-linejoin: round", css);
        Assert.Contains("border-left: 3px solid #9ca9b1", css);
        Assert.Contains("background: color-mix(in srgb, #00457b 8%, var(--color-bg-panel))", css);
        Assert.Contains("border-collapse: separate", css);
        Assert.DoesNotContain("sm-pos-card", page);
        Assert.Contains("sm-merkmal__desc-content--collapsed", page);
        Assert.Contains("aria-expanded=\"@istAusgeklappt\"", page);
        Assert.Contains("-webkit-line-clamp: 2", css);
        Assert.Contains("width: 50%", css);
        Assert.Contains("margin: .3rem 0 0 50%", css);
        Assert.Contains("background: linear-gradient(", css);
        Assert.Contains("MehrSchwellenwertZeichen = 110", page);
        Assert.Contains("PositionUmschalten", page);
        Assert.Contains("sm-position-group__toggle", page);
        Assert.Contains("aria-expanded=\"@(!gruppeIstEingeklappt)\"", page);
        Assert.Contains("_eingeklapptePositionen", page);
        Assert.Contains("position: sticky", css);
        Assert.Contains("top: 0", css);
        Assert.Contains("sm-position-group__toggle--collapsed", css);
        Assert.Contains("transform: rotate(-90deg)", css);
    }

    [Fact]
    public void ReferenztrefferPage_RunsSearch_AndNavigatesToDetails()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Referenztreffer.razor"));

        Assert.Contains("@page \"/referenztreffer\"", page);
        Assert.Contains("SupplyParameterFromQuery", page);
        Assert.Contains("SearchAsync", page);
        Assert.Contains("sm-reference-table", page);
        Assert.Contains("<table", page);
        Assert.Contains("Enthaltene Sondermerkmale", page);
        Assert.Contains("UeberschneidendeMerkmalsnummern", page);
        Assert.Contains("UeberschneidungenSortiert", page);
        Assert.Contains("/auftragsdetails?auftragsnummer=", page);
        Assert.Contains("&merkmale=", page);
        Assert.Contains("&aktuellste=", page);
        Assert.Contains("sm-results-legend", page);
        Assert.Contains("sm-result-group--found", page);
        Assert.Contains("sm-result-group--missing", page);
        Assert.Contains("In Referenzaufträgen gefunden", page);
        Assert.Contains("Nicht in Referenzaufträgen gefunden", page);
        Assert.Contains("sm-result-group__badge", page);
        Assert.Contains("GefundeneMerkmalsnummernAnzahl", page);
        Assert.Contains("in den angezeigten Referenzaufträgen gefunden", page);
        Assert.Contains("aktuellste Stückliste", page);
        Assert.Contains("sm-reference-table__customer", page);
        Assert.Contains("sm-reference-table__score", page);
        Assert.Contains("SichtbareMerkmaleProTreffer = 6", page);
        Assert.Contains("AktuellsteMerkmalsnummern.Contains", page);
        Assert.Contains("sm-reference-table__feature-toggle", page);
        Assert.Contains("+ {VerborgeneUeberschneidungen(t)} weitere", page);
        Assert.Contains("\"Mehr\"", page);
        Assert.Contains("aria-expanded", page);
        Assert.Contains("sm-merkmal-fehlend__text--eingeklappt", page);
        Assert.Contains("sm-missing-table", page);
        Assert.Contains("<th scope=\"col\">Position</th>", page);
        Assert.Contains("<th scope=\"col\">Merkmalsnummer</th>", page);

        var css = File.ReadAllText(
            ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Referenztreffer.razor.css"));
        var appCss = File.ReadAllText(ClientFile("wwwroot", "app.css"));
        Assert.Contains("sm-referenztreffer-page", page);
        Assert.Contains("body:has(.sm-referenztreffer-page)", appCss);
        Assert.Contains(".sm-referenztreffer-page > .sm-wizard-content > .sm-card", css);
        Assert.Contains("overflow-y: auto", css);
        Assert.Contains("scrollbar-gutter: stable", css);
        Assert.Contains("-webkit-line-clamp: 2", css);
        Assert.Contains("width: 50%", css);
        Assert.Contains("margin-left: 50%", css);
        Assert.Contains(".sm-missing-table th", css);
        Assert.Contains(".sm-missing-table td", css);
        Assert.Contains("#e3e9ec 18%", css);
        Assert.Contains("border-bottom: 1px solid", css);
        Assert.Contains(".sm-missing-table tbody tr:last-child td", css);
        Assert.Contains("min-width: 70rem", css);
        Assert.Contains(".sm-reference-table th", css);
        Assert.Contains(".sm-results-legend__marker", css);
        Assert.Contains(".sm-result-group--found", css);
        Assert.Contains("border-left-color: #9bc832", css);
        Assert.Contains(".sm-result-group--missing", css);
        Assert.Contains("border-left-color: #f7a600", css);
        Assert.Contains(".sm-result-group__head", css);
        Assert.Contains("class=\"sm-result-group__copy\"", page);
        Assert.Contains(".sm-result-group__copy", css);
        Assert.Contains("flex: 1 1 auto", css);
        Assert.Contains(".sm-result-group__badge", css);
        Assert.Contains("border-left: 3px solid transparent", css);
        Assert.Contains("border-radius: 0", css);
        Assert.Contains(".sm-reference-table__open", css);
        Assert.Contains(".sm-reference-table__feature-toggle", css);
        Assert.Contains("min-height: 1.75rem", css);
        Assert.Contains("justify-content: center", css);
    }

    [Fact]
    public void DetailsPage_HasRouteAndReadsQueryParam()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "Auftragsdetails.razor"));
        var css = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "Auftragsdetails.razor.css"));
        var appCss = File.ReadAllText(ClientFile("wwwroot", "app.css"));

        Assert.Contains("@page \"/auftragsdetails\"", page);
        Assert.Contains("SupplyParameterFromQuery", page);
        Assert.Contains("Merkmalsnummer", page);
        Assert.Contains("public string? Merkmale", page);
        Assert.Contains("AppRoutes.Referenztreffer", page);
        Assert.Contains("In beiden Aufträgen", page);
        Assert.Contains("Nur im Referenzauftrag", page);
        Assert.Contains("<PdfViewerPanel", page);
        Assert.Contains("GetDokumentAsync", page);
        Assert.Contains("\".sm-detail-main\"", page);
        Assert.DoesNotContain(".sm-detail-main, .content__steps", page);
        Assert.Contains("sm-reference-result-card", page);
        Assert.Contains("Prüfergebnis für", page);
        Assert.Contains("sm-reference-info__icon", page);
        Assert.Contains("Sondermerkmale des Referenzauftrags", page);
        Assert.Contains("sm-results-legend", page);
        Assert.Contains("public string? Aktuellste", page);
        Assert.Contains("AktuellsteMerkmalsnummern=\"_aktuellsteMerkmalsnummern\"", page);
        Assert.Contains("Merkmale im Referenzauftrag", page);
        Assert.DoesNotContain("<table", page);
        Assert.Contains(".sm-detail-main--pdf-offen", css);
        Assert.Contains(
            ".sm-detail-main--pdf-eingeklappt {\n    padding-right: 0;",
            css.Replace("\r\n", "\n"));
        Assert.Contains("border-top: 4px solid var(--color-accent)", css);
        Assert.Contains("border-left: 3px solid transparent", css);
        Assert.Contains(".sm-feature-group--match { border-left-color: #9bc832; }", css);
        Assert.Contains(".sm-feature-group--additional", css);
        Assert.Contains("border-left-color: #f7a600", css);
        Assert.Contains("background: rgba(155, 200, 50, .12)", css);
        Assert.Contains("background: rgba(247, 166, 0, .11)", css);
        Assert.Contains("padding: .85rem 1rem", css);
        Assert.Contains("border-radius: 1rem", css);
        Assert.Contains("background: rgba(87, 102, 109, .12)", css);
        Assert.Contains("background: color-mix(in srgb, #e3e9ec 34%, var(--color-bg-panel))", css);
        Assert.Contains("--feature-row-bg: color-mix(in srgb, #e3e9ec 18%, var(--color-bg-panel))", css);
        Assert.Contains("border-radius: 0", css);
        Assert.Contains("border-radius: .2rem", css);
        Assert.Contains("padding: .7rem .85rem", css);
        Assert.Contains(".sm-detail__number--aktuell", css);
        Assert.Contains(".sm-detail__number-chip", css);
        Assert.Contains("width: 6rem", css);
        Assert.Contains("box-sizing: border-box", css);
        Assert.Contains("background: #9bc832", css);
        Assert.Contains("min-height: 1.75rem", css);
        Assert.Contains("grid-template-columns: repeat(auto-fit, minmax(180px, 1fr))", css);
        Assert.Contains(".sm-feature-group--match", css);
        Assert.Contains(".sm-feature-group--additional", css);

        var merkmalsGruppe = File.ReadAllText(
            ClientFile("Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "MerkmalsGruppe.razor"));
        Assert.DoesNotContain("StatusText", merkmalsGruppe);
        Assert.DoesNotContain("sm-feature-status", merkmalsGruppe);
        Assert.DoesNotContain("sm-feature-group__icon", merkmalsGruppe);
        Assert.DoesNotContain(">Status</th>", merkmalsGruppe);
        Assert.Contains("<table class=\"sm-feature-table\">", merkmalsGruppe);
        Assert.Contains("<th scope=\"col\">Merkmalsnummer</th>", merkmalsGruppe);
        Assert.Contains("<th scope=\"col\">Beschreibung</th>", merkmalsGruppe);
        Assert.Contains("sm-feature-table__number", merkmalsGruppe);
        Assert.Contains("sm-feature-table__description", merkmalsGruppe);
        Assert.Contains("sm-detail__number-chip", merkmalsGruppe);
        Assert.Contains("sm-feature-description__text--eingeklappt", merkmalsGruppe);
        Assert.Contains("BeschreibungUmschalten", merkmalsGruppe);
        Assert.Contains("\"Weniger\" : \"Mehr\"", merkmalsGruppe);
        Assert.Contains(".sm-feature-description__toggle", css);
        Assert.Contains(".sm-feature-table-wrap", css);
        Assert.Contains(".sm-feature-table th", css);
        Assert.Contains(".sm-feature-table td", css);
        Assert.Contains("min-width: 42rem", css);
        Assert.Contains("padding: .55rem .75rem", css);
        Assert.Contains("padding: .7rem .75rem", css);
        Assert.Contains("background: color-mix(in srgb, #00457b 8%, var(--color-bg-panel))", css);

        Assert.Contains("padding-right: min(30vw, 520px)", css);

        // Schritt 4: Nur der Inhalt unterhalb von Titel und Stepper scrollt, die Seite selbst nicht.
        Assert.Contains("sm-auftragsdetails-page", page);
        Assert.Contains("body:has(.sm-auftragsdetails-page)", appCss);
        Assert.Contains(".sm-auftragsdetails-page .sm-wizard-content", css);
        Assert.Contains("overflow-y: auto", css);
        Assert.Contains("scrollbar-gutter: stable", css);
        Assert.Contains("margin-bottom: .5rem", css);
    }

    [Fact]
    public void StuecklistenUploadPage_GatesAccessAndShowsDropzoneAndReview()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));

        Assert.Contains("@page \"/stuecklistenpruefung\"", page);
        Assert.Contains("<RoleGuard", page);
        Assert.Contains("<FileDropzone", page);
        Assert.Contains("su-review", page);
        Assert.Contains("Sondermerkmale", page);
    }

    [Fact]
    public void Pages_RenderRouteDrivenStepperAfterTitleAndDescription()
    {
        var layout = File.ReadAllText(ClientFile("Layout", "MainLayout.razor"));
        var stepper = File.ReadAllText(ClientFile("Components", "SondermerkmalsucheStepper.razor"));
        var search = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Plausibilitaetspruefung.razor"));
        var sondermerkmale = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Sondermerkmale.razor"));
        var referenzen = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Referenztreffer.razor"));
        var details = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "Auftragsdetails.razor"));
        var appCss = File.ReadAllText(ClientFile("wwwroot", "app.css"));

        Assert.DoesNotContain("<StepIndicator", layout);
        Assert.Contains("<CascadingValue Value=\"this\"", layout);
        Assert.Contains("<StepIndicator", stepper);
        Assert.Contains("MaxErreicht=\"Layout.SonderWizardMaxStep\"", stepper);
        Assert.Contains("OnStepClick=\"SchrittWechseln\"", stepper);
        Assert.Contains("top-row__crumb", layout);
        Assert.Contains("Plausibilitätsprüfung", layout);
        Assert.Contains("Sondermerkmalsuche", layout);

        Assert.True(search.IndexOf("</header>", StringComparison.Ordinal)
                    < search.IndexOf("<SondermerkmalsucheStepper", StringComparison.Ordinal));
        Assert.True(sondermerkmale.IndexOf("<SeitenKopf", StringComparison.Ordinal)
                    < sondermerkmale.IndexOf("<SondermerkmalsucheStepper", StringComparison.Ordinal));
        Assert.True(referenzen.IndexOf("</header>", StringComparison.Ordinal)
                    < referenzen.IndexOf("<SondermerkmalsucheStepper", StringComparison.Ordinal));
        Assert.True(details.IndexOf("<SeitenKopf", StringComparison.Ordinal)
                    < details.IndexOf("<SondermerkmalsucheStepper", StringComparison.Ordinal));
        Assert.Contains(".sm-wizard-content", appCss);
        Assert.Contains("padding-inline: 4rem", appCss);
        Assert.Contains("padding-inline: 0", appCss);
        Assert.All(new[] { search, sondermerkmale, referenzen, details },
            page => Assert.True(
                page.IndexOf("<SondermerkmalsucheStepper", StringComparison.Ordinal) <
                page.IndexOf("<div class=\"sm-wizard-content\">", StringComparison.Ordinal)));
    }

    [Fact]
    public void StepIndicator_RendersBadgeAndLabelPerStep()
    {
        var comp = File.ReadAllText(ClientFile("Components", "StepIndicator.razor"));
        var wrapperCss = File.ReadAllText(ClientFile("Components", "SondermerkmalsucheStepper.razor.css"));

        Assert.Contains("sm-stepper__badge", comp);
        Assert.Contains("sm-stepper__label", comp);
        Assert.Contains("Current", comp);
        Assert.Contains("box-sizing: border-box", wrapperCss);
        Assert.Contains("min-width: 0", wrapperCss);
        Assert.DoesNotContain(".sm-stepper__label {\n        display: none", wrapperCss);
    }

    [Fact]
    public void AppCss_DefinesSharedChoiceAndReviewStyles()
    {
        var css = File.ReadAllText(ClientFile("wwwroot", "app.css"));
        var auftragsanlage = File.ReadAllText(
            ClientFile("Pages", "Auftragsanlage", "Auftragsanlage.razor"));
        var plausibilitaetspruefung = File.ReadAllText(
            ClientFile("Pages", "Plausibilitaetspruefung", "Plausibilitaetspruefung.razor"));

        // Shared across mehrere Use Cases — bleiben in der globalen app.css (siehe README.md).
        Assert.Contains(".sm-choice-card", css);
        Assert.Contains(".sm-choice-card__cta::before", css);
        Assert.Contains(".sm-choice-card:hover .sm-choice-card__cta::before", css);
        Assert.Contains(".sm-choice-card:focus-visible .sm-choice-card__cta::before", css);
        Assert.Contains("body:has(.sm-choice-page)", css);
        Assert.Contains("height: 100dvh", css);
        Assert.Contains("overflow: hidden", css);
        Assert.Contains("justify-content: center", css);
        Assert.Contains("sm-choice-page", auftragsanlage);
        Assert.Contains("sm-choice-page", plausibilitaetspruefung);
        Assert.Contains(".su-review", css);
    }

    [Fact]
    public void ScopedCss_DefinesStepperResultAndDropzoneStylesNextToTheirComponent()
    {
        var stepIndicatorCss = File.ReadAllText(ClientFile("Components", "StepIndicator.razor.css"));
        var referenztrefferCss = File.ReadAllText(
            ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Referenztreffer.razor.css"));
        var fileDropzoneCss = File.ReadAllText(ClientFile("Components", "FileDropzone.razor.css"));

        Assert.Contains(".sm-stepper", stepIndicatorCss);
        Assert.Contains(".sm-reference-table", referenztrefferCss);
        Assert.Contains(".sm-reference-table__feature", referenztrefferCss);
        Assert.Contains(".su-dropzone", fileDropzoneCss);
    }

    [Fact]
    public void Pages_GatePermissionBeforeAccess()
    {
        var search = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Plausibilitaetspruefung.razor"));
        var sonder = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Sondermerkmale.razor"));
        var treffer = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Referenztreffer.razor"));
        var detail = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "Auftragsdetails.razor"));
        var upload = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));

        foreach (var page in new[] { search, sonder, treffer, detail, upload })
        {
            Assert.Contains("<RoleGuard", page);
        }
    }
}
