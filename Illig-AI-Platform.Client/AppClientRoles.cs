namespace Illig_AI_Platform.Client;

/// <summary>
/// Client-seitige Konstanten der App-Rollennamen (müssen mit den DB-Rollen übereinstimmen).
/// Der Client referenziert Shared/AppRoles bewusst nicht (kein EF im WASM-Bundle).
/// </summary>
public static class AppClientRoles
{
    public const string Admin = "Admin";
    public const string Dev = "DEV";
    public const string PlausibilityCheck = "PlausibilityCheck";
    public const string SearchSystem = "SearchSystem";
    public const string OrderCreationSales = "OrderCreationSales";
    public const string OrderCreationBackoffice = "OrderCreationBackoffice";
    public const string Lieferantenassistent = "Lieferantenassistent";

    public static bool HatAdminBerechtigungen(IEnumerable<string> roles) =>
        roles.Any(role => role is Admin or Dev);
}
