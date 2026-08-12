using ClosedXML.Excel;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Parser für das <b>RDM76Kb-Format</b> der Umsetzungsmatrix (Übergangslösung). Gegenüber
/// RDM75 (<see cref="UmsetzungsmatrixXlsxParser"/>) und RDM73K
/// (<see cref="UmsetzungsmatrixRdm73kXlsxParser"/>) abweichend:
/// <list type="bullet">
/// <item>Varianten-Spalten beginnen ab Spalte 13 (M) statt 14; Varianten-Kopf in Zeile 3,
/// "Merkmalskombination" ebenfalls in Zeile 3.</item>
/// <item>Hierarchie nur bis Spalte 9 (Artikelnummern in B–G, max. Tiefe 5). Die Spalten 10–12
/// tragen Z-Zeichnung, Materialnummer (Spalte 11 = reine Zahlen!) und Baugruppenkenner und
/// dürfen NICHT als Hierarchie gelesen werden — sonst entstünden hunderte Geister-Knoten.</item>
/// <item><b>Durchgestrichene</b> Artikelnummern sind ersetzte Alt-Nummern und werden bei der
/// Hierarchie-Erkennung übersprungen; gültig ist die nicht durchgestrichene Nummer daneben.</item>
/// </list>
/// Bewusst eigene Klasse (nicht parametrisiert), damit Anpassungen die anderen Formate nicht berühren.
/// </summary>
public static class UmsetzungsmatrixRdm76kbXlsxParser
{
    private const int ErsteHierarchieSpalte = 2;  // B
    private const int LetzteHierarchieSpalte = 9;  // I — davor endet die Hierarchie (Spalte 10+ = Metadaten)
    private const int ErsteVariantenSpalte = 13;   // M
    private const int HeaderZeile = 3;
    private const string StandardstromMerkmal = "9020016";

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
            var tiefe = FindeHierarchieTiefe(ws, zeile);
            if (tiefe is null)
                continue; // Zeile ohne (gültige) Artikelnummer.

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

    private static int? FindeHierarchieTiefe(IXLWorksheet ws, int zeile)
    {
        for (var spalte = ErsteHierarchieSpalte; spalte <= LetzteHierarchieSpalte; spalte++)
        {
            var zelle = ws.Cell(zeile, spalte);
            // Durchgestrichene Zellen sind ersetzte Alt-Nummern — überspringen, damit die gültige
            // Artikelnummer daneben die Hierarchie-Tiefe bestimmt.
            if (zelle.Style.Font.Strikethrough)
                continue;

            var wert = zelle.GetString().Trim();
            // Artikelnummern sind rein numerisch — filtert Freitext-/Kennerspalten heraus.
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
        return -1; // Keine Kombinationsspalte (z. B. in der Test-Mini-Matrix) — kein Fehler.
    }

    private static string? ErmittleBedingung(IXLWorksheet ws, int zeile, int letzteSpalte, int kombinationsSpalte)
    {
        // Alle gefüllten Varianten-Spalten (außer der Kombinationsspalte selbst) einsammeln.
        var teilbedingungen = new List<string>();
        for (var spalte = ErsteVariantenSpalte; spalte <= letzteSpalte; spalte++)
        {
            if (spalte == kombinationsSpalte)
                continue;

            var wert = ws.Cell(zeile, spalte).GetFormattedString().Trim();
            if (wert.Length == 0)
                continue;

            var normalisiert = NormalisiereZellenwert(ws, spalte, wert);
            if (normalisiert is not null)
                teilbedingungen.Add(normalisiert);
        }

        if (teilbedingungen.Count == 0)
            return null;

        // Ist die Merkmalskombinationsspalte gefüllt, ist SIE die maßgebliche, vollständige Bedingung
        // der Zeile — die Einzelspalten markieren dann nur die beteiligten Varianten-Gruppen (teils als
        // "siehe Komb."/"s. Kombi"/"s.Komb."-Verweis oder Mischzelle "022689 s.Komb.", teils mit den
        // Rohwerten). Das deckt die uneinheitlichen Marker-Schreibweisen ohne String-Erkennung ab.
        if (kombinationsSpalte > 0)
        {
            var kombi = ws.Cell(zeile, kombinationsSpalte).GetFormattedString().Trim();
            if (kombi.Length > 0)
            {
                var normalisierteKombi = NormalisiereZellenwert(ws, kombinationsSpalte, kombi);
                if (!string.IsNullOrEmpty(normalisierteKombi))
                    return normalisierteKombi;
            }
        }

        // Sonst: mehrere gefüllte Spalten gehören per UND zusammen (z. B. Zeile 208: Formluft-Variante
        // UND NICHT Kondenswasser). Früher wurde nur die erste Spalte übernommen und der Rest verworfen.
        return teilbedingungen.Count == 1
            ? teilbedingungen[0]
            : string.Join(" U ", teilbedingungen.Select(t => $"({t})"));
    }

    private static string? NormalisiereZellenwert(IXLWorksheet ws, int spalte, string wert)
    {
        if (IstMaschinenhinweis(wert))
            return null;

        if (string.Equals(wert, "x", StringComparison.OrdinalIgnoreCase)
            && KopfEnthaeltMerkmal(ws, spalte, StandardstromMerkmal))
            return $"N{StandardstromMerkmal}";

        return wert;
    }

    private static bool KopfEnthaeltMerkmal(IXLWorksheet ws, int spalte, string merkmal) =>
        ws.Cell(HeaderZeile, spalte)
            .GetFormattedString()
            .Contains(merkmal, StringComparison.Ordinal);

    private static bool IstMaschinenhinweis(string wert)
    {
        var normalisiert = string.Concat(wert.Where(char.IsLetterOrDigit)).ToUpperInvariant();
        return normalisiert is
            "75KC" or "75KD" or "76K" or "76KB" or
            "RDM75KC" or "RDM75KD" or "RDM76K" or "RDM76KB";
    }
}
