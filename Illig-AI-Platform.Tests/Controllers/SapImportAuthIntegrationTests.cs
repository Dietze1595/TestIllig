using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.SapImport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Illig_AI_Platform.Tests.Controllers;

/// <summary>
/// End-to-end Tests für <see cref="Illig_AI_Platform.Services.ApiKeyAuthenticationHandler"/>,
/// die über einen echten <see cref="WebApplicationFactory{TEntryPoint}"/>-Testserver laufen
/// (inkl. UseAuthentication()/UseAuthorization()-Middleware). Läuft komplett offline: Azure
/// Key Vault wird per Konfiguration deaktiviert, MySQL wird durch EF Core InMemory ersetzt.
/// </summary>
public class SapImportAuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string ConfiguredApiKey = "integration-test-key";
    private const string RequestUri = "/api/v1/sap-import/plausibilitaetspruefung/stuecklisten";
    private const string KundenPartneradressenUri = "/api/v1/sap-import/kunden/partneradressen";

    private static readonly string[] OverriddenEnvVars =
        ["KeyVaults__General", "Db__ConnectionString", "SapIntegration__ApiKey", "ApplicationInsights__ConnectionString"];

    private readonly Dictionary<string, string?> _originalEnvVars = [];
    private readonly WebApplicationFactory<Program> _factory;

    public SapImportAuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Program.cs liest die Konfiguration bereits in den Top-Level-Statements aus
        // (z. B. für KeyVaults:General und in AddSharedServices für Db:ConnectionString),
        // also lange bevor WithWebHostBuilder(...).ConfigureAppConfiguration(...) greifen
        // würde (dieser Hook wirkt erst auf den deferred Host-Builder, nicht auf den
        // WebApplicationBuilder, der im Entry Point synchron ausgeführt wird). Deshalb
        // werden die Overrides hier als Umgebungsvariablen gesetzt: Diese liest
        // WebApplication.CreateBuilder(args) als Standard-Konfigurationsquelle bereits beim
        // Aufbau von builder.Configuration selbst ein.
        //
        // Umgebungsvariablen sind prozessweit sichtbar — damit sie keine anderen, parallel
        // laufenden Testklassen beeinflussen (xUnit führt Testklassen standardmäßig parallel
        // aus), werden die ursprünglichen Werte gesichert und in Dispose() wiederhergestellt.
        foreach (var name in OverriddenEnvVars)
            _originalEnvVars[name] = Environment.GetEnvironmentVariable(name);

        Environment.SetEnvironmentVariable("KeyVaults__General", "");
        Environment.SetEnvironmentVariable(
            "Db__ConnectionString",
            "server=localhost;database=placeholder;user=root;password=placeholder;");
        Environment.SetEnvironmentVariable("SapIntegration__ApiKey", ConfiguredApiKey);
        // Verhindert, dass die Tests echte Telemetrie an Azure senden (Program.cs
        // registriert App Insights nur bei gesetztem Connection String).
        Environment.SetEnvironmentVariable("ApplicationInsights__ConnectionString", "");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Die Pomelo/MySql-Registrierung aus AddSharedServices vollständig entfernen
                // (nicht nur DbContextOptions<AppDbContext>, sondern auch die zugehörigen
                // DbContextOptions<AppDbContext>-Konfigurationsbausteine), sonst meldet EF Core
                // "Only a single database provider can be registered", weil sowohl Pomelo als
                // auch InMemory gleichzeitig registriert wären.
                var descriptors = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        d.ServiceType == typeof(DbContextOptions) ||
                        d.ServiceType == typeof(AppDbContext) ||
                        (d.ServiceType.IsGenericType &&
                         d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>) &&
                         d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext)))
                    .ToList();
                foreach (var descriptor in descriptors)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("sap-import-integration-tests"));
            });
        });
    }

    public void Dispose()
    {
        foreach (var (name, value) in _originalEnvVars)
            Environment.SetEnvironmentVariable(name, value);
        GC.SuppressFinalize(this);
    }

    private static object ValidRows() => new
    {
        bomType = "ORDER_BOM",
        orderNumber = "A100",
        orderItem = "10",
        validAt = "2026-07-08",
        rootNodeId = "root",
        nodes = new[]
        {
            new
            {
                nodeId = "root", parentNodeId = (string?)null, position = (string?)null,
                type = "ASSEMBLY", materialNumber = "ART-1", description = "Schraube",
                quantity = 4, unit = "ST"
            }
        }
    };

    private static object GueltigeKundenAdressen() => new
    {
        kunden = new[]
        {
            new
            {
                hauptkundennummer = "717216",
                adressen = new[]
                {
                    new { partnerrolle = "Auftraggeber", partnerId = "717216", name = "Malico General Trading", strasse = "PO Box No. 18257, Office 1660", plz = (string?)null, ort = "Dubai", land = "AE" },
                    new { partnerrolle = "Rechnungsempfänger", partnerId = "717216", name = "Malico General Trading", strasse = "PO Box No. 18257, Office 1660", plz = (string?)null, ort = "Dubai", land = "AE" },
                    new { partnerrolle = "Regulierer", partnerId = "717216", name = "Malico General Trading", strasse = "PO Box No. 18257, Office 1660", plz = (string?)null, ort = "Dubai", land = "AE" },
                    new { partnerrolle = "Vertretung", partnerId = "2222", name = "ILLIG Packaging solution", strasse = "Robert Bosch Straße 10", plz = (string?)"74081", ort = "Heilbronn", land = "DE" },
                    new { partnerrolle = "Warenempfänger", partnerId = "717220", name = "Al Sulaymania for the Pro", strasse = "Hurr region, Lamalliye Industrial", plz = (string?)null, ort = "Karbala Governorate", land = "IQ" },
                    new { partnerrolle = "Endkunde", partnerId = "717220", name = "Al Sulaymania for the Pro", strasse = "Hurr region, Lamalliye Industrial", plz = (string?)null, ort = "Karbala Governorate", land = "IQ" }
                }
            }
        }
    };

    [Fact]
    public async Task PostStuecklisten_ReturnsUnauthorized_WhenApiKeyHeaderMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RequestUri, ValidRows());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostStuecklisten_ReturnsUnauthorized_WhenApiKeyWrong()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.PostAsJsonAsync(RequestUri, ValidRows());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task McpEndpoint_ReturnsOAuthResourceChallenge_WhenTokenIsMissing()
    {
        var client = _factory.CreateClient();
        using var content = new StringContent(
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/mcp", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Bearer", challenge.Scheme);
        Assert.Contains("resource_metadata=", challenge.Parameter);
        Assert.Contains("/.well-known/oauth-protected-resource/mcp", challenge.Parameter);
    }

    [Fact]
    public async Task McpEndpoint_ReturnsOAuthResourceChallenge_WhenInitialProbeHasNoContentType()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new ByteArrayContent(
                Encoding.UTF8.GetBytes(
                    """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}"""))
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Bearer", challenge.Scheme);
        Assert.Contains("resource_metadata=", challenge.Parameter);
        Assert.Contains("/.well-known/oauth-protected-resource/mcp", challenge.Parameter);
    }

    [Fact]
    public async Task McpResourceMetadata_AdvertisesAuthorizationServerAndReadScope()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/.well-known/oauth-protected-resource/mcp");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Contains(
            "https://illigaiplatform.b2clogin.com/illigaiplatform.onmicrosoft.com/B2C_1A_IlligMultiTenantSignUpSignIn/v2.0",
            root.GetProperty("authorization_servers").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains(
            "https://illigaiplatform.onmicrosoft.com/a2ea605c-8444-4ce3-aab0-b1fce0e11881/get_Access",
            root.GetProperty("scopes_supported").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public async Task SwaggerDoc_ContainsVersionSubstitutedRoutes()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/sap-import-v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/v1/sap-import/plausibilitaetspruefung/stuecklisten", json);
        Assert.Contains("/api/v1/sap-import/plausibilitaetspruefung/maximalstuecklisten", json);
        Assert.Contains("/api/v1/sap-import/lieferantenassistent/dispositionsliste", json);
        Assert.Contains("/api/v1/sap-import/lieferantenassistent/lieferanten-kreditoren-stammdaten", json);
        Assert.Contains("/api/v1/sap-import/kunden/partneradressen", json);
        // Für den Kunden bewusst nur Dispositionsliste + Stammdaten sichtbar — die übrigen
        // Lieferantenassistent-Endpunkte wurden entfernt und dürfen nicht im Swagger auftauchen.
        Assert.DoesNotContain("/api/v1/sap-import/lieferantenassistent/einkaeufergruppen", json);
        Assert.DoesNotContain("/api/v1/sap-import/lieferantenassistent/warengruppen", json);
        Assert.DoesNotContain("/api/v1/sap-import/lieferantenassistent/all-open-orders", json);
        Assert.DoesNotContain("/dokumente", json);
        Assert.DoesNotContain("{version}", json);

        // Gruppierung in der Swagger-UI erfolgt pro Use Case (Tags), nicht pro Controller.
        // "Plausibilit" statt "Plausibilitätsprüfung", falls der Serializer Umlaute escapt.
        Assert.Contains("Plausibilit", json);
        Assert.Contains("Lieferantenassistent", json);
        Assert.Contains("Kunden", json);
        Assert.DoesNotContain("SapStaging", json);
        Assert.DoesNotContain("SapImport", json);

        // EndpointSummary-Attribute müssen als Beschreibung im Swagger ankommen.
        Assert.Contains("Dispositionslisten", json);
    }

    [Fact]
    public async Task PostLieferanten_ReturnsOkAndPersistsDirekt_WhenApiKeyCorrect()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ConfiguredApiKey);

        var response = await client.PostAsJsonAsync(
            "/api/v1/sap-import/lieferantenassistent/lieferanten-kreditoren-stammdaten",
            new
            {
                suppliers = new[]
                {
                    new
                    {
                        supplierNumber = "9100", name = "Muster Lieferant GmbH", country = "DE",
                        postalCode = "74076", city = "Heilbronn", street = "Musterstraße 1",
                        addressNumber = "99100",
                        contacts = new[]
                        {
                            new { email = "bestellung@muster.de", isDefault = true, contactType = "ORDER" }
                        }
                    }
                }
            });

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 OK, got {response.StatusCode}: {responseBody}");
        var body = await response.Content.ReadFromJsonAsync<SapDirektImportErgebnis>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.ZeilenAnzahl);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(db.Lieferanten, lieferant => lieferant.Kreditor == 9100);
        Assert.Contains(db.LieferantEmailAdressen, kontakt => kontakt.AdressNummer == 99100);
    }

    [Fact]
    public async Task PostStuecklisten_ReturnsOkAndPersists_WhenApiKeyCorrect()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ConfiguredApiKey);

        var response = await client.PostAsJsonAsync(RequestUri, ValidRows());

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 OK, got {response.StatusCode}: {responseBody}");
        var body = await response.Content.ReadFromJsonAsync<SapDirektImportErgebnis>();
        Assert.NotNull(body);
        Assert.Equal(1, body.ZeilenAnzahl);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(db.StuecklistenPositionen, position =>
            position.Auftragsnummer == "A100" && position.NodeId == "root");
    }

    [Fact]
    public async Task PostKundenPartneradressen_ReturnsUnauthorized_WhenApiKeyHeaderMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(KundenPartneradressenUri, GueltigeKundenAdressen());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostKundenPartneradressen_ReturnsOkAndPersists_WhenApiKeyCorrect()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ConfiguredApiKey);

        var response = await client.PostAsJsonAsync(KundenPartneradressenUri, GueltigeKundenAdressen());

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 OK, got {response.StatusCode}: {responseBody}");
        var body = await response.Content.ReadFromJsonAsync<SapDirektImportErgebnis>();
        Assert.NotNull(body);
        Assert.Equal(6, body!.ZeilenAnzahl);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(db.KundenPartneradressen, adresse =>
            adresse.Hauptkundennummer == "717216" && adresse.Partnerrolle == "Vertretung" && adresse.PartnerId == "2222");
    }
}
