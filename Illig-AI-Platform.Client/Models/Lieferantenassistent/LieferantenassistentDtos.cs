namespace Illig_AI_Platform.Client.Models.Lieferantenassistent;

// Client-eigene DTOs für den Lieferantenassistenten — bewusst ohne Shared/EF-Abhängigkeit
// (vgl. StuecklistenpruefungDtos.cs). Spiegeln die Shape der Server-DTOs.

public enum LieferterminStatus { ImPlan, BaldFaellig, Ueberfaellig }

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
