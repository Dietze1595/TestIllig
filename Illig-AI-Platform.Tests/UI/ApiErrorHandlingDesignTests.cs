using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class ApiErrorHandlingDesignTests
{
    private static string ClientFile(params string[] parts)
    {
        var all = new[] { AppContext.BaseDirectory, "..", "..", "..", "..", "Illig-AI-Platform.Client" }
            .Concat(parts).ToArray();
        return Path.GetFullPath(Path.Combine(all));
    }

    [Fact]
    public void ApiFehlerHinweis_HasMessageParameterAndRetryButton()
    {
        var component = File.ReadAllText(
            ClientFile("Components", "ApiFehlerHinweis.razor"));

        Assert.Contains("[Parameter, EditorRequired] public string Message", component);
        Assert.Contains("[Parameter, EditorRequired] public EventCallback OnRetry", component);
        Assert.Contains("Erneut versuchen", component);
    }

    [Fact]
    public void SondermerkmalePage_HandlesApiErrorsWithRetry()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Sondermerkmale.razor"));

        Assert.Contains("ApiRequestException", page);
        Assert.Contains("<ApiFehlerHinweis", page);
        Assert.Contains("OnRetry=\"LoadAsync\"", page);
    }

    [Fact]
    public void ReferenztrefferPage_HandlesApiErrorsWithRetry()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Sondermerkmalsuche", "Referenztreffer.razor"));

        Assert.Contains("ApiRequestException", page);
        Assert.Contains("<ApiFehlerHinweis", page);
        Assert.Contains("OnRetry=\"LoadAsync\"", page);
    }

    [Fact]
    public void AuftragsdetailsPage_HandlesApiErrorsWithRetry()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Plausibilitaetspruefung", "Stuecklistenpruefung", "Auftragsdetails.razor"));

        Assert.Contains("ApiRequestException", page);
        Assert.Contains("<ApiFehlerHinweis", page);
        Assert.Contains("OnRetry=\"LoadAsync\"", page);
    }

    [Fact]
    public void MainLayout_WrapsBodyInErrorBoundaryResetOnNavigation()
    {
        var layout = File.ReadAllText(ClientFile("Layout", "MainLayout.razor"));

        Assert.Contains("<ErrorBoundary @key=\"Nav.Uri\">", layout);
        Assert.Contains("@Body", layout);
        Assert.Contains("Neu laden", layout);
    }
}
