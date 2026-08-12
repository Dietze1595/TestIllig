using System.Globalization;
using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Parst den RDK80k-SAP-Mehrstufen-Stücklistenexport. Die Hierarchie steht in den Spalten
/// 0–18, Kurztext/Menge/Einheit in 19/20/24. Die eigene Klasse hält RDK80-spezifische
/// Tokenformen von den bestehenden Maschinenformaten getrennt.
/// </summary>
public static partial class MaximalstuecklisteRdk80kTxtParser
{
    private const int ErsteHierarchieSpalte = 0;
    private const int LetzteHierarchieSpalte = 18;
    private const int KurztextSpalte = 19;
    private const int MengeSpalte = 20;
    private const int EinheitSpalte = 24;

    [GeneratedRegex(@"^(\d{4})\s+[A-Z](?:\s+(.+))?$")]
    private static partial Regex PositionsToken();

    [GeneratedRegex(@"^\S+\s+\d+\s+\d+\s+\d+$|^\S+\s*/\s*\d+\s+\S+\s+\d+$")]
    private static partial Regex WurzelToken();

    [GeneratedRegex(@"^\S+\s*/\s*\d+\s+(\S+)\s+\d+$")]
    private static partial Regex AuftragsWurzelToken();

    public static RohPosition Parse(string inhalt)
    {
        var zeilen = inhalt.Replace("\r\n", "\n").Split('\n');
        RohPosition? wurzel = null;
        var stapel = new List<(int Spalte, RohPosition Position)>();

        foreach (var rohZeile in zeilen)
        {
            if (string.IsNullOrWhiteSpace(rohZeile))
                continue;

            var felder = rohZeile.Split('\t');
            var hierarchieSpalte = FindeHierarchieSpalte(felder);
            if (hierarchieSpalte is null)
                continue;

            var istWurzelzeile = wurzel is null;
            var token = felder[hierarchieSpalte.Value].Trim();
            if (!TryParseMenge(SpalteOderLeer(felder, MengeSpalte), out var menge))
            {
                if (!istWurzelzeile)
                    continue;
                if (!WurzelToken().IsMatch(token))
                    continue;
                menge = 0m;
            }

            var artikelnummer = istWurzelzeile
                ? ExtrahiereWurzelArtikelnummer(token)
                : ExtrahiereArtikelnummer(token);
            var position = new RohPosition(
                artikelnummer,
                SpalteOderLeer(felder, KurztextSpalte).Trim(),
                menge,
                SpalteOderLeer(felder, EinheitSpalte).Trim(),
                []);

            while (stapel.Count > 0 && stapel[^1].Spalte >= hierarchieSpalte.Value)
                stapel.RemoveAt(stapel.Count - 1);

            if (stapel.Count > 0)
                stapel[^1].Position.Kinder.Add(position);
            wurzel ??= position;
            stapel.Add((hierarchieSpalte.Value, position));
        }

        return wurzel ?? throw new FormatException("Keine Wurzelposition gefunden.");
    }

    private static int? FindeHierarchieSpalte(string[] felder)
    {
        for (var spalte = ErsteHierarchieSpalte;
             spalte <= LetzteHierarchieSpalte && spalte < felder.Length;
             spalte++)
        {
            if (!string.IsNullOrWhiteSpace(felder[spalte]))
                return spalte;
        }

        return null;
    }

    private static string SpalteOderLeer(string[] felder, int spalte) =>
        spalte < felder.Length ? felder[spalte] : "";

    private static bool TryParseMenge(string roh, out decimal menge) =>
        decimal.TryParse(
            roh.Trim(),
            NumberStyles.Number,
            CultureInfo.GetCultureInfo("de-DE"),
            out menge);

    private static string ExtrahiereArtikelnummer(string token)
    {
        var match = PositionsToken().Match(token);
        if (!match.Success)
            return token;
        return match.Groups[2].Success
            ? match.Groups[2].Value.Trim()
            : match.Groups[1].Value;
    }

    private static string ExtrahiereWurzelArtikelnummer(string token)
    {
        var auftragsWurzel = AuftragsWurzelToken().Match(token);
        return auftragsWurzel.Success
            ? auftragsWurzel.Groups[1].Value
            : token.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }
}
