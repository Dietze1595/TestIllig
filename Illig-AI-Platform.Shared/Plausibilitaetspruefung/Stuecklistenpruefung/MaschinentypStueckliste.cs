namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Ein einmalig importiertes Maximalstückliste+Umsetzungsmatrix-Paar für einen Maschinentyp.
/// <see cref="MaschinentypSchluessel"/> wird gegen den von <c>AuftragsinformationParser</c>
/// gelieferten <c>Maschinentyp</c>-Freitext per Prefix-Vergleich abgeglichen (siehe Design-Spec,
/// Abschnitt "Offene Annahme" — mit nur einer Beispielmaschine nicht abschließend verifiziert).
/// </summary>
public class MaschinentypStueckliste
{
    public int Id { get; set; }
    public string BomTyp { get; set; } = "";
    public string MaschinentypSchluessel { get; set; } = "";
    public string Kopfmaterial { get; set; } = "";
    public string Beschreibung { get; set; } = "";
    public string Werk { get; set; } = "";
    public string Stuecklistenverwendung { get; set; } = "";
    public string Stuecklistenalternative { get; set; } = "";
    public DateOnly GueltigAm { get; set; }
    public DateTime ImportiertAm { get; set; }
}
