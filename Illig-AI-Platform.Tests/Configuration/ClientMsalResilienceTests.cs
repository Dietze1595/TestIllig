using Xunit;

namespace Illig_AI_Platform.Tests.Configuration;

public class ClientMsalResilienceTests
{
    [Fact]
    public void ClientProgram_UsesB2CFallbacks_WhenRuntimeConfigIsMissing()
    {
        var programPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Program.cs"));

        var programSource = File.ReadAllText(programPath);

        Assert.Contains("FallbackAuthority", programSource);
        Assert.Contains("FallbackClientId", programSource);
        Assert.Contains("auth.Authority ??=", programSource);
        Assert.Contains("auth.ClientId ??=", programSource);
        Assert.Contains("authentication/login-callback", programSource);
    }

    [Fact]
    public void ClientProgram_PersistsLogin_ViaLocalStorageTokenCache()
    {
        var programPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Program.cs"));

        var programSource = File.ReadAllText(programPath);

        // localStorage überlebt Reloads/Tab-Wechsel, sodass der zwischengespeicherte Login nicht
        // bei jedem Aufruf verloren geht (Standard wäre das flüchtige sessionStorage).
        Assert.Contains("CacheLocation = \"localStorage\"", programSource);
    }

    [Fact]
    public void ClientProject_ExplicitlyPublishesAppsettingsFiles()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Illig-AI-Platform.Client.csproj"));

        var projectSource = File.ReadAllText(projectPath);

        Assert.Contains("wwwroot\\appsettings.json", projectSource);
        Assert.Contains("wwwroot\\appsettings.Development.json", projectSource);
        Assert.Contains("<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>", projectSource);
    }
}
