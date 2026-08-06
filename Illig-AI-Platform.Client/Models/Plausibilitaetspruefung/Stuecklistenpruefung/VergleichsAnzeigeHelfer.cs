namespace Illig_AI_Platform.Client.Models.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Anzeige-Helfer für Vergleichsergebnisse — gemeinsam genutzt von der flachen
/// "Nur in SAP"-Tabelle und der Baum-Ansicht (<see cref="VergleichsBaumZeile"/>).
/// </summary>
public static class VergleichsAnzeigeHelfer
{
    // Ganze Zahlen ohne Nachkommastellen, sonst bis zu 3, ohne unnötige Nullen — ohne
    // explizite Kultur, damit weiterhin die aktuelle UI-Kultur greift (Dezimal-Komma).
    public static string FormatMenge(decimal? menge) => menge?.ToString("0.###") ?? "—";

    public static string BadgeKlasse(VergleichsStatus status) => status switch
    {
        VergleichsStatus.Uebereinstimmung => "bg-success",
        VergleichsStatus.Abweichung => "bg-warning",
        VergleichsStatus.NurBeiUns => "bg-danger",
        VergleichsStatus.NurInSap => "bg-secondary",
        _ => "bg-secondary",
    };

    public static string StatusText(VergleichsStatus status) => status switch
    {
        VergleichsStatus.Uebereinstimmung => "Übereinstimmung",
        VergleichsStatus.Abweichung => "Abweichung",
        VergleichsStatus.NurBeiUns => "Fehlt in Stückliste",
        VergleichsStatus.NurInSap => "Nur in SAP",
        _ => status.ToString(),
    };
}
