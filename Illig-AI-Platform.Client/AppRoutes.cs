namespace Illig_AI_Platform.Client;

/// <summary>
/// Route-Pfade der Client-Seiten an einer Stelle, damit Links (Home-Kacheln, "Zurück"-Buttons,
/// <see cref="Layout.MainLayout"/>-Breadcrumb) nicht bei jeder Umbenennung einzeln durchsucht
/// werden müssen. Muss zum jeweiligen <c>@page</c>-Attribut der Zielseite passen.
/// </summary>
public static class AppRoutes
{
    public const string Home = "/";
    public const string Plausibilitaetspruefung = "/plausibilitaetspruefung";
    public const string Sondermerkmale = "/sondermerkmale";
    public const string Referenztreffer = "/referenztreffer";
    public const string Auftragsdetails = "/auftragsdetails";
    public const string Stuecklistenpruefung = "/stuecklistenpruefung";
    public const string Auftragsanlage = "/auftragsanlage";
    public const string AuftragsanlageVertrieb = "/auftragsanlage/vertrieb";
    public const string AuftragsanlageInnendienst = "/auftragsanlage/innendienst";
    public const string AuftragsanlageDashboard = "/auftragsanlage/dashboard";
    public const string Lieferantenassistent = "/lieferantenassistent";
    public const string IlligGpt = "/illig-gpt";
    public const string Kunden = "/illig-gpt/kunden";
    public const string KundenDetail = "/illig-gpt/kunden/detail";
    public const string Profile = "/profile";
    public const string AdminBenutzer = "/admin/benutzer";
}
