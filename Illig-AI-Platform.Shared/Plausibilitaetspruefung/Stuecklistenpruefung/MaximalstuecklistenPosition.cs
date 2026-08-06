namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Ein Knoten im gemergten Stücklistenbaum (Maximalstückliste-Struktur + Umsetzungsmatrix-
/// Bedingung). <see cref="Bedingung"/> null bedeutet "immer enthalten" — laut echter
/// Beispieldatei der Normalfall (43% der Matrix-Zeilen ohne Bedingung, plus alle Positionen,
/// die in der Umsetzungsmatrix gar nicht vorkommen). Kein FK auf sich selbst mit Cascade-Delete
/// wählen (Full-Replace-Import löscht ohnehin den kompletten Baum eines Maschinentyps auf
/// einmal, nicht knotenweise). Nicht zu verwechseln mit der bestehenden, flachen
/// <see cref="StuecklistenPosition"/> (pro Auftragsnummer, täglicher SAP-Import) — komplett
/// anderes Konzept.
/// </summary>
public class MaximalstuecklistenPosition
{
    public int Id { get; set; }
    public int MaschinentypStuecklisteId { get; set; }
    public int? ParentId { get; set; }
    public int Reihenfolge { get; set; }
    public string SapNodeId { get; set; } = "";
    public string? SapPosition { get; set; }
    public string Typ { get; set; } = "";
    public string Artikelnummer { get; set; } = "";
    public string Bezeichnung { get; set; } = "";
    public decimal Menge { get; set; }
    public decimal? Gesamtmenge { get; set; }
    public string Einheit { get; set; } = "";
    public string? Bedingung { get; set; }
    public string? Dokumenttyp { get; set; }
    public string? Dokumentnummer { get; set; }
    public string? Dokumentversion { get; set; }
}
