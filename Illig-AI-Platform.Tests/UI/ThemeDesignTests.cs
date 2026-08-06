using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class ThemeDesignTests
{
    [Fact]
    public void App_BietetGlobalenPersistiertenLightUndDarkMode()
    {
        var loginDisplay = File.ReadAllText(ClientFile("Layout", "LoginDisplay.razor"));
        var loginDisplayCss = File.ReadAllText(ClientFile("Layout", "LoginDisplay.razor.css"));
        var mainLayout = File.ReadAllText(ClientFile("Layout", "MainLayout.razor"));
        var mainLayoutCss = File.ReadAllText(ClientFile("Layout", "MainLayout.razor.css"));
        var appCss = File.ReadAllText(ClientFile("wwwroot", "app.css"));
        var homeCss = File.ReadAllText(ClientFile("Pages", "Home.razor.css"));
        var fileDropzoneCss = File.ReadAllText(ClientFile("Components", "FileDropzone.razor.css"));
        var index = File.ReadAllText(ClientFile("wwwroot", "index.html"));
        var themeScript = File.ReadAllText(ClientFile("wwwroot", "theme.js"));

        Assert.Contains("Light Mode", loginDisplay);
        Assert.Contains("Dark Mode", loginDisplay);
        Assert.Contains("illigTheme.toggle", loginDisplay);
        Assert.Contains("html[data-bs-theme=\"light\"] .profile-action--danger", loginDisplayCss);
        Assert.Contains(":root[data-bs-theme=\"light\"]", appCss);
        Assert.Contains("--color-marker-start: rgba(131, 201, 240, 0.68)", appCss);
        Assert.Contains("--color-marker-end: rgba(200, 232, 251, 0.94)", appCss);
        Assert.Contains("var(--color-marker-start)", homeCss);
        Assert.Contains("html[data-bs-theme=\"light\"] .landing-hero", homeCss);
        Assert.Contains("html[data-bs-theme=\"light\"] .landing-hero__overlay", homeCss);
        Assert.Contains("html[data-bs-theme=\"light\"] .su-dropzone", fileDropzoneCss);
        Assert.Contains("html[data-bs-theme=\"light\"] .su-dropzone--dragging", fileDropzoneCss);
        Assert.Contains("html[data-bs-theme=\"light\"] .su-dropzone__title", fileDropzoneCss);
        Assert.Contains("images/ILLIG_Maschinenbau_light.png", mainLayout);
        Assert.Contains("html[data-bs-theme=\"light\"] .top-row__logo--light", mainLayoutCss);
        Assert.Contains("<script src=\"theme.js\"></script>", index);
        Assert.True(
            index.IndexOf("theme.js", StringComparison.Ordinal) <
            index.IndexOf("app.css", StringComparison.Ordinal));
        Assert.Contains("window.localStorage.setItem", themeScript);
        Assert.Contains("prefers-color-scheme: light", themeScript);
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
