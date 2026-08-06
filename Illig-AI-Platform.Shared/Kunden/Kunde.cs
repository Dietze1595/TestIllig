namespace Illig_AI_Platform.Shared.Kunden;

public enum KundeStatus
{
    Vorlaeufig = 0,
    Bestaetigt = 1,
    Neukunde = 2,
}

public class Kunde
{
    public int Id { get; set; }
    public string? Kundennummer { get; set; }
    public string? Name { get; set; }
    public string? Adresse { get; set; }
    public string NormalisierterName { get; set; } = "";
    public string NormalisierteAdresse { get; set; } = "";
    public KundeStatus Status { get; set; }
    public DateTime ErstelltAm { get; set; }
    public DateTime AktualisiertAm { get; set; }
}
