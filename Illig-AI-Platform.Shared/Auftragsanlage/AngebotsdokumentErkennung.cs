using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Erkennt das einheitliche ILLIG-Angebotslayout anhand der stabilen Kennzeichen
/// auf der ersten Dokumentseite: der Titelzeile ("Angebot"/"Quotation") und einem
/// zweiten Angebotsmerkmal (Angebotsnummer-Beschriftung bzw. bereits extrahierte Nummer).
/// </summary>
public static partial class AngebotsdokumentErkennung
{
    public static bool IstAngebot(ExtrahierteAngebotsdaten daten)
    {
        var ersteSeite = string.IsNullOrWhiteSpace(daten.ErsteSeiteText)
            ? daten.Volltext
            : daten.ErsteSeiteText;

        // Die Titelzeile ("Angebot"/"Quotation") als eigenständige Zeile ist das eigentliche
        // Unterscheidungsmerkmal gegenüber Bestellung/Rechnung/Auftragsbestätigung. Sie ist
        // bewusst verankert (^…$), damit eine bloße Bezugnahme ("Bezug: Angebot 50209936")
        // nicht fälschlich als Angebot durchgeht.
        if (!AngebotsTitel().IsMatch(ersteSeite))
            return false;

        // Zweitmerkmal zur Absicherung: die Angebotsnummer-Beschriftung ODER eine bereits aus
        // dem Volltext extrahierte Nummer. Document Intelligence gibt den Doppelpunkt der
        // Beschriftung wegen Tabellenlayout/Grundlinienversatz nicht zuverlässig direkt an der
        // Beschriftung aus – deshalb ist er hier, wie im AngebotsLayoutParser, optional. Die
        // bereits extrahierte Nummer deckt zusätzlich Fälle ab, in denen die Beschriftung selbst
        // vom Layout-Modell zerlegt wurde.
        return AngebotsnummerFeld().IsMatch(ersteSeite)
            || !string.IsNullOrWhiteSpace(daten.Nummer);
    }

    [GeneratedRegex(
        @"^\s*(?:Angebot|Quotation)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex AngebotsTitel();

    // Beschriftung der Angebotsnummer; Doppelpunkt und nachgestellter Punkt sind optional
    // (siehe IstAngebot). Das Wortgrenzen-Anker verhindert Fehltreffer wie "quotation now".
    [GeneratedRegex(
        @"\b(?:Angebotsnr|Angebotsnummer|quotation\s+no)\b\.?\s*:?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AngebotsnummerFeld();
}
