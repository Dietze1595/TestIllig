using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class MainLayoutShellTests
{
    [Fact]
    public void MainLayout_UsesSingleColumnShellWithoutSidebarNavigation()
    {
        var layoutPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Layout",
            "MainLayout.razor"));

        var layoutCssPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Layout",
            "MainLayout.razor.css"));

        var layoutSource = File.ReadAllText(layoutPath);
        var layoutCssSource = File.ReadAllText(layoutCssPath);

        Assert.DoesNotContain("<NavMenu />", layoutSource);
        Assert.DoesNotContain("class=\"sidebar\"", layoutSource);
        Assert.Contains("class=\"top-row px-4\"", layoutSource);
        Assert.Contains("top-row__brand", layoutSource);
        Assert.Contains("KI-Plattform", layoutSource);
        Assert.DoesNotContain(".sidebar", layoutCssSource);
        Assert.DoesNotContain("flex-direction: row;", layoutCssSource);
        Assert.Contains("width: 100%;", layoutCssSource);
    }
}
