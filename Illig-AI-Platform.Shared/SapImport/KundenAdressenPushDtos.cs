namespace Illig_AI_Platform.Shared.SapImport;

public sealed class KundenAdressenPush
{
    public List<SapKundenAdressGruppe> Kunden { get; init; } = [];
}

public sealed class SapKundenAdressGruppe
{
    public string Hauptkundennummer { get; init; } = "";
    public List<SapKundenPartnerAdresse> Adressen { get; init; } = [];
}

public sealed class SapKundenPartnerAdresse
{
    public string Partnerrolle { get; init; } = "";
    public string PartnerId { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Strasse { get; init; }
    public string? Plz { get; init; }
    public string? Ort { get; init; }
    public string? Land { get; init; }
}
