using Asp.Versioning;
using Azure.Core;
using Azure.Identity;
using Illig_AI_Platform.Mcp;
using Illig_AI_Platform.Middleware;
using Illig_AI_Platform.Services;
using Illig_AI_Platform.Services.Auftragsinformationen;
using Illig_AI_Platform.Shared;
using Illig_AI_Platform.Shared.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using ModelContextProtocol.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUrl = builder.Configuration["KeyVaults:General"];
if (!string.IsNullOrWhiteSpace(keyVaultUrl))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
    catch (Exception ex)
    {
        await Console.Error.WriteLineAsync($"KeyVault '{keyVaultUrl}' nicht erreichbar: {ex.Message}");
    }
}

var authenticationBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
authenticationBuilder.AddMicrosoftIdentityWebApi(builder.Configuration, "AzureAdB2C");
authenticationBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
    ApiKeyAuthenticationHandler.SchemeName, _ => { });
authenticationBuilder.AddMcp(options =>
{
    var authorizationServer = builder.Configuration["Mcp:AuthorizationServer"];
    options.ResourceMetadata = new()
    {
        ResourceName = "ILLIG AI Platform Wissensdatenbank",
        AuthorizationServers = string.IsNullOrWhiteSpace(authorizationServer) ? [] : [authorizationServer],
        ScopesSupported = [McpConfiguration.ReadScope],
    };
});
authenticationBuilder.AddPolicyScheme(
    McpConfiguration.AuthenticationScheme,
    "ILLIG MCP OAuth",
    options =>
    {
        options.ForwardAuthenticate = JwtBearerDefaults.AuthenticationScheme;
        options.ForwardChallenge = McpAuthenticationDefaults.AuthenticationScheme;
        options.ForwardForbid = JwtBearerDefaults.AuthenticationScheme;
    });

builder.Services.PostConfigure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.MapInboundClaims = false;
    });

builder.Services.AddSharedServices(builder.Configuration);

builder.Services.Configure<SharePointAuftragsinformationenOptions>(
    builder.Configuration.GetSection(SharePointAuftragsinformationenOptions.SectionName));
builder.Services.AddSingleton<TokenCredential>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SharePointAuftragsinformationenOptions>>().Value;
    return SharePointTokenCredentialFactory.Create(options);
});
builder.Services.AddHttpClient<ISharePointDokumentClient, GraphSharePointDokumentClient>();
builder.Services.AddScoped<AuftragsinformationenImportService>();
builder.Services.AddScoped<AuftragsdokumentService>();
builder.Services.AddHostedService<SharePointAuftragsinformationenWorker>();

// Welche ILogger-Level nach App Insights fließen, steuert Logging:ApplicationInsights
// in appsettings.json (Standard des Providers wäre nur Warning+). Ohne Connection String
// (lokal, Integrationstests) wird die Telemetrie gar nicht erst registriert — sonst
// versucht der Kanal, Telemetrie an Azure zu senden.
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
    builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<IlligKnowledgeTools>()
    .AddAuthorizationFilters();

// Lädt die DB-Rollen des Nutzers als Role-Claims → [Authorize(Roles="…")] funktioniert.
builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();

builder.Services.AddControllers();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    // Ersetzt {version:apiVersion} in den Swagger-Pfaden durch die konkrete Version (z. B. /api/v1/…).
    options.SubstituteApiVersionInUrl = true;
    // Kombiniert die per ApiExplorerSettings gesetzte Gruppe mit der Version → "sap-import-v1".
    options.FormatGroupName = (group, version) => $"{group}-{version}";
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("sap-import-v1", new OpenApiInfo
    {
        Title = "ILLIG SAP-Import API",
        Version = "v1",
        Description = "Typisierte Endpunkte, über die SAP Stücklisten, Maximalstücklisten, offene Bestellungen und Lieferantenstammdaten direkt importiert."
    });
    options.SwaggerDoc("app-v1", new OpenApiInfo
    {
        Title = "ILLIG AI Platform API",
        Version = "v1",
        Description = "Endpoints der Web-App (Auftragsanlage, Lieferantenassistent, Plausibilitätsprüfung, Users)."
    });
    options.DocInclusionPredicate((docName, apiDesc) => apiDesc.GroupName == docName);

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API-Key für den SAP-Import (Header X-Api-Key)."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("ApiKey", document),
            new List<string>()
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Azure AD B2C Access Token (Bearer) für die App-Endpunkte."
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, AdminAuthorizationHandler>();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedOptions.KnownIPNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}

// Auffangnetz für Ausnahmen, die eine Controller-Action nicht selbst behandelt. Muss vor
// UseRouting/MapControllers stehen, damit es die gesamte nachgelagerte Pipeline umschließt.
app.UseExceptionHandler();

app.UseHttpsRedirection();

// index.html und blazor.boot.json referenzieren die aktuellen, fingerprinted _framework-Dateien.
// Werden sie vom Browser gecacht, zeigen sie nach einem Rebuild auf nicht mehr existierende
// Hash-Dateinamen (404 + SRI-Fehler). Die fingerprinted Dateien selbst dürfen dagegen beliebig
// lange gecacht werden, da der Hash im Dateinamen steckt.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.Equals("/", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("/blazor.boot.json", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Cache-Control"] = "no-cache, no-store";
            return Task.CompletedTask;
        });
    }

    await next();
});

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/sap-import-v1/swagger.json", "SAP Import API v1");
    options.SwaggerEndpoint("/swagger/app-v1/swagger.json", "App API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();

// ChatGPT probes the MCP endpoint before it has an access token and may omit the
// MCP Content-Type during that first request. Challenge these requests before
// the transport validates the media type; otherwise the client receives 415
// instead of the OAuth discovery challenge it needs to start authorization.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/mcp")
        && context.User.Identity?.IsAuthenticated != true)
    {
        await context.ChallengeAsync(McpConfiguration.AuthenticationScheme);
        return;
    }

    await next();
});

app.UseAuthorization();

app.UseMiddleware<UserProvisioningMiddleware>();

app.MapControllers();
app.MapMcp("/mcp").RequireAuthorization(new AuthorizeAttribute
{
    AuthenticationSchemes = McpConfiguration.AuthenticationScheme,
    Roles = AppRoles.SearchSystem,
});
app.MapFallbackToFile("index.html");

await app.RunAsync();

public partial class Program { }
