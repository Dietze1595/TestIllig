namespace Illig_AI_Platform.Services.Auftragsinformationen;

public sealed class SharePointAuftragsinformationenOptions
{
    public const string SectionName = "SharePointAuftragsinformationen";

    public bool Enabled { get; set; }
    public string HostName { get; set; } = "illiggroup.sharepoint.com";
    public string SitePath { get; set; } = "/sites/DVN_SAP_DE";
    public string LibraryName { get; set; } = "Freigegebene Dokumente";
    public string FolderPath { get; set; } = "Projekt Novazoon/99_SAP_Dokument_Transfer/Auftragsinformationen";
    public string? DriveId { get; set; }
    public string? PermissionRootItemId { get; set; }
    public string SyncSubfolderPath { get; set; } = "Auftragsinformationen";
    public int HistoricalYears { get; set; } = 5;
    public int PollingMinutes { get; set; } = 60;
    public int MaxFileSizeMegabytes { get; set; } = 50;
    public string? ResourceTenantId { get; set; }
    public string? ApplicationClientId { get; set; }
    public string? ManagedIdentityClientId { get; set; }
}
