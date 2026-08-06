namespace Illig_AI_Platform.Shared.Lieferantenassistent;

/// <summary>Lieferantenstammdaten aus dem SAP-Export LFA1. Kreditor ist der SAP-Schlüssel.</summary>
public class Lieferant
{
    public int Kreditor { get; set; }
    public string Name { get; set; } = "";
    public string? Land { get; set; }
    public string? Ort { get; set; }
    public int? Postleitzahl { get; set; }
    public string? Strasse { get; set; }
    public int? AdressNummer { get; set; }
}
