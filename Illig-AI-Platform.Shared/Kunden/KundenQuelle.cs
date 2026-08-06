namespace Illig_AI_Platform.Shared.Kunden;

public enum KundenQuelltyp
{
    Angebot = 1,
    Kundenbestellung = 2,
    Auftragsinformation = 3,
}

public class KundenQuelle
{
    public int Id { get; set; }
    public int KundeId { get; set; }
    public KundenQuelltyp Quelltyp { get; set; }
    public int QuellId { get; set; }
    public string? Kundennummer { get; set; }
    public string? Kundenname { get; set; }
    public string? Kundenadresse { get; set; }
    public DateTime ErfasstAm { get; set; }
}
