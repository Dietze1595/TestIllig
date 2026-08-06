namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Per Document Intelligence (prebuilt-invoice) aus einem Angebot bzw. einer
/// Kundenbestellung extrahierte Felder. Null = Feld im Dokument nicht erkannt.
/// </summary>
public record ExtrahierteAngebotsdaten(
    string? Nummer,
    string? Kundenname,
    string? Kundenadresse,
    string? Zahlungsbedingungen,
    IReadOnlyList<ExtrahiertePosition> Positionen,
    // Voller erkannter Dokumenttext — Grundlage für Volltextsuche (Zuordnung) und
    // LLM-Vergleich (siehe Spec Update 2026-07-16). Default "" hält bestehende
    // 5-Parameter-Aufrufe (Tests) unverändert kompilierbar.
    string Volltext = "",
    string? ZahlungsbedingungCode = null,
    string? Zahlungsplan = null,
    string? Verkaeufer = null,
    string? Liefertermin = null,
    DateTime? GueltigBis = null,
    string? Gesamtpreis = null,
    // Separater Text der ersten Seite für die Dokumentart-Prüfung. Bei älteren Daten
    // bleibt er leer; dort kann auf den vollständigen Dokumenttext zurückgefallen werden.
    string ErsteSeiteText = "",
    // Nullable hält ältere Clients und bereits serialisierte Analysedaten kompatibel.
    // Ein Wert kann deterministisch oder durch den beleggeprüften LLM-Fallback stammen.
    string? Versandart = null,
    string? Lieferadresse = null);

public record ExtrahiertePosition(
    string Beschreibung,
    decimal? Menge,
    decimal? Einzelpreis,
    decimal? Gesamtbetrag);
