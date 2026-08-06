using System.Security.Claims;
using System.Text.Encodings.Web;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Illig_AI_Platform.Services;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    private const string HeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var providedKey = Request.Headers.TryGetValue(HeaderName, out var values)
            ? values.ToString()
            : null;

        if (string.IsNullOrEmpty(providedKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        var configuredKey = configuration["SapIntegration:ApiKey"];
        if (!ApiKeyValidator.IsValid(providedKey, configuredKey))
            return Task.FromResult(AuthenticateResult.Fail("Ungültiger API-Key."));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, AppRoles.SearchSystem)], SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
