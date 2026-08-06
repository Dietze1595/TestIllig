namespace Illig_AI_Platform.Client.Models.Plausibilitaetspruefung.Sondermerkmalsuche;

// Client-eigene DTOs für die Sondermerkmalsuche — bewusst ohne Shared/EF-Abhängigkeit
// (vgl. UserProfileDto). Spiegeln die Shape der Server-DTOs; JSON-(De)Serialisierung
// läuft über die Eigenschaftsnamen.

/// <summary>Ein aus einer Stückliste erkanntes Sondermerkmal.</summary>
public record Sondermerkmal(
    string Position,
    string Merkmalsnummer,
    string Beschreibung,
    string Quelle);

/// <summary>Eine Referenz-Stückliste mit Überschneidung zum Originalauftrag (Step 3).</summary>
public record ReferenzTreffer(
    string Auftragsnummer,
    string Maschinentyp,
    DateOnly Abschlussdatum,
    IReadOnlyList<string> UeberschneidendeMerkmalsnummern,
    int TrefferAnzahl,
    int Gesamtanzahl,
    IReadOnlyList<string> AktuellsteMerkmalsnummern,
    string Kundennummer = "",
    string? Kundenname = null);

public record StuecklisteDetailMerkmal(
    string Merkmalsnummer,
    string Beschreibung,
    string Referenzauftrag,
    string Quelle);

/// <summary>Detailansicht einer Referenz-Stückliste (Drawer in Step 3).</summary>
public record StuecklisteDetail(
    string Auftragsnummer,
    string Maschinentyp,
    DateOnly Abschlussdatum,
    string? Kundenname,
    IReadOnlyList<StuecklisteDetailMerkmal> Merkmale);

public record ReferenzDokument(string Dateiname, byte[] Inhalt);

/// <summary>Suchanfrage für Step 2→3 (Merkmalsnummern + Quellauftrag zum Ausschluss).</summary>
public record SearchRequest(
    string? QuellAuftragsnummer,
    IReadOnlyList<string> Merkmalsnummern);

/// <summary>Analyse-Ergebnis (Step 1→2): globale Stücklisten-Infos + erkannte Sondermerkmale.</summary>
public record StuecklisteAnalyse(
    string Auftragsnummer,
    string Maschinentyp,
    DateOnly Datum,
    string Kundennummer,
    string? Kundenname,
    IReadOnlyList<Sondermerkmal> Sondermerkmale);
