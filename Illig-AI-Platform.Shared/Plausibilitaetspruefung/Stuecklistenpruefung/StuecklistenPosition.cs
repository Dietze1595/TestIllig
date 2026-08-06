namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public class StuecklistenPosition
{
    public int Id { get; set; }
    public string BomTyp { get; set; } = "";
    public string Auftragsnummer { get; set; } = "";
    public string Auftragsposition { get; set; } = "";
    public DateOnly GueltigAm { get; set; }
    public string RootNodeId { get; set; } = "";
    public string NodeId { get; set; } = "";
    public string? ParentNodeId { get; set; }
    public string? SapPosition { get; set; }
    public string Typ { get; set; } = "";
    public int Position { get; set; }
    public string Artikelnummer { get; set; } = "";
    public string Bezeichnung { get; set; } = "";
    public decimal Menge { get; set; }
    public string? Einheit { get; set; }
    public DateTime ImportiertAm { get; set; }
}
