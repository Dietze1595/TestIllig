namespace Illig_AI_Platform.Shared.Lieferantenassistent;

public class LieferantEmailAdresse
{
    public int Id { get; set; }
    public int AdressNummer { get; set; }
    public string EmailAdresse { get; set; } = "";
    public bool IstStandard { get; set; }
    public string? KontaktTyp { get; set; }
}
