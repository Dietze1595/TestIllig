namespace Illig_AI_Platform.Shared.Data;

/// <summary>
/// Central, type-safe definition of role names — avoids magic strings/typos.
/// Usable as a constant in [Authorize(Roles = AppRoles.Admin)], in the seed and in comparisons.
/// The ids match the roles seeded in <see cref="AppDbContext"/>.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Dev = "DEV";
    public const string PlausibilityCheck = "PlausibilityCheck";
    public const string SearchSystem = "SearchSystem";
    public const string OrderCreationSales = "OrderCreationSales";
    public const string OrderCreationBackoffice = "OrderCreationBackoffice";
    public const string Lieferantenassistent = "Lieferantenassistent";

    /// <summary>Beide Auftragsanlage-Rollen kombiniert — für Endpunkte, die Vertrieb UND Innendienst offenstehen.</summary>
    public const string OrderCreation = OrderCreationSales + "," + OrderCreationBackoffice;

    /// <summary>Id + name of the seeded roles (order = id).</summary>
    public static readonly IReadOnlyList<(int Id, string Name)> Seed =
    [
        (1, Admin),
        (2, PlausibilityCheck),
        (3, SearchSystem),
        (4, OrderCreationSales),
        (5, OrderCreationBackoffice),
        (6, Lieferantenassistent),
        (7, Dev),
    ];
}
