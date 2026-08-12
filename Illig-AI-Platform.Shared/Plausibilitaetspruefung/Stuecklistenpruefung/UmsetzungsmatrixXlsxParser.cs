using ClosedXML.Excel;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public record MatrixZeile(
    IReadOnlyList<string> Pfad,
    string Bedingung,
    int? PfadVorkommen = null);

/// <summary>
/// Parst eine Umsetzungsmatrix-Arbeitsmappe. Hierarchie: Tiefe k hat die Artikelnummer in
/// Spalte k+2 (1-indiziert, Spalte B = Tiefe 0). Varianten-Spalten beginnen ab Spalte N (14).
/// Enthält eine Zeile "siehe Komb." in mehr als einer Varianten-Spalte, gilt die volle
/// Bedingung aus der Spalte "Merkmalskombination" (per Kopfzeilentext gefunden, nicht per
/// fester Spaltennummer — Position kann zwischen Dateien variieren).
/// Siehe Design-Spec, Abschnitt "Umsetzungsmatrix (.xlsx)".
/// </summary>
public static class UmsetzungsmatrixXlsxParser
{
    private const int ErsteHierarchieSpalte = 2; // B
    private const int ErsteVariantenSpalte = 14; // N
    private const int HeaderZeile = 2;

    public static List<MatrixZeile> Parse(Stream xlsxStream)
    {
        using var wb = new XLWorkbook(xlsxStream);
        var ws = wb.Worksheets.First(w => w.Name.StartsWith("Umsetztabelle", StringComparison.OrdinalIgnoreCase));

        var letzteSpalte = ws.LastColumnUsed()!.ColumnNumber();
        var kombinationsSpalte = FindeKombinationsSpalte(ws, letzteSpalte);

        var pfadStapel = new List<(int Tiefe, string Artikelnummer)>();
        var naechstesVorkommenNachPfad = new Dictionary<string, int>();
        var ergebnis = new List<MatrixZeile>();

        var letzteZeile = ws.LastRowUsed()!.RowNumber();
        for (var zeile = HeaderZeile + 1; zeile <= letzteZeile; zeile++)
        {
            var tiefe = FindeHierarchieTiefe(ws, zeile, letzteSpalte);
            if (tiefe is null)
                continue; // Zeile ohne Artikelnummer (Leerzeile, Kommentarzeile o. Ä.)

            var artikelnummer = ws.Cell(zeile, ErsteHierarchieSpalte + tiefe.Value).GetString().Trim();

            while (pfadStapel.Count > 0 && pfadStapel[^1].Tiefe >= tiefe.Value)
                pfadStapel.RemoveAt(pfadStapel.Count - 1);
            pfadStapel.Add((tiefe.Value, artikelnummer));

            var pfad = pfadStapel.Select(p => p.Artikelnummer).ToList();
            var pfadSchluessel = string.Join('/', pfad);
            var pfadVorkommen = naechstesVorkommenNachPfad.GetValueOrDefault(pfadSchluessel);
            naechstesVorkommenNachPfad[pfadSchluessel] = pfadVorkommen + 1;

            var bedingung = ErmittleBedingung(ws, zeile, letzteSpalte, kombinationsSpalte);
            if (bedingung is not null)
                ergebnis.Add(new MatrixZeile(pfad, bedingung, pfadVorkommen));
        }

        return ergebnis;
    }

    private static int? FindeHierarchieTiefe(IXLWorksheet ws, int zeile, int letzteSpalte)
    {
        for (var spalte = ErsteHierarchieSpalte; spalte < Math.Min(ErsteVariantenSpalte, letzteSpalte); spalte++)
        {
            var wert = ws.Cell(zeile, spalte).GetString().Trim();
            // Artikelnummern sind immer rein numerisch — filtert Freitextzeilen wie die Legende
            // (Zeile 3) heraus, die sonst fälschlich als Hierarchie-Eintrag gelesen würden.
            if (wert.Length > 0 && wert.All(char.IsDigit))
                return spalte - ErsteHierarchieSpalte;
        }
        return null;
    }

    private static int FindeKombinationsSpalte(IXLWorksheet ws, int letzteSpalte)
    {
        for (var spalte = ErsteVariantenSpalte; spalte <= letzteSpalte; spalte++)
        {
            if (ws.Cell(HeaderZeile, spalte).GetString().Contains("Merkmalskombination", StringComparison.OrdinalIgnoreCase))
                return spalte;
        }
        return -1; // Keine Kombinationsspalte vorhanden (z. B. in der Test-Mini-Matrix) — kein Fehler.
    }

    private static string? ErmittleBedingung(IXLWorksheet ws, int zeile, int letzteSpalte, int kombinationsSpalte)
    {
        // Bis letzteSpalte (nicht bis kombinationsSpalte) laufen — sonst wird die Bedingung nie
        // gefunden, wenn keine Kombinationsspalte existiert (kombinationsSpalte == -1).
        var teilbedingungen = new List<string>();
        for (var spalte = ErsteVariantenSpalte; spalte <= letzteSpalte; spalte++)
        {
            if (spalte == kombinationsSpalte)
                continue;

            var wert = ws.Cell(zeile, spalte).GetFormattedString().Trim();
            if (wert.Length == 0)
                continue;

            if (wert.Contains("siehe Komb.", StringComparison.OrdinalIgnoreCase))
                return kombinationsSpalte > 0
                    ? ws.Cell(zeile, kombinationsSpalte).GetFormattedString().Trim()
                    : wert;

            teilbedingungen.Add(wert);
        }

        // Mehrere gefüllte Varianten-Spalten (ohne "siehe Komb.") gehören per UND zusammen, z. B.
        // Zeile 222: Formluft-Variante (Z) UND NICHT Kondenswasser (AI). Früher wurde nur die erste
        // Spalte übernommen und die restlichen Bedingungen stillschweigend verworfen.
        return teilbedingungen.Count switch
        {
            0 => null,
            1 => teilbedingungen[0],
            _ => string.Join(" U ", teilbedingungen.Select(t => $"({t})"))
        };
    }
}
