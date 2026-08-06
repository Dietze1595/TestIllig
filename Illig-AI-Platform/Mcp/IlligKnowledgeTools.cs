using System.ComponentModel;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Lieferantenassistent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Illig_AI_Platform.Mcp;

[McpServerToolType]
[Authorize(
    Roles = AppRoles.SearchSystem,
    AuthenticationSchemes = McpConfiguration.AuthenticationScheme)]
public sealed class IlligKnowledgeTools(
    AppDbContext db,
    IHttpContextAccessor httpContextAccessor,
    ILogger<IlligKnowledgeTools> logger)
{
    private const int DefaultLimit = 20;
    private const int MaximumLimit = 50;
    private const int MaximumQueryLength = 200;
    private const int MaximumDeviationLength = 2_000;

    [McpServerTool(
        Name = "search_customers",
        Title = "Kunden suchen",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [McpMeta("securitySchemes", "", JsonValue = McpConfiguration.SecuritySchemesJson)]
    [Description("Sucht im ILLIG-Kundenstamm nach Name, Kundennummer oder Adresse und liefert Aktivitätszahlen. Für die Detailansicht anschließend get_customer_context verwenden.")]
    public async Task<CustomerSearchResult> SearchCustomersAsync(
        [Description("Optionaler Suchtext für Kundenname, Kundennummer oder Adresse. Leer liefert die zuletzt aktualisierten Kunden.")]
        string? query = null,
        [Description("Maximale Trefferzahl zwischen 1 und 50. Standard: 20.")]
        int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        query = ValidateOptionalQuery(query);
        limit = ValidateLimit(limit);

        var customersQuery = db.Kunden.AsNoTracking();
        if (query is not null)
        {
            customersQuery = customersQuery.Where(customer =>
                (customer.Name != null && customer.Name.Contains(query)) ||
                (customer.Kundennummer != null && customer.Kundennummer.Contains(query)) ||
                (customer.Adresse != null && customer.Adresse.Contains(query)));
        }

        var rows = await customersQuery
            .OrderByDescending(customer => customer.AktualisiertAm)
            .ThenBy(customer => customer.Name)
            .Take(limit + 1)
            .Select(customer => new
            {
                customer.Id,
                customer.Name,
                customer.Kundennummer,
                customer.Adresse,
                customer.Status,
                customer.AktualisiertAm,
            })
            .ToListAsync(cancellationToken);

        var isTruncated = rows.Count > limit;
        rows = rows.Take(limit).ToList();
        var customerIds = rows.Select(row => row.Id).ToList();

        var offerCounts = await db.Angebote.AsNoTracking()
            .Where(offer => offer.KundeId != null && customerIds.Contains(offer.KundeId.Value))
            .GroupBy(offer => offer.KundeId!.Value)
            .Select(group => new { CustomerId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CustomerId, item => item.Count, cancellationToken);

        var customerOrderCounts = await db.Auftragsbestaetigungen.AsNoTracking()
            .Where(order => order.KundeId != null && customerIds.Contains(order.KundeId.Value))
            .GroupBy(order => order.KundeId!.Value)
            .Select(group => new { CustomerId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CustomerId, item => item.Count, cancellationToken);

        var machineOrderCounts = await db.StuecklistenpruefungVerlaufEintraege.AsNoTracking()
            .Where(order => order.KundeId != null && customerIds.Contains(order.KundeId.Value))
            .GroupBy(order => order.KundeId!.Value)
            .Select(group => new { CustomerId = group.Key, Count = group.Select(order => order.Auftragsnummer).Distinct().Count() })
            .ToDictionaryAsync(item => item.CustomerId, item => item.Count, cancellationToken);

        var customers = rows.Select(row => new CustomerSearchHit(
            row.Id,
            DisplayName(row.Name, row.Kundennummer, row.Id),
            row.Kundennummer,
            row.Adresse,
            row.Status.ToString(),
            offerCounts.GetValueOrDefault(row.Id),
            customerOrderCounts.GetValueOrDefault(row.Id),
            machineOrderCounts.GetValueOrDefault(row.Id),
            row.AktualisiertAm)).ToList();

        LogInvocation("search_customers", customers.Count);
        return new CustomerSearchResult(customers, isTruncated);
    }

    [McpServerTool(
        Name = "get_customer_context",
        Title = "Kundenkontext abrufen",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [McpMeta("securitySchemes", "", JsonValue = McpConfiguration.SecuritySchemesJson)]
    [Description("Liefert den strukturierten ILLIG-Kontext eines Kunden mit Angeboten, Kundenbestellungen und Maschinenaufträgen. Gibt keine Dokumentvolltexte, Blob-Pfade oder Benutzerprofile zurück.")]
    public async Task<CustomerContextResult> GetCustomerContextAsync(
        [Description("Interne Kunden-ID aus search_customers.")]
        int customerId,
        CancellationToken cancellationToken = default)
    {
        if (customerId <= 0)
            throw new McpException("customerId muss größer als 0 sein.");

        var customer = await db.Kunden.AsNoTracking()
            .Where(item => item.Id == customerId)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Kundennummer,
                item.Adresse,
                item.Status,
                item.AktualisiertAm,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new McpException($"Kunde mit ID {customerId} wurde nicht gefunden.");

        var offers = await db.Angebote.AsNoTracking()
            .Where(offer => offer.KundeId == customerId)
            .OrderByDescending(offer => offer.HochgeladenAm)
            .Take(MaximumLimit)
            .Select(offer => new OfferContext(
                offer.Id,
                offer.Angebotsnummer,
                offer.Version,
                offer.Zahlungsbedingungen,
                offer.Zahlungsplan,
                offer.Verkaeufer,
                offer.Liefertermin,
                offer.GueltigBis,
                offer.Incoterm,
                offer.IncotermOrt,
                offer.Versandbedingung,
                offer.Freigegeben,
                offer.HochgeladenAm))
            .ToListAsync(cancellationToken);

        var offerNumbers = await db.Angebote.AsNoTracking()
            .Where(offer => offer.KundeId == customerId)
            .Select(offer => new { offer.Id, offer.Angebotsnummer })
            .ToDictionaryAsync(offer => offer.Id, offer => offer.Angebotsnummer, cancellationToken);

        var customerOrdersRaw = await db.Auftragsbestaetigungen.AsNoTracking()
            .Where(order => order.KundeId == customerId)
            .OrderByDescending(order => order.HochgeladenAm)
            .Take(MaximumLimit)
            .Select(order => new
            {
                order.Id,
                order.Nummer,
                order.AngebotId,
                order.Zahlungsbedingungen,
                order.LieferterminAngebot,
                order.LieferterminBestaetigung,
                order.LieferterminIdentisch,
                order.SonstigeAbweichungen,
                order.HochgeladenAm,
            })
            .ToListAsync(cancellationToken);

        var customerOrders = customerOrdersRaw.Select(order => new CustomerOrderContext(
            order.Id,
            order.Nummer,
            offerNumbers.GetValueOrDefault(order.AngebotId) ?? $"Angebot #{order.AngebotId}",
            order.Zahlungsbedingungen,
            order.LieferterminAngebot,
            order.LieferterminBestaetigung,
            order.LieferterminIdentisch,
            Truncate(order.SonstigeAbweichungen, MaximumDeviationLength),
            order.HochgeladenAm)).ToList();

        var machineOrdersRaw = await db.StuecklistenpruefungVerlaufEintraege.AsNoTracking()
            .Where(order => order.KundeId == customerId)
            .OrderByDescending(order => order.ErstelltAm)
            .Take(MaximumLimit * 2)
            .Select(order => new
            {
                order.Id,
                order.Auftragsnummer,
                order.Maschinentyp,
                order.Datum,
                order.ErstelltAm,
            })
            .ToListAsync(cancellationToken);

        var machineOrders = machineOrdersRaw
            .GroupBy(order => order.Auftragsnummer, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(MaximumLimit)
            .Select(order => new MachineOrderContext(
                order.Id,
                order.Auftragsnummer,
                order.Maschinentyp,
                order.Datum,
                order.ErstelltAm))
            .ToList();

        LogInvocation("get_customer_context", offers.Count + customerOrders.Count + machineOrders.Count);
        return new CustomerContextResult(
            new CustomerHeader(
                customer.Id,
                DisplayName(customer.Name, customer.Kundennummer, customer.Id),
                customer.Kundennummer,
                customer.Adresse,
                customer.Status.ToString(),
                customer.AktualisiertAm),
            offers,
            customerOrders,
            machineOrders);
    }

    [McpServerTool(
        Name = "search_commercial_documents",
        Title = "Angebote und Kundenbestellungen suchen",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [McpMeta("securitySchemes", "", JsonValue = McpConfiguration.SecuritySchemesJson)]
    [Description("Sucht Angebote und Kundenbestellungen anhand einer Referenz oder eines Kundennamens. Liefert nur strukturierte Kerndaten und keine Dokumentvolltexte.")]
    public async Task<CommercialDocumentSearchResult> SearchCommercialDocumentsAsync(
        [Description("Angebotsnummer, Kundenbestellnummer oder Kundenname; mindestens zwei Zeichen.")]
        string query,
        [Description("Maximale Gesamttrefferzahl zwischen 1 und 50. Standard: 20.")]
        int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        query = ValidateRequiredQuery(query, 2);
        limit = ValidateLimit(limit);

        var offers = await db.Angebote.AsNoTracking()
            .Where(offer => offer.Angebotsnummer.Contains(query) ||
                            (offer.Kundenname != null && offer.Kundenname.Contains(query)))
            .OrderByDescending(offer => offer.HochgeladenAm)
            .Take(limit + 1)
            .Select(offer => new CommercialDocumentSearchHit(
                "Angebot",
                offer.Id,
                offer.Angebotsnummer,
                null,
                offer.KundeId,
                offer.Kundenname,
                offer.Liefertermin,
                null,
                null,
                offer.HochgeladenAm))
            .ToListAsync(cancellationToken);

        var ordersRaw = await db.Auftragsbestaetigungen.AsNoTracking()
            .Where(order => order.Nummer.Contains(query) ||
                            (order.Kundenname != null && order.Kundenname.Contains(query)))
            .OrderByDescending(order => order.HochgeladenAm)
            .Take(limit + 1)
            .Select(order => new
            {
                order.Id,
                order.Nummer,
                order.AngebotId,
                order.KundeId,
                order.Kundenname,
                order.LieferterminBestaetigung,
                order.LieferterminIdentisch,
                order.SonstigeAbweichungen,
                order.HochgeladenAm,
            })
            .ToListAsync(cancellationToken);

        var referencedOfferIds = ordersRaw.Select(order => order.AngebotId).Distinct().ToList();
        var referencedOffers = await db.Angebote.AsNoTracking()
            .Where(offer => referencedOfferIds.Contains(offer.Id))
            .Select(offer => new { offer.Id, offer.Angebotsnummer })
            .ToDictionaryAsync(offer => offer.Id, offer => offer.Angebotsnummer, cancellationToken);

        var orders = ordersRaw.Select(order => new CommercialDocumentSearchHit(
            "Kundenbestellung",
            order.Id,
            order.Nummer,
            referencedOffers.GetValueOrDefault(order.AngebotId),
            order.KundeId,
            order.Kundenname,
            order.LieferterminBestaetigung,
            order.LieferterminIdentisch,
            Truncate(order.SonstigeAbweichungen, MaximumDeviationLength),
            order.HochgeladenAm));

        var combined = offers
            .Concat(orders)
            .OrderByDescending(document => document.CapturedAt)
            .ToList();
        var isTruncated = combined.Count > limit;
        var documents = combined.Take(limit).ToList();

        LogInvocation("search_commercial_documents", documents.Count);
        return new CommercialDocumentSearchResult(documents, isTruncated);
    }

    [McpServerTool(
        Name = "search_supplier_commitments",
        Title = "Offene Lieferantenpositionen suchen",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [McpMeta("securitySchemes", "", JsonValue = McpConfiguration.SecuritySchemesJson)]
    [Description("Sucht offene SAP-Dispositionspositionen mit Lieferant, Material, Restmenge und Lieferstatus. Gibt keine E-Mail-Adressen oder Preise zurück.")]
    public async Task<SupplierCommitmentSearchResult> SearchSupplierCommitmentsAsync(
        [Description("Optionaler Suchtext für Lieferantenname, Material, Kurztext oder Einkaufsbeleg.")]
        string? query = null,
        [Description("Optionales spätestes Lieferdatum einschließlich, ISO-Format YYYY-MM-DD.")]
        DateOnly? dueBefore = null,
        [Description("Wenn true, werden nur bereits überfällige Positionen geliefert.")]
        bool overdueOnly = false,
        [Description("Maximale Trefferzahl zwischen 1 und 50. Standard: 20.")]
        int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        query = ValidateOptionalQuery(query);
        limit = ValidateLimit(limit);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var commitmentsQuery =
            from position in db.Dispositionspositionen.AsNoTracking()
            where position.NochZuLiefernMenge > 0
            join supplier in db.Lieferanten.AsNoTracking()
                on position.LieferantKreditor equals supplier.Kreditor
            select new { Position = position, Supplier = supplier };

        if (query is not null)
        {
            commitmentsQuery = commitmentsQuery.Where(item =>
                item.Supplier.Name.Contains(query) ||
                (item.Position.Material != null && item.Position.Material.Contains(query)) ||
                item.Position.Kurztext.Contains(query) ||
                item.Position.Einkaufsbeleg.Contains(query));
        }

        if (dueBefore is not null)
            commitmentsQuery = commitmentsQuery.Where(item => item.Position.Lieferdatum <= dueBefore.Value);
        if (overdueOnly)
            commitmentsQuery = commitmentsQuery.Where(item => item.Position.Lieferdatum < today);

        var rows = await commitmentsQuery
            .OrderBy(item => item.Position.Lieferdatum)
            .ThenBy(item => item.Supplier.Name)
            .Take(limit + 1)
            .Select(item => new
            {
                item.Position.Id,
                item.Position.Einkaufsbeleg,
                item.Position.Position,
                item.Supplier.Kreditor,
                SupplierName = item.Supplier.Name,
                item.Position.Einkaeufergruppe,
                item.Position.Material,
                item.Position.Kurztext,
                item.Position.Bestellmenge,
                item.Position.NochZuLiefernMenge,
                item.Position.Bestellmengeneinheit,
                item.Position.Lieferdatum,
            })
            .ToListAsync(cancellationToken);

        var isTruncated = rows.Count > limit;
        var commitments = rows.Take(limit).Select(row => new SupplierCommitment(
            row.Id,
            row.Einkaufsbeleg,
            row.Position,
            row.Kreditor,
            row.SupplierName,
            row.Einkaeufergruppe,
            row.Material,
            row.Kurztext,
            row.Bestellmenge,
            row.NochZuLiefernMenge,
            row.Bestellmengeneinheit,
            row.Lieferdatum,
            LieferterminStatusBerechnung.Berechnen(row.Lieferdatum, today).ToString())).ToList();

        LogInvocation("search_supplier_commitments", commitments.Count);
        return new SupplierCommitmentSearchResult(commitments, isTruncated, today);
    }

    [McpServerTool(
        Name = "search_bill_of_materials",
        Title = "Stücklistenpositionen suchen",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [McpMeta("securitySchemes", "", JsonValue = McpConfiguration.SecuritySchemesJson)]
    [Description("Sucht importierte Stücklistenpositionen anhand einer Auftrags- oder Artikelnummer.")]
    public async Task<BillOfMaterialsSearchResult> SearchBillOfMaterialsAsync(
        [Description("Optionale vollständige oder teilweise Auftragsnummer.")]
        string? orderNumber = null,
        [Description("Optionale vollständige oder teilweise Artikelnummer.")]
        string? articleNumber = null,
        [Description("Maximale Trefferzahl zwischen 1 und 50. Standard: 20.")]
        int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        orderNumber = ValidateOptionalQuery(orderNumber);
        articleNumber = ValidateOptionalQuery(articleNumber);
        if (orderNumber is null && articleNumber is null)
            throw new McpException("Mindestens orderNumber oder articleNumber muss angegeben werden.");

        limit = ValidateLimit(limit);
        var positionsQuery = db.StuecklistenPositionen.AsNoTracking();
        if (orderNumber is not null)
            positionsQuery = positionsQuery.Where(position => position.Auftragsnummer.Contains(orderNumber));
        if (articleNumber is not null)
            positionsQuery = positionsQuery.Where(position => position.Artikelnummer.Contains(articleNumber));

        var rows = await positionsQuery
            .OrderByDescending(position => position.ImportiertAm)
            .ThenBy(position => position.Auftragsnummer)
            .ThenBy(position => position.Position)
            .Take(limit + 1)
            .Select(position => new BillOfMaterialsPosition(
                position.Id,
                position.Auftragsnummer,
                position.Position,
                position.Artikelnummer,
                position.Bezeichnung,
                position.Menge,
                position.Einheit,
                position.ImportiertAm))
            .ToListAsync(cancellationToken);

        var isTruncated = rows.Count > limit;
        var positions = rows.Take(limit).ToList();
        LogInvocation("search_bill_of_materials", positions.Count);
        return new BillOfMaterialsSearchResult(positions, isTruncated);
    }

    private static int ValidateLimit(int limit)
    {
        if (limit is < 1 or > MaximumLimit)
            throw new McpException($"limit muss zwischen 1 und {MaximumLimit} liegen.");
        return limit;
    }

    private static string ValidateRequiredQuery(string? query, int minimumLength)
    {
        var value = ValidateOptionalQuery(query);
        if (value is null || value.Length < minimumLength)
            throw new McpException($"query muss mindestens {minimumLength} Zeichen enthalten.");
        return value;
    }

    private static string? ValidateOptionalQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        var value = query.Trim();
        if (value.Length > MaximumQueryLength)
            throw new McpException($"Suchtexte dürfen maximal {MaximumQueryLength} Zeichen enthalten.");
        return value;
    }

    private static string DisplayName(string? name, string? customerNumber, int id) =>
        !string.IsNullOrWhiteSpace(name)
            ? name
            : !string.IsNullOrWhiteSpace(customerNumber)
                ? $"Kunde {customerNumber}"
                : $"Kunde #{id}";

    private static string Truncate(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return value.Length <= maximumLength ? value : $"{value[..maximumLength]}…";
    }

    private void LogInvocation(string toolName, int resultCount)
    {
        var objectId = httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value ?? "unknown";
        logger.LogInformation(
            "MCP tool {ToolName} invoked by user {ObjectId}; returned {ResultCount} items.",
            toolName,
            objectId,
            resultCount);
    }
}
