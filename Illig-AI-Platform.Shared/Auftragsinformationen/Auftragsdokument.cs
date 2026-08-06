namespace Illig_AI_Platform.Shared.Auftragsinformationen;

/// <summary>
/// Strukturierte, aus SharePoint gelesene Auftragsinformation. Das Originaldokument bleibt in
/// SharePoint; Drive-/Item-ID sind die stabile Referenz zum erneuten Abruf.
/// </summary>
public class Auftragsdokument
{
    public int Id { get; set; }
    public string SharePointDriveId { get; set; } = "";
    public string SharePointItemId { get; set; } = "";
    public string ETag { get; set; } = "";
    public string Dateiname { get; set; } = "";
    public string WebUrl { get; set; } = "";
    public DateTime SharePointErstelltAm { get; set; }
    public DateTime SharePointGeaendertAm { get; set; }
    public string Auftragsnummer { get; set; } = "";
    public string Kundennummer { get; set; } = "";
    public string? Kundenname { get; set; }
    public string? Kundenadresse { get; set; }
    public DateOnly? Datum { get; set; }
    public string Maschinentyp { get; set; } = "";
    public AuftragsdokumentAnalyseStatus AnalyseStatus { get; set; }
    public string? AnalyseFehler { get; set; }
    public DateTime? VerarbeitetAm { get; set; }
    public DateTime? GeloeschtAm { get; set; }

    public ICollection<AuftragsdokumentMerkmal> Merkmale { get; set; } = [];
}
