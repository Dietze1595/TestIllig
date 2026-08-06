namespace Illig_AI_Platform.Shared.Lieferantenassistent;

/// <summary>Eine Zeile des SAP-Reports "Einkaufsbelege zum Lieferant" (Einkaufsbeleg + Position).</summary>
public class Dispositionsposition
{
    public int Id { get; set; }
    // Einkaufsbeleg+Position ist NICHT eindeutig: SAP-Lieferpläne teilen eine Position in
    // mehrere Einteilungen (Teillieferungen) mit je eigenem Lieferdatum auf, die als separate
    // Zeilen im Report erscheinen. Additiver Import-/Eindeutigkeits-Schlüssel ist deshalb
    // Einkaufsbeleg+Position+Lieferdatum (siehe Schluessel), nicht Einkaufsbeleg+Position allein.
    public string Einkaufsbeleg { get; set; } = "";
    public string Position { get; set; } = "";
    public string? Einteilungsnummer { get; set; }
    public string Schluessel { get; set; } = "";
    // Bewusst keine EF-Navigation/FK zu Lieferant: Dispositionsliste und Lieferantenstammdaten
    // werden unabhängig importiert, eine harte FK würde den Import in der "falschen"
    // Reihenfolge (Dispo vor Stammdaten) zum Scheitern bringen.
    public int LieferantKreditor { get; set; }
    public string? Einkaeufergruppe { get; set; }
    public DateOnly? Belegdatum { get; set; }
    public DateOnly Lieferdatum { get; set; }
    public string? Auftragsbestaetigung { get; set; }
    public string? LabNr { get; set; }
    public string? Material { get; set; }
    public string Kurztext { get; set; } = "";
    public string? Werk { get; set; }
    public string? EinkaeufergruppenName { get; set; }
    public string? VerantwortlichePerson { get; set; }
    public string? EinkaeuferEmail { get; set; }
    public string? MaterialgruppenCode { get; set; }
    public string? MaterialgruppenName { get; set; }
    public decimal Bestellmenge { get; set; }
    public string? Bestellmengeneinheit { get; set; }
    public decimal Nettopreis { get; set; }
    public string? Waehrung { get; set; }
    public decimal Preiseinheit { get; set; }
    public decimal Einteilungsmenge { get; set; }
    public decimal GelieferteMenge { get; set; }
    public decimal NochZuLiefernMenge { get; set; }
    public decimal MengeInLagerME { get; set; }
    public DateTime ImportiertAm { get; set; }
}
