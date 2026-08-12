using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.Lieferantenassistent;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Illig_AI_Platform.Shared.SapImport;

/// <summary>
/// Materialisiert die SAP-Push-Verträge direkt in den jeweiligen Fachtabellen.
/// Ein Rohdaten-Staging ist bewusst nicht Bestandteil dieses Imports.
/// </summary>
public sealed class SapDirektImportService(AppDbContext db, ILogger<SapDirektImportService> logger)
{
    public Task<SapDirektImportErgebnis> ImportiereStuecklisteAsync(
        StuecklistenPush push,
        CancellationToken cancellationToken = default)
    {
        ValidiereStueckliste(push);

        return InTransaktionAsync(async () =>
        {
            var importiertAm = DateTime.UtcNow;
            var vorhandene = await db.StuecklistenPositionen
                .Where(position => position.Auftragsnummer == push.OrderNumber &&
                                   position.Auftragsposition == push.OrderItem)
                .ToListAsync(cancellationToken);

            db.StuecklistenPositionen.RemoveRange(vorhandene);
            db.StuecklistenPositionen.AddRange(push.Nodes.Select(node => new StuecklistenPosition
            {
                BomTyp = push.BomType,
                Auftragsnummer = push.OrderNumber,
                Auftragsposition = push.OrderItem,
                GueltigAm = push.ValidAt,
                RootNodeId = push.RootNodeId,
                NodeId = node.NodeId,
                ParentNodeId = node.ParentNodeId,
                SapPosition = node.Position,
                Typ = node.Type,
                Position = Positionsnummer(node.Position),
                Artikelnummer = node.MaterialNumber ?? node.Position ?? node.NodeId,
                Bezeichnung = node.Description ?? "",
                Menge = node.Quantity,
                Einheit = node.Unit,
                ImportiertAm = importiertAm
            }));

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "SAP-Auftragsstückliste {Auftrag}/{Position}: {Anzahl} Knoten importiert, {Ersetzt} ersetzt.",
                push.OrderNumber, push.OrderItem, push.Nodes.Count, vorhandene.Count);
            return new SapDirektImportErgebnis(push.Nodes.Count, vorhandene.Count, importiertAm);
        }, cancellationToken);
    }

    public Task<SapDirektImportErgebnis> ImportiereMaximalstuecklisteAsync(
        MaximalstuecklistenPush push,
        CancellationToken cancellationToken = default)
    {
        ValidiereMaximalstueckliste(push);

        return InTransaktionAsync(async () =>
        {
            var importiertAm = DateTime.UtcNow;
            var maschinentypSchluessel = push.Description.Trim();
            var vorhandeneStuecklisten = await db.MaschinentypStuecklisten
                .Where(stueckliste =>
                    stueckliste.MaschinentypSchluessel == maschinentypSchluessel ||
                    (stueckliste.Kopfmaterial == push.MaterialNumber &&
                     stueckliste.Werk == push.Plant &&
                     stueckliste.Stuecklistenverwendung == push.BomUsage &&
                     stueckliste.Stuecklistenalternative == push.BomAlternative))
                .ToListAsync(cancellationToken);
            var ersetztePositionen = 0;
            foreach (var vorhanden in vorhandeneStuecklisten)
            {
                var positionen = await db.MaximalstuecklistenPositionen
                    .Where(position => position.MaschinentypStuecklisteId == vorhanden.Id)
                    .ToListAsync(cancellationToken);
                ersetztePositionen += positionen.Count;
                db.MaximalstuecklistenPositionen.RemoveRange(positionen);
                db.MaschinentypStuecklisten.Remove(vorhanden);
            }
            if (vorhandeneStuecklisten.Count > 0)
                await db.SaveChangesAsync(cancellationToken);

            var stueckliste = new MaschinentypStueckliste
            {
                BomTyp = push.BomType,
                MaschinentypSchluessel = maschinentypSchluessel,
                Kopfmaterial = push.MaterialNumber,
                Beschreibung = push.Description,
                Werk = push.Plant,
                Stuecklistenverwendung = push.BomUsage,
                Stuecklistenalternative = push.BomAlternative,
                GueltigAm = push.ValidAt,
                ImportiertAm = importiertAm
            };
            db.MaschinentypStuecklisten.Add(stueckliste);
            await db.SaveChangesAsync(cancellationToken);

            var restlicheKnoten = push.Nodes.ToDictionary(node => node.NodeId, StringComparer.Ordinal);
            var datenbankIds = new Dictionary<string, int>(StringComparer.Ordinal);
            var reihenfolge = push.Nodes
                .Select((node, index) => (node.NodeId, index))
                .ToDictionary(entry => entry.NodeId, entry => entry.index, StringComparer.Ordinal);

            while (restlicheKnoten.Count > 0)
            {
                var importierbar = restlicheKnoten.Values
                    .Where(node => node.ParentNodeId is null || datenbankIds.ContainsKey(node.ParentNodeId))
                    .ToList();
                if (importierbar.Count == 0)
                    throw new SapImportValidierungsException(["Die Maximalstückliste enthält eine zyklische Hierarchie."]);

                foreach (var node in importierbar)
                {
                    var position = new MaximalstuecklistenPosition
                    {
                        MaschinentypStuecklisteId = stueckliste.Id,
                        ParentId = node.ParentNodeId is null ? null : datenbankIds[node.ParentNodeId],
                        Reihenfolge = reihenfolge[node.NodeId],
                        SapNodeId = node.NodeId,
                        SapPosition = node.Position,
                        Typ = node.Type,
                        // Die bestehende Vergleichslogik benötigt für jeden Knoten einen stabilen Wert.
                        // TEXT-/DOCUMENT-Knoten besitzen laut SAP-Vertrag keine Materialnummer.
                        Artikelnummer = node.MaterialNumber ?? node.Position ?? node.NodeId,
                        Bezeichnung = node.Description ?? "",
                        Menge = node.Quantity,
                        Gesamtmenge = node.TotalQuantity,
                        Einheit = node.Unit ?? "",
                        Dokumenttyp = node.Document?.DocumentType,
                        Dokumentnummer = node.Document?.DocumentNumber,
                        Dokumentversion = node.Document?.Version
                    };
                    db.MaximalstuecklistenPositionen.Add(position);
                    await db.SaveChangesAsync(cancellationToken);
                    datenbankIds[node.NodeId] = position.Id;
                    restlicheKnoten.Remove(node.NodeId);
                }
            }

            logger.LogInformation(
                "SAP-Maximalstückliste {Maschinentyp}: {Anzahl} Knoten importiert, {Ersetzt} ersetzt.",
                maschinentypSchluessel, push.Nodes.Count, ersetztePositionen);
            return new SapDirektImportErgebnis(push.Nodes.Count, ersetztePositionen, importiertAm);
        }, cancellationToken);
    }

    public Task<SapDirektImportErgebnis> ImportiereDispositionAsync(
        OffeneBestellungenPush push,
        CancellationToken cancellationToken = default)
    {
        var neuePositionen = ErzeugeDispositionspositionen(push);

        return InTransaktionAsync(async () =>
        {
            var importiertAm = DateTime.UtcNow;
            foreach (var position in neuePositionen)
                position.ImportiertAm = importiertAm;

            var schluessel = neuePositionen.Select(position => position.Schluessel).ToHashSet();
            var gepushtePositionen = neuePositionen
                .Select(position => $"{position.Einkaufsbeleg}/{position.Position}")
                .ToHashSet();
            var einkaufsbelege = neuePositionen.Select(position => position.Einkaufsbeleg).Distinct().ToList();
            var kandidaten = await db.Dispositionspositionen
                .Where(position => einkaufsbelege.Contains(position.Einkaufsbeleg))
                .ToListAsync(cancellationToken);
            var vorhandene = kandidaten
                .Where(position =>
                    schluessel.Contains(position.Schluessel) ||
                    (position.Einteilungsnummer is null &&
                     gepushtePositionen.Contains($"{position.Einkaufsbeleg}/{position.Position}")))
                .ToList();
            db.Dispositionspositionen.RemoveRange(vorhandene);
            db.Dispositionspositionen.AddRange(neuePositionen);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "SAP-Dispositionsliste: {Anzahl} Einteilungen importiert, {Ersetzt} ersetzt.",
                neuePositionen.Count, vorhandene.Count);
            return new SapDirektImportErgebnis(neuePositionen.Count, vorhandene.Count, importiertAm);
        }, cancellationToken);
    }

    public Task<SapDirektImportErgebnis> ImportiereLieferantenAsync(
        LieferantenDatenPush push,
        CancellationToken cancellationToken = default)
    {
        var (lieferanten, kontakte) = ErzeugeLieferanten(push);

        return InTransaktionAsync(async () =>
        {
            var kreditoren = lieferanten.Select(lieferant => lieferant.Kreditor).Distinct().ToList();
            var neueAdressNummern = lieferanten
                .Where(lieferant => lieferant.AdressNummer.HasValue)
                .Select(lieferant => lieferant.AdressNummer!.Value)
                .Distinct()
                .ToList();

            var vorhandeneLieferanten = await db.Lieferanten
                .Where(lieferant => kreditoren.Contains(lieferant.Kreditor))
                .ToListAsync(cancellationToken);
            var adressNummern = neueAdressNummern
                .Concat(vorhandeneLieferanten
                    .Where(lieferant => lieferant.AdressNummer.HasValue)
                    .Select(lieferant => lieferant.AdressNummer!.Value))
                .Distinct()
                .ToList();
            var vorhandeneKontakte = await db.LieferantEmailAdressen
                .Where(kontakt => adressNummern.Contains(kontakt.AdressNummer))
                .ToListAsync(cancellationToken);

            db.LieferantEmailAdressen.RemoveRange(vorhandeneKontakte);
            db.Lieferanten.RemoveRange(vorhandeneLieferanten);
            db.Lieferanten.AddRange(lieferanten);
            db.LieferantEmailAdressen.AddRange(kontakte);
            await db.SaveChangesAsync(cancellationToken);

            var importiertAm = DateTime.UtcNow;
            logger.LogInformation(
                "SAP-Lieferantenstamm: {Lieferanten} Lieferanten und {Kontakte} Kontakte importiert.",
                lieferanten.Count, kontakte.Count);
            return new SapDirektImportErgebnis(
                lieferanten.Count + kontakte.Count,
                vorhandeneLieferanten.Count + vorhandeneKontakte.Count,
                importiertAm);
        }, cancellationToken);
    }

    public Task<SapDirektImportErgebnis> ImportiereKundenAdressenAsync(
        KundenAdressenPush push,
        CancellationToken cancellationToken = default)
    {
        var adressen = ErzeugeKundenPartneradressen(push);

        return InTransaktionAsync(async () =>
        {
            var importiertAm = DateTime.UtcNow;
            var hauptkundennummern = adressen.Select(adresse => adresse.Hauptkundennummer).Distinct().ToList();
            var vorhandene = await db.KundenPartneradressen
                .Where(adresse => hauptkundennummern.Contains(adresse.Hauptkundennummer))
                .ToListAsync(cancellationToken);

            db.KundenPartneradressen.RemoveRange(vorhandene);
            foreach (var adresse in adressen)
                adresse.ImportiertAm = importiertAm;
            db.KundenPartneradressen.AddRange(adressen);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "SAP-Kunden-Partneradressen: {Anzahl} Adressen importiert, {Ersetzt} ersetzt.",
                adressen.Count, vorhandene.Count);
            return new SapDirektImportErgebnis(adressen.Count, vorhandene.Count, importiertAm);
        }, cancellationToken);
    }

    private static List<Dispositionsposition> ErzeugeDispositionspositionen(OffeneBestellungenPush push)
    {
        var fehler = new List<string>();
        if (push.PurchaseOrders.Count == 0)
            fehler.Add("purchaseOrders darf nicht leer sein.");

        var ergebnis = new List<Dispositionsposition>();
        foreach (var bestellung in push.PurchaseOrders)
        {
            if (string.IsNullOrWhiteSpace(bestellung.PurchaseOrderNumber))
                fehler.Add("purchaseOrderNumber fehlt.");
            if (!int.TryParse(bestellung.SupplierNumber, out var kreditor))
                fehler.Add($"supplierNumber '{bestellung.SupplierNumber}' ist keine Ganzzahl.");

            foreach (var item in bestellung.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Position))
                    fehler.Add($"Position in Bestellung {bestellung.PurchaseOrderNumber} fehlt.");
                if (string.IsNullOrWhiteSpace(item.Description))
                    fehler.Add($"description in Bestellung {bestellung.PurchaseOrderNumber}/{item.Position} fehlt.");

                foreach (var einteilung in item.ScheduleLines)
                {
                    if (string.IsNullOrWhiteSpace(einteilung.ScheduleLineNumber))
                        fehler.Add($"scheduleLineNumber in Bestellung {bestellung.PurchaseOrderNumber}/{item.Position} fehlt.");
                    if (einteilung.DeliveryDate == default)
                        fehler.Add($"deliveryDate in Bestellung {bestellung.PurchaseOrderNumber}/{item.Position} fehlt.");

                    ergebnis.Add(new Dispositionsposition
                    {
                        Einkaufsbeleg = bestellung.PurchaseOrderNumber,
                        Position = item.Position,
                        Einteilungsnummer = einteilung.ScheduleLineNumber,
                        Schluessel = $"{bestellung.PurchaseOrderNumber}/{item.Position}/{einteilung.ScheduleLineNumber}",
                        LieferantKreditor = kreditor,
                        Einkaeufergruppe = item.PurchasingGroup?.Code ?? bestellung.PurchasingGroup,
                        Belegdatum = bestellung.DocumentDate,
                        Lieferdatum = einteilung.DeliveryDate,
                        Auftragsbestaetigung = einteilung.Confirmation ?? bestellung.LabNr,
                        LabNr = bestellung.LabNr,
                        Material = item.MaterialNumber,
                        Kurztext = item.Description,
                        Werk = item.Plant,
                        EinkaeufergruppenName = item.PurchasingGroup?.Name,
                        VerantwortlichePerson = item.PurchasingGroup?.ResponsiblePerson,
                        EinkaeuferEmail = item.PurchasingGroup?.Email,
                        MaterialgruppenCode = item.MaterialGroup?.Code,
                        MaterialgruppenName = item.MaterialGroup?.Name,
                        Bestellmenge = item.OrderQuantity,
                        Bestellmengeneinheit = item.Unit,
                        Waehrung = bestellung.Currency,
                        Einteilungsmenge = einteilung.ScheduledQuantity,
                        GelieferteMenge = einteilung.DeliveredQuantity,
                        NochZuLiefernMenge = einteilung.OpenQuantity
                    });
                }
            }
        }

        if (ergebnis.Count == 0)
            fehler.Add("Der Push enthält keine scheduleLines und damit keine importierbaren Dispositionspositionen.");
        foreach (var doppelterSchluessel in ergebnis.GroupBy(position => position.Schluessel).Where(group => group.Count() > 1))
            fehler.Add($"Die Dispositions-Einteilung '{doppelterSchluessel.Key}' kommt mehrfach vor.");
        WirfBeiFehlern(fehler);
        return ergebnis;
    }

    private static (List<Lieferant> Lieferanten, List<LieferantEmailAdresse> Kontakte) ErzeugeLieferanten(
        LieferantenDatenPush push)
    {
        var fehler = new List<string>();
        if (push.Suppliers.Count == 0)
            fehler.Add("suppliers darf nicht leer sein.");

        var lieferanten = new List<Lieferant>();
        var kontakte = new List<LieferantEmailAdresse>();
        foreach (var supplier in push.Suppliers)
        {
            if (!int.TryParse(supplier.SupplierNumber, out var kreditor))
                fehler.Add($"supplierNumber '{supplier.SupplierNumber}' ist keine Ganzzahl.");
            if (string.IsNullOrWhiteSpace(supplier.Name))
                fehler.Add($"name für Lieferant '{supplier.SupplierNumber}' fehlt.");
            if (!OptionaleGanzzahl(supplier.PostalCode, out var postleitzahl))
                fehler.Add($"postalCode '{supplier.PostalCode}' ist keine Ganzzahl.");
            if (!OptionaleGanzzahl(supplier.AddressNumber, out var adressNummer))
                fehler.Add($"addressNumber '{supplier.AddressNumber}' ist keine Ganzzahl.");

            lieferanten.Add(new Lieferant
            {
                Kreditor = kreditor,
                Name = supplier.Name,
                Land = supplier.Country,
                Postleitzahl = postleitzahl,
                Ort = supplier.City,
                Strasse = supplier.Street,
                AdressNummer = adressNummer
            });

            if (supplier.Contacts.Count > 0 && adressNummer is null)
                fehler.Add($"Lieferant '{supplier.SupplierNumber}' hat Kontakte, aber keine addressNumber.");
            foreach (var contact in supplier.Contacts)
            {
                if (string.IsNullOrWhiteSpace(contact.Email))
                    fehler.Add($"Ein Kontakt von Lieferant '{supplier.SupplierNumber}' hat keine E-Mail-Adresse.");
                if (adressNummer.HasValue)
                {
                    kontakte.Add(new LieferantEmailAdresse
                    {
                        AdressNummer = adressNummer.Value,
                        EmailAdresse = contact.Email,
                        IstStandard = contact.IsDefault,
                        KontaktTyp = contact.ContactType
                    });
                }
            }
        }

        foreach (var doppelterKreditor in lieferanten.GroupBy(lieferant => lieferant.Kreditor).Where(group => group.Count() > 1))
            fehler.Add($"supplierNumber '{doppelterKreditor.Key}' kommt mehrfach vor.");

        WirfBeiFehlern(fehler);
        return (lieferanten, kontakte);
    }

    private static readonly string[] BekanntePartnerrollen =
        ["Auftraggeber", "Rechnungsempfänger", "Regulierer", "Vertretung", "Warenempfänger", "Endkunde"];

    private static List<KundenPartneradresse> ErzeugeKundenPartneradressen(KundenAdressenPush push)
    {
        var fehler = new List<string>();
        if (push.Kunden.Count == 0)
            fehler.Add("kunden darf nicht leer sein.");

        var ergebnis = new List<KundenPartneradresse>();
        foreach (var gruppe in push.Kunden)
        {
            if (string.IsNullOrWhiteSpace(gruppe.Hauptkundennummer))
                fehler.Add("hauptkundennummer fehlt.");
            if (gruppe.Adressen.Count == 0)
                fehler.Add($"Kunde '{gruppe.Hauptkundennummer}' hat keine adressen.");

            var rollenInGruppe = new HashSet<string>(StringComparer.Ordinal);
            foreach (var adresse in gruppe.Adressen)
            {
                if (!BekanntePartnerrollen.Contains(adresse.Partnerrolle))
                    fehler.Add($"partnerrolle '{adresse.Partnerrolle}' ist unbekannt.");
                else if (!rollenInGruppe.Add(adresse.Partnerrolle))
                    fehler.Add($"partnerrolle '{adresse.Partnerrolle}' kommt bei Kunde '{gruppe.Hauptkundennummer}' mehrfach vor.");

                if (string.IsNullOrWhiteSpace(adresse.PartnerId))
                    fehler.Add($"partnerId für Rolle '{adresse.Partnerrolle}' bei Kunde '{gruppe.Hauptkundennummer}' fehlt.");
                if (string.IsNullOrWhiteSpace(adresse.Name))
                    fehler.Add($"name für Rolle '{adresse.Partnerrolle}' bei Kunde '{gruppe.Hauptkundennummer}' fehlt.");

                ergebnis.Add(new KundenPartneradresse
                {
                    Hauptkundennummer = gruppe.Hauptkundennummer,
                    Partnerrolle = adresse.Partnerrolle,
                    PartnerId = adresse.PartnerId,
                    Name = adresse.Name,
                    Strasse = adresse.Strasse,
                    Plz = adresse.Plz,
                    Ort = adresse.Ort,
                    Land = adresse.Land
                });
            }
        }

        foreach (var doppelteHauptkundennummer in push.Kunden
            .GroupBy(gruppe => gruppe.Hauptkundennummer)
            .Where(group => group.Count() > 1))
            fehler.Add($"hauptkundennummer '{doppelteHauptkundennummer.Key}' kommt mehrfach vor.");

        WirfBeiFehlern(fehler);
        return ergebnis;
    }

    private static void ValidiereStueckliste(StuecklistenPush push)
    {
        var fehler = ValidiereKnoten(push.Nodes, genauEineWurzel: false);
        if (!string.Equals(push.BomType, "ORDER_BOM", StringComparison.OrdinalIgnoreCase))
            fehler.Add("bomType muss ORDER_BOM sein.");
        if (string.IsNullOrWhiteSpace(push.OrderNumber))
            fehler.Add("orderNumber fehlt.");
        if (string.IsNullOrWhiteSpace(push.OrderItem))
            fehler.Add("orderItem fehlt.");
        if (string.IsNullOrWhiteSpace(push.RootNodeId))
            fehler.Add("rootNodeId fehlt.");
        else if (!push.Nodes.Any(node => node.NodeId == push.RootNodeId && node.ParentNodeId is null))
            fehler.Add("rootNodeId verweist nicht auf einen Wurzelknoten.");
        if (push.ValidAt == default)
            fehler.Add("validAt fehlt.");
        WirfBeiFehlern(fehler);
    }

    private static void ValidiereMaximalstueckliste(MaximalstuecklistenPush push)
    {
        var fehler = ValidiereKnoten(push.Nodes, genauEineWurzel: true);
        if (!string.Equals(push.BomType, "MAXIMUM_MATERIAL_BOM", StringComparison.OrdinalIgnoreCase))
            fehler.Add("bomType muss MAXIMUM_MATERIAL_BOM sein.");
        if (string.IsNullOrWhiteSpace(push.MaterialNumber))
            fehler.Add("materialNumber fehlt.");
        if (string.IsNullOrWhiteSpace(push.Description))
            fehler.Add("description fehlt; es wird als Maschinentyp-Schlüssel verwendet.");
        if (string.IsNullOrWhiteSpace(push.Plant))
            fehler.Add("plant fehlt.");
        if (string.IsNullOrWhiteSpace(push.BomUsage))
            fehler.Add("bomUsage fehlt.");
        if (string.IsNullOrWhiteSpace(push.BomAlternative))
            fehler.Add("bomAlternative fehlt.");
        if (push.ValidAt == default)
            fehler.Add("validAt fehlt.");
        WirfBeiFehlern(fehler);
    }

    private static List<string> ValidiereKnoten(IReadOnlyList<SapBomKnoten> nodes, bool genauEineWurzel)
    {
        var fehler = new List<string>();
        if (nodes.Count == 0)
        {
            fehler.Add("nodes darf nicht leer sein.");
            return fehler;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.NodeId))
                fehler.Add("Ein Knoten hat keine nodeId.");
            else if (!ids.Add(node.NodeId))
                fehler.Add($"nodeId '{node.NodeId}' kommt mehrfach vor.");
            if (string.IsNullOrWhiteSpace(node.Type))
                fehler.Add($"type für Knoten '{node.NodeId}' fehlt.");
            if (node.Quantity < 0)
                fehler.Add($"quantity für Knoten '{node.NodeId}' darf nicht negativ sein.");
        }

        foreach (var node in nodes.Where(node => node.ParentNodeId is not null))
        {
            if (!ids.Contains(node.ParentNodeId!))
                fehler.Add($"parentNodeId '{node.ParentNodeId}' von Knoten '{node.NodeId}' existiert nicht.");
        }
        foreach (var node in nodes)
        {
            var besucht = new HashSet<string>(StringComparer.Ordinal);
            var aktuell = node;
            while (aktuell.ParentNodeId is not null)
            {
                if (!besucht.Add(aktuell.NodeId))
                {
                    fehler.Add($"Die Hierarchie enthält bei Knoten '{node.NodeId}' einen Zyklus.");
                    break;
                }
                aktuell = nodes.FirstOrDefault(kandidat => kandidat.NodeId == aktuell.ParentNodeId)!;
                if (aktuell is null)
                    break;
            }
        }
        if (genauEineWurzel && nodes.Count(node => node.ParentNodeId is null) != 1)
            fehler.Add("Die Maximalstückliste muss genau einen Wurzelknoten enthalten.");
        return fehler;
    }

    private static void WirfBeiFehlern(List<string> fehler)
    {
        if (fehler.Count > 0)
            throw new SapImportValidierungsException(fehler);
    }

    private static bool OptionaleGanzzahl(string? wert, out int? zahl)
    {
        if (string.IsNullOrWhiteSpace(wert))
        {
            zahl = null;
            return true;
        }
        if (int.TryParse(wert, out var ergebnis))
        {
            zahl = ergebnis;
            return true;
        }
        zahl = null;
        return false;
    }

    private static int Positionsnummer(string? position) =>
        int.TryParse(position, out var nummer) ? nummer : 0;

    private async Task<T> InTransaktionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational())
            return await operation();

        await using IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);
        var ergebnis = await operation();
        await transaction.CommitAsync(cancellationToken);
        return ergebnis;
    }
}
