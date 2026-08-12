namespace Illig_AI_Platform.Shared.Kunden;

public class KundenPartneradresse
{
    public int Id { get; set; }
    public string Hauptkundennummer { get; set; } = "";
    public string Partnerrolle { get; set; } = "";
    public string PartnerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Strasse { get; set; }
    public string? Plz { get; set; }
    public string? Ort { get; set; }
    public string? Land { get; set; }
    public DateTime ImportiertAm { get; set; }
}
