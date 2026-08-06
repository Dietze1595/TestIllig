using System.Text.Json.Serialization;

namespace Illig_AI_Platform.Shared.SapImport;

public sealed class StuecklistenPush
{
    public string BomType { get; init; } = "";
    public string OrderNumber { get; init; } = "";
    public string OrderItem { get; init; } = "";
    public DateOnly ValidAt { get; init; }
    public string RootNodeId { get; init; } = "";
    public List<SapBomKnoten> Nodes { get; init; } = [];
}

public sealed class MaximalstuecklistenPush
{
    public string BomType { get; init; } = "";
    public string MaterialNumber { get; init; } = "";
    public string Description { get; init; } = "";
    public string Plant { get; init; } = "";
    public string BomUsage { get; init; } = "";
    public string BomAlternative { get; init; } = "";
    public DateOnly ValidAt { get; init; }
    public List<SapBomKnoten> Nodes { get; init; } = [];
}

public sealed class SapBomKnoten
{
    public string NodeId { get; init; } = "";
    public string? ParentNodeId { get; init; }
    public string? Position { get; init; }
    public string Type { get; init; } = "";
    public string? MaterialNumber { get; init; }
    public string? Description { get; init; }
    public decimal Quantity { get; init; }
    public decimal? TotalQuantity { get; init; }
    public string? Unit { get; init; }
    public SapDokumentReferenz? Document { get; init; }
}

public sealed class SapDokumentReferenz
{
    public string DocumentType { get; init; } = "";
    public string DocumentNumber { get; init; } = "";
    public string Version { get; init; } = "";
}

public sealed class OffeneBestellungenPush
{
    public List<SapBestellung> PurchaseOrders { get; init; } = [];
}

public sealed class SapBestellung
{
    public string PurchaseOrderNumber { get; init; } = "";

    [JsonPropertyName("LABNr")]
    public string? LabNr { get; init; }

    public string SupplierNumber { get; init; } = "";
    public DateOnly? DocumentDate { get; init; }
    public string? PurchasingGroup { get; init; }
    public string? Currency { get; init; }
    public List<SapBestellposition> Items { get; init; } = [];
}

public sealed class SapBestellposition
{
    public string Position { get; init; } = "";
    public string? MaterialNumber { get; init; }
    public string Description { get; init; } = "";
    public string? Plant { get; init; }
    public SapEinkaeufergruppe? PurchasingGroup { get; init; }
    public SapMaterialgruppe? MaterialGroup { get; init; }
    public decimal OrderQuantity { get; init; }
    public string? Unit { get; init; }
    public List<SapEinteilung> ScheduleLines { get; init; } = [];
}

public sealed class SapEinkaeufergruppe
{
    public string? Code { get; init; }
    public string? Name { get; init; }
    public string? ResponsiblePerson { get; init; }
    public string? Email { get; init; }
}

public sealed class SapMaterialgruppe
{
    public string? Code { get; init; }
    public string? Name { get; init; }
}

public sealed class SapEinteilung
{
    public string ScheduleLineNumber { get; init; } = "";
    public DateOnly DeliveryDate { get; init; }
    public decimal ScheduledQuantity { get; init; }
    public decimal DeliveredQuantity { get; init; }
    public decimal OpenQuantity { get; init; }
    public string? Confirmation { get; init; }
}

public sealed class LieferantenDatenPush
{
    public List<SapLieferant> Suppliers { get; init; } = [];
}

public sealed class SapLieferant
{
    public string SupplierNumber { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Country { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Street { get; init; }
    public string? AddressNumber { get; init; }
    public List<SapLieferantenkontakt> Contacts { get; init; } = [];
}

public sealed class SapLieferantenkontakt
{
    public string Email { get; init; } = "";
    public bool IsDefault { get; init; }
    public string? ContactType { get; init; }
}

public sealed record SapDirektImportErgebnis(
    int ZeilenAnzahl,
    int ErsetzteZeilen,
    DateTime ImportiertAm);
