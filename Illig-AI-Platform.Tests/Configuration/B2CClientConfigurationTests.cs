using System.Text.Json;
using Xunit;

namespace Illig_AI_Platform.Tests.Configuration;

public class B2CClientConfigurationTests
{
    [Fact]
    public void ClientAndApiConfiguration_UseSameB2CPolicy_AndBackendScope()
    {
        var clientAppSettingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "wwwroot",
            "appsettings.json"));

        using var clientDocument = JsonDocument.Parse(File.ReadAllText(clientAppSettingsPath));
        var clientAzureAdB2C = clientDocument.RootElement.GetProperty("AzureAdB2C");

        var clientId = clientAzureAdB2C.GetProperty("ClientId").GetString();
        var authority = clientAzureAdB2C.GetProperty("Authority").GetString();

        Assert.Equal("942c9f77-fbb9-4e2d-aa53-c9ee1e9ef845", clientId);
        Assert.Equal(
            "https://illigaiplatform.b2clogin.com/illigaiplatform.onmicrosoft.com/B2C_1A_IlligMultiTenantSignUpSignIn",
            authority);

        var serverAppSettingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform",
            "appsettings.json"));

        using var serverDocument = JsonDocument.Parse(File.ReadAllText(serverAppSettingsPath));
        var serverAzureAdB2C = serverDocument.RootElement.GetProperty("AzureAdB2C");

        Assert.Equal(
            "a2ea605c-8444-4ce3-aab0-b1fce0e11881",
            serverAzureAdB2C.GetProperty("ClientId").GetString());
        Assert.Equal(
            "B2C_1A_IlligMultiTenantSignUpSignIn",
            serverAzureAdB2C.GetProperty("SignUpSignInPolicyId").GetString());

        var appConfigPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "AppConfig.cs"));
        var appConfigSource = File.ReadAllText(appConfigPath);

        Assert.Contains(
            "\"https://illigaiplatform.onmicrosoft.com/a2ea605c-8444-4ce3-aab0-b1fce0e11881/get_Access\"",
            appConfigSource);
    }
}
