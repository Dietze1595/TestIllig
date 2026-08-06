using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class HomePageDesignTests
{
    [Fact]
    public void HomePage_UsesLandingHeroAndMarkerUnderlineStyling()
    {
        var homePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Pages",
            "Home.razor"));

        var cssPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Pages",
            "Home.razor.css"));

        var homeSource = File.ReadAllText(homePath);
        var cssSource = File.ReadAllText(cssPath);

        Assert.Contains("landing-hero", homeSource);
        Assert.Contains("marker-underline", homeSource);
        Assert.Contains("usecase-section", homeSource);
        Assert.Contains(".landing-hero", cssSource);
        Assert.Contains(".marker-underline", cssSource);
        Assert.Contains("linear-gradient", cssSource);
        Assert.Contains("margin-top: -1.35rem;", cssSource);
        Assert.Contains("border-radius: 0;", cssSource);
        Assert.Contains("margin-inline: -1.5rem;", cssSource);
        Assert.Contains("margin-inline: -4rem;", cssSource);
        Assert.DoesNotContain(".usecase-card-reveal:nth-child(even)", cssSource);
    }

    [Fact]
    public void HomePage_DeaktiviertUseCasesOhnePassendeRolle()
    {
        var homePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Pages",
            "Home.razor"));

        var homeSource = File.ReadAllText(homePath);

        Assert.Contains("usecase-card--disabled", homeSource);
        Assert.Contains("Keine Berechtigung", homeSource);
        Assert.Contains("HatBerechtigung(tile)", homeSource);
        Assert.Contains("AppClientRoles.PlausibilityCheck", homeSource);
        Assert.Contains("AppClientRoles.OrderCreationSales", homeSource);
        Assert.Contains("AppClientRoles.OrderCreationBackoffice", homeSource);
        Assert.Contains("AppClientRoles.Lieferantenassistent", homeSource);
        Assert.Contains("AppClientRoles.SearchSystem", homeSource);
        Assert.Contains("ProfileState.HatAdminBerechtigungen", homeSource);
        Assert.Contains("Prüfung der Auftragsanlage", homeSource);

        // Jeder Tile-Eintrag beginnt mit "new(" — Reihenfolge ist Plausibilitätsprüfung,
        // Prüfung der Auftragsanlage, Lieferantenassistent, ILLIG GPT.
        var tileBloecke = homeSource.Split("new(", StringSplitOptions.None).Skip(1).ToArray();
        Assert.Equal(4, tileBloecke.Length);

        Assert.Contains("AppClientRoles.PlausibilityCheck", tileBloecke[0]);
        Assert.Contains("AppClientRoles.OrderCreationSales", tileBloecke[1]);
        Assert.Contains("AppClientRoles.OrderCreationBackoffice", tileBloecke[1]);
        Assert.Contains("AppClientRoles.Lieferantenassistent", tileBloecke[2]);
        Assert.Contains("AppClientRoles.SearchSystem", tileBloecke[3]);
        Assert.Contains("AppRoutes.Kunden", tileBloecke[3]);
        Assert.DoesNotContain("AppRoutes.IlligGpt", tileBloecke[3]);
    }

    [Fact]
    public void UnternehmensweitesSuchsystem_OeffnetKundensucheOhneZwischenauswahl()
    {
        var clientPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Illig-AI-Platform.Client"));

        var legacyRoute = File.ReadAllText(Path.Combine(clientPath, "Pages", "IlligGpt.razor"));

        Assert.Contains("Nav.NavigateTo(AppRoutes.Kunden", legacyRoute);
        Assert.DoesNotContain("enterprise-search-intro", legacyRoute);
    }

    [Fact]
    public void Seitenrahmen_VerwendetGlobal64PixelUndStabilenStepper()
    {
        var clientPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Illig-AI-Platform.Client"));

        var layoutCss = File.ReadAllText(Path.Combine(clientPath, "Layout", "MainLayout.razor.css"));
        var appCss = File.ReadAllText(Path.Combine(clientPath, "wwwroot", "app.css"));
        var stepperCss = File.ReadAllText(Path.Combine(clientPath, "Components", "StepIndicator.razor.css"));
        var stueckliste = File.ReadAllText(Path.Combine(
            clientPath, "Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "StuecklistenUpload.razor"));

        Assert.Contains("padding-left: 4rem !important;", layoutCss);
        Assert.Contains("padding-right: 4rem !important;", layoutCss);
        Assert.Contains("max-width: none;", layoutCss);
        Assert.Contains(".sm-page", appCss);
        Assert.Contains("width: 100%;", appCss);
        Assert.Contains("padding-inline: 4rem;", appCss);
        Assert.Contains(".sm-stepper__item:last-child", stepperCss);
        Assert.Contains("flex: 0 0 auto;", stepperCss);
        Assert.Contains("su-page-head", stueckliste);
        Assert.Contains("su-stepper-wide", stueckliste);

        var stepperPosition = stueckliste.IndexOf("<StepIndicator", StringComparison.Ordinal);
        var variableContentPosition = stueckliste.IndexOf(
            "<div class=\"su-main su-panel-reserve @PdfPanelKlasse @VerlaufPanelKlasse\">",
            StringComparison.Ordinal);

        Assert.True(stepperPosition >= 0);
        Assert.True(variableContentPosition > stepperPosition);
    }
}
