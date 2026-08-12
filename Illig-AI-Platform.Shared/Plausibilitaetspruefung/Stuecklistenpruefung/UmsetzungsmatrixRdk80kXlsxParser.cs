using ClosedXML.Excel;
using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Parst das erste Arbeitsblatt der RDK80-Umsetzungsmatrix. Die Hierarchie steht in
/// den Spalten L bis Q; die Merkmalsbedingungen beginnen in Spalte W.
/// </summary>
public static partial class UmsetzungsmatrixRdk80kXlsxParser
{
    private const string ErwarteterBlattname = "Umsetzmatrix_V05_V06";
    private const string Grundmaschine = "9209425";
    private const int ErsteHierarchieSpalte = 12; // L
    private const int LetzteHierarchieSpalte = 17; // Q
    private const int Rdk80Spalte = 21; // U
    private const int Rdkp72Spalte = 22; // V
    private const int ErsteBedingungsspalte = 23; // W
    private const int LetzteBedingungsspalte = 85; // CG

    private enum Abschnitt
    {
        Kopf,
        Rdk80,
        Rdkp72,
        Gemeinsam
    }

    private sealed record HierarchieEintrag(int Tiefe, string Artikelnummer, bool GiltFuerRdk80);

    public static List<MatrixZeile> Parse(Stream xlsxStream)
    {
        using var arbeitsmappe = new XLWorkbook(xlsxStream);
        if (arbeitsmappe.Worksheets.Count == 0 ||
            !string.Equals(arbeitsmappe.Worksheet(1).Name, ErwarteterBlattname, StringComparison.Ordinal))
        {
            throw new FormatException(
                $"Das erste Tabellenblatt der RDK80-Umsetzungsmatrix muss '{ErwarteterBlattname}' heißen.");
        }

        var arbeitsblatt = arbeitsmappe.Worksheet(1);
        var letzteZeile = arbeitsblatt.LastRowUsed()?.RowNumber() ?? 0;
        var abschnitt = Abschnitt.Kopf;
        var rdk80AbschnittGefunden = false;
        var pfadStapel = new List<HierarchieEintrag>();
        var naechstesVorkommenNachPfad = new Dictionary<string, int>();
        var ergebnis = new List<MatrixZeile>();

        for (var zeile = 1; zeile <= letzteZeile; zeile++)
        {
            var neuerAbschnitt = ErmittleAbschnitt(arbeitsblatt, zeile);
            if (neuerAbschnitt is not null)
            {
                abschnitt = neuerAbschnitt.Value;
                rdk80AbschnittGefunden |= abschnitt == Abschnitt.Rdk80;
                pfadStapel.Clear();
                continue;
            }

            if (abschnitt is Abschnitt.Kopf or Abschnitt.Rdkp72)
                continue;

            var hierarchie = ErmittleHierarchie(arbeitsblatt, zeile);
            if (hierarchie is null)
                continue;

            while (pfadStapel.Count > 0 && pfadStapel[^1].Tiefe >= hierarchie.Value.Tiefe)
                pfadStapel.RemoveAt(pfadStapel.Count - 1);

            var elternGelten = pfadStapel.Count == 0 || pfadStapel[^1].GiltFuerRdk80;
            var zeileGilt = abschnitt == Abschnitt.Rdk80 || GiltImGemeinsamenAbschnitt(arbeitsblatt, zeile);
            var giltFuerRdk80 = elternGelten && zeileGilt;
            pfadStapel.Add(new HierarchieEintrag(
                hierarchie.Value.Tiefe,
                hierarchie.Value.Artikelnummer,
                giltFuerRdk80));

            if (!giltFuerRdk80)
                continue;

            var pfad = new List<string> { Grundmaschine };
            pfad.AddRange(pfadStapel.Select(eintrag => eintrag.Artikelnummer));

            // Vorkommen werden auch für reine Strukturzeilen gezählt. Dadurch bleibt die
            // Zuordnung bei mehrfach vorkommenden Pfaden deckungsgleich mit der Stückliste.
            var pfadSchluessel = string.Join('/', pfad);
            var pfadVorkommen = naechstesVorkommenNachPfad.GetValueOrDefault(pfadSchluessel);
            naechstesVorkommenNachPfad[pfadSchluessel] = pfadVorkommen + 1;

            var bedingung = ErmittleBedingung(arbeitsblatt, zeile);
            if (bedingung is not null)
                ergebnis.Add(new MatrixZeile(pfad, bedingung, pfadVorkommen));
        }

        if (!rdk80AbschnittGefunden)
            throw new FormatException("Die Umsetzungsmatrix enthält keinen RDK80-Abschnitt.");

        return ergebnis;
    }

    private static Abschnitt? ErmittleAbschnitt(IXLWorksheet arbeitsblatt, int zeile)
    {
        var text = arbeitsblatt.Cell(zeile, ErsteHierarchieSpalte).GetString().Trim();
        if (!text.Contains("gelten", StringComparison.OrdinalIgnoreCase))
            return null;

        var enthaeltRdkp72 = text.Contains("RDKP 72", StringComparison.OrdinalIgnoreCase);
        var enthaeltRdk80 = text.Contains("RDK 80", StringComparison.OrdinalIgnoreCase);

        if (enthaeltRdkp72 && enthaeltRdk80)
            return Abschnitt.Gemeinsam;
        if (enthaeltRdkp72)
            return Abschnitt.Rdkp72;
        if (enthaeltRdk80)
            return Abschnitt.Rdk80;

        return null;
    }

    private static (int Tiefe, string Artikelnummer)? ErmittleHierarchie(
        IXLWorksheet arbeitsblatt,
        int zeile)
    {
        for (var spalte = ErsteHierarchieSpalte; spalte <= LetzteHierarchieSpalte; spalte++)
        {
            var zelle = arbeitsblatt.Cell(zeile, spalte);
            var artikelnummer = zelle.GetString().Trim();
            if (!zelle.Style.Font.Strikethrough && artikelnummer.Length > 0 && artikelnummer.All(char.IsDigit))
                return (spalte - ErsteHierarchieSpalte, artikelnummer);
        }

        return null;
    }

    private static bool GiltImGemeinsamenAbschnitt(IXLWorksheet arbeitsblatt, int zeile)
    {
        var rdk80 = arbeitsblatt.Cell(zeile, Rdk80Spalte).GetString().Trim();
        var rdkp72 = arbeitsblatt.Cell(zeile, Rdkp72Spalte).GetString().Trim();
        return rdk80 != "-" && (rdk80.Length > 0 || rdkp72.Length == 0);
    }

    private static string? ErmittleBedingung(IXLWorksheet arbeitsblatt, int zeile)
    {
        var undGruppen = new List<List<string>>();
        var naechsteBedingungIstOder = false;

        for (var spalte = ErsteBedingungsspalte; spalte <= LetzteBedingungsspalte; spalte++)
        {
            var wert = arbeitsblatt.Cell(zeile, spalte).GetFormattedString().Trim();
            if (wert.Length == 0)
                continue;

            var aufgeloest = LoeseSondermarkerAuf(arbeitsblatt, spalte, wert);
            if (aufgeloest is null)
                continue;

            var normalisiert = NormalisiereMerkmalsausdruck(arbeitsblatt, spalte, aufgeloest);
            if (normalisiert is null)
                continue;

            var beginntMitOder = normalisiert.StartsWith('/');
            var endetMitOder = normalisiert.EndsWith('/');
            normalisiert = normalisiert.Trim(' ', '/');

            if (normalisiert.Length == 0)
            {
                naechsteBedingungIstOder = true;
                continue;
            }

            normalisiert = SchraegstrichMitLeerraum().Replace(normalisiert, " / ");
            var mitVorherigerGruppeVerbinden =
                (naechsteBedingungIstOder || beginntMitOder) && undGruppen.Count > 0;

            if (mitVorherigerGruppeVerbinden)
                undGruppen[^1].Add(normalisiert);
            else
                undGruppen.Add([normalisiert]);

            naechsteBedingungIstOder = endetMitOder;
        }

        var gerenderteGruppen = undGruppen
            .Select(gruppe => string.Join(" / ", gruppe))
            .ToList();

        return gerenderteGruppen.Count switch
        {
            0 => null,
            1 => gerenderteGruppen[0],
            _ => string.Join(" U ", gerenderteGruppen.Select(gruppe => $"({gruppe})"))
        };
    }

    private static string? LoeseSondermarkerAuf(
        IXLWorksheet arbeitsblatt,
        int spalte,
        string wert)
    {
        if (string.Equals(wert, "XXXXX", StringComparison.OrdinalIgnoreCase))
            return null;

        var istNegation = string.Equals(wert, "N", StringComparison.OrdinalIgnoreCase);
        var verweistAufKopf =
            istNegation ||
            string.Equals(wert, "J", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(wert, "x", StringComparison.OrdinalIgnoreCase) ||
            wert is "!!" or "!!!";

        if (!verweistAufKopf)
            return wert;

        var ueberschrift = arbeitsblatt.Cell(2, spalte).GetFormattedString();
        var merkmalsnummern = MerkmalsnummerInUeberschrift()
            .Matches(ueberschrift)
            .Select(treffer => treffer.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (merkmalsnummern.Count == 0)
            return null;

        if (!istNegation)
            return string.Join(" / ", merkmalsnummern);

        return merkmalsnummern.Count == 1
            ? $"N{merkmalsnummern[0]}"
            : $"N ({string.Join(" / ", merkmalsnummern)})";
    }

    private static string? NormalisiereMerkmalsausdruck(
        IXLWorksheet arbeitsblatt,
        int spalte,
        string ausdruck)
    {
        var normalisiert = OderWort().Replace(ausdruck, "/");
        normalisiert = OderAbkuerzung().Replace(normalisiert, "/");
        normalisiert = UndWort().Replace(normalisiert, " U ");
        normalisiert = UndAbkuerzung().Replace(normalisiert, " U ");
        normalisiert = NichtWort().Replace(normalisiert, " N ");

        var kopfMerkmale = MerkmalsnummerInUeberschrift()
            .Matches(arbeitsblatt.Cell(2, spalte).GetFormattedString())
            .Select(treffer => treffer.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var tokens = BedingungsToken()
            .Matches(normalisiert)
            .Select(treffer => NormalisiereToken(treffer.Value, kopfMerkmale))
            .ToList();

        // Nicht auswertbarer Beschreibungstext wird bei der Tokenisierung verworfen. Dadurch
        // können UND-Fragmente direkt vor einem ODER, einer schließenden Klammer oder dem
        // Ausdrucksende übrig bleiben; diese redaktionellen Fragmente tragen keine Bedingung.
        tokens = tokens
            .Where((token, index) => !IstUngueltigesUnd(tokens, index, token))
            .ToList();

        if (tokens.Count == 0)
            return null;

        return string.Join(' ', tokens)
            .Replace("( ", "(", StringComparison.Ordinal)
            .Replace(" )", ")", StringComparison.Ordinal)
            .Trim();
    }

    private static string NormalisiereToken(string token, IReadOnlyList<string> kopfMerkmale)
    {
        var hatNichtPraefix = token.Length > 1 &&
            token.StartsWith('N') &&
            char.IsDigit(token[1]);
        var nummer = hatNichtPraefix ? token[1..] : token;
        if (!nummer.All(char.IsDigit))
            return token.ToUpperInvariant();

        if (nummer.Length == 5)
        {
            var passendeKopfMerkmale = kopfMerkmale
                .Where(merkmal => merkmal.EndsWith(nummer, StringComparison.Ordinal))
                .ToList();
            nummer = passendeKopfMerkmale.Count == 1
                ? passendeKopfMerkmale[0]
                : nummer.PadLeft(6, '0');
        }

        return hatNichtPraefix ? $"N{nummer}" : nummer;
    }

    private static bool IstUngueltigesUnd(IReadOnlyList<string> tokens, int index, string token)
    {
        if (token != "U")
            return false;

        if (index == 0 || index == tokens.Count - 1)
            return true;

        return tokens[index - 1] is "U" or "/" or "(" ||
               tokens[index + 1] is "U" or "/" or ")";
    }

    [GeneratedRegex(@"\boder\b", RegexOptions.IgnoreCase)]
    private static partial Regex OderWort();

    [GeneratedRegex(@"\bund\b", RegexOptions.IgnoreCase)]
    private static partial Regex UndWort();

    [GeneratedRegex(@"\bnicht\b", RegexOptions.IgnoreCase)]
    private static partial Regex NichtWort();

    [GeneratedRegex(@"\bo\b\.?", RegexOptions.IgnoreCase)]
    private static partial Regex OderAbkuerzung();

    [GeneratedRegex(@"\bu\b\.?", RegexOptions.IgnoreCase)]
    private static partial Regex UndAbkuerzung();

    [GeneratedRegex(@"\s*/\s*")]
    private static partial Regex SchraegstrichMitLeerraum();

    [GeneratedRegex(@"(?<!\d)\d{6,7}(?!\d)")]
    private static partial Regex MerkmalsnummerInUeberschrift();

    [GeneratedRegex(@"(?<!\d)N?\d{5,7}(?!\d)|N(?=\s*(?:\(|\d))|[()/]|(?<![A-Za-z])U(?![A-Za-z])", RegexOptions.IgnoreCase)]
    private static partial Regex BedingungsToken();
}
