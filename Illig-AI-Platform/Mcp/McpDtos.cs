namespace Illig_AI_Platform.Mcp;

public sealed record CustomerSearchResult(
    IReadOnlyList<CustomerSearchHit> Customers,
    bool IsTruncated);

public sealed record CustomerSearchHit(
    int Id,
    string DisplayName,
    string? CustomerNumber,
    string? Address,
    string Status,
    int Offers,
    int CustomerOrders,
    int MachineOrders,
    DateTime LastActivity);

public sealed record CustomerContextResult(
    CustomerHeader Customer,
    IReadOnlyList<OfferContext> Offers,
    IReadOnlyList<CustomerOrderContext> CustomerOrders,
    IReadOnlyList<MachineOrderContext> MachineOrders);

public sealed record CustomerHeader(
    int Id,
    string DisplayName,
    string? CustomerNumber,
    string? Address,
    string Status,
    DateTime UpdatedAt);

public sealed record OfferContext(
    int Id,
    string OfferNumber,
    int Version,
    string? PaymentTerms,
    string? PaymentPlan,
    string? Seller,
    string? DeliveryTerm,
    DateTime? ValidUntil,
    string? Incoterm,
    string? IncotermLocation,
    string? ShippingCondition,
    bool Approved,
    DateTime UploadedAt);

public sealed record CustomerOrderContext(
    int Id,
    string OrderNumber,
    string OfferNumber,
    string? PaymentTerms,
    string? OfferDeliveryTerm,
    string? OrderDeliveryTerm,
    bool DeliveryTermMatches,
    string OtherDeviations,
    DateTime UploadedAt);

public sealed record MachineOrderContext(
    int Id,
    string OrderNumber,
    string MachineType,
    DateOnly? OrderDate,
    DateTime CapturedAt);

public sealed record CommercialDocumentSearchResult(
    IReadOnlyList<CommercialDocumentSearchHit> Documents,
    bool IsTruncated);

public sealed record CommercialDocumentSearchHit(
    string DocumentType,
    int Id,
    string Reference,
    string? RelatedOffer,
    int? CustomerId,
    string? CustomerName,
    string? DeliveryTerm,
    bool? DeliveryTermMatches,
    string? OtherDeviations,
    DateTime CapturedAt);

public sealed record SupplierCommitmentSearchResult(
    IReadOnlyList<SupplierCommitment> Commitments,
    bool IsTruncated,
    DateOnly EvaluatedAt);

public sealed record SupplierCommitment(
    int Id,
    string PurchaseDocument,
    string Position,
    int SupplierNumber,
    string SupplierName,
    string? BuyerGroup,
    string? Material,
    string Description,
    decimal OrderedQuantity,
    decimal OutstandingQuantity,
    string? Unit,
    DateOnly DueDate,
    string DueStatus);

public sealed record BillOfMaterialsSearchResult(
    IReadOnlyList<BillOfMaterialsPosition> Positions,
    bool IsTruncated);

public sealed record BillOfMaterialsPosition(
    int Id,
    string OrderNumber,
    int Position,
    string ArticleNumber,
    string Description,
    decimal Quantity,
    string? Unit,
    DateTime ImportedAt);
