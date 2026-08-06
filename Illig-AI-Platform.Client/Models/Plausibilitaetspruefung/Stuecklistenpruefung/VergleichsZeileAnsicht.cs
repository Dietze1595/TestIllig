namespace Illig_AI_Platform.Client.Models.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Ein Eintrag in der flachen, nur-sichtbaren Liste für die virtualisierte Baum-Anzeige.
/// HatSichtbareKinder wird sowohl für die Chevron-Anzeige als auch für die
/// Sticky-Path-Berechnung gebraucht (entspricht dem früheren data-tree-group-Attribut,
/// das per DOM-Scan ermittelt wurde). IstAufgeklappt legt fest, ob die Kind-Zeilen dieses
/// Knotens direkt danach in der flachen Liste folgen.
/// </summary>
public sealed record VergleichsZeileAnsicht(VergleichsKnotenAnsicht Knoten, int Tiefe, bool HatSichtbareKinder, bool IstAufgeklappt);
