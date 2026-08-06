namespace Illig_AI_Platform.Shared.Auftragsanlage;

public record VertriebsbedingungenErgebnis(
    string? Zahlungsbedingungen,
    string? Incoterm,
    string? IncotermOrt,
    string? Versandbedingung,
    string? ZahlungsbedingungCode = null,
    string? Zahlungsplan = null,
    string? Verkaeufer = null,
    string? Liefertermin = null,
    DateTime? GueltigBis = null);
