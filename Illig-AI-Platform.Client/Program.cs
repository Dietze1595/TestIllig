using Illig_AI_Platform.Client;
using Illig_AI_Platform.Client.Services;
using Illig_AI_Platform.Client.Services.Auftragsanlage;
using Illig_AI_Platform.Client.Services.Lieferantenassistent;
using Illig_AI_Platform.Client.Services.Plausibilitaetspruefung.Sondermerkmalsuche;
using Illig_AI_Platform.Client.Services.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

const string FallbackAuthority =
    "https://illigaiplatform.b2clogin.com/illigaiplatform.onmicrosoft.com/B2C_1A_ILLIGMULTITENANTSIGNUPSIGNIN";
const string FallbackClientId =
    "942c9f77-fbb9-4e2d-aa53-c9ee1e9ef845";

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddTransient<ApiErrorHandler>();

builder.Services.AddHttpClient("ServerAPI", client =>
    {
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
        client.Timeout = TimeSpan.FromMinutes(10);
    })
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>()
    .AddHttpMessageHandler<ApiErrorHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

builder.Services.AddScoped<UserProfileState>();
builder.Services.AddScoped<SondermerkmalClient>();
builder.Services.AddScoped<StuecklistenpruefungClient>();
builder.Services.AddScoped<AuftragsanlageClient>();
builder.Services.AddScoped<LieferantenassistentClient>();
builder.Services.AddScoped<KundenClient>();
builder.Services.AddScoped<AdminUsersClient>();

builder.Services.AddMsalAuthentication(options =>
{
    var auth = options.ProviderOptions.Authentication;
    builder.Configuration.Bind("AzureAdB2C", auth);

    auth.Authority ??= FallbackAuthority;
    auth.ClientId ??= FallbackClientId;
    auth.ValidateAuthority = false;
    auth.RedirectUri ??= new Uri(new Uri(builder.HostEnvironment.BaseAddress), "authentication/login-callback").ToString();
    auth.PostLogoutRedirectUri ??= builder.HostEnvironment.BaseAddress;

    options.ProviderOptions.LoginMode = "redirect";
    options.ProviderOptions.Cache.CacheLocation = "localStorage";
    options.ProviderOptions.DefaultAccessTokenScopes.Add(AppConfig.ApiScope);
});

await builder.Build().RunAsync();
