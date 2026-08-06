namespace Illig_AI_Platform.Shared.Lieferantenassistent;

public record LieferantEmailAdresseAnsicht(string EmailAdresse, bool IstStandard);

public record OffenePositionAnsicht(
    int Id,
    string Einkaufsbeleg,
    string Position,
    int LieferantKreditor,
    string LieferantName,
    string? Einkaeufergruppe,
    string? Material,
    string Kurztext,
    decimal Bestellmenge,
    decimal NochZuLiefernMenge,
    DateOnly Lieferdatum,
    LieferterminStatus Status,
    IReadOnlyList<LieferantEmailAdresseAnsicht> EmailAdressen);
