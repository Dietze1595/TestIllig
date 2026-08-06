namespace Illig_AI_Platform.Shared.Auftragsinformationen;

public class SharePointSynchronisationsstand
{
    public const string AuftragsinformationenQuelle = "Auftragsinformationen";

    public int Id { get; set; }
    public string Quelle { get; set; } = AuftragsinformationenQuelle;
    public string? DriveId { get; set; }
    public string? OrdnerItemId { get; set; }
    public string? DeltaLink { get; set; }
    public DateTime? LetzterVersuchAm { get; set; }
    public DateTime? LetzterErfolgAm { get; set; }
    public string? LetzterFehler { get; set; }
}
