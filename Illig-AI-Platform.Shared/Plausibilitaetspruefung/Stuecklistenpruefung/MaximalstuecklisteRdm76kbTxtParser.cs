using System.Globalization;
using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// TXT-Parser für das <b>RDM76Kb-Format</b> des SAP-Mehrstufen-Stückliste-Exports (ISO-8859-1,
/// tab-getrennt). Das Spaltenlayout entspricht dem RDM75-Format (Kurztext = 17, Menge = 18,
/// Komp.-ME = 22, Hierarchie 0–16); die Logik ist identisch zu
/// <see cref="MaximalstuecklisteTxtParser"/>. Bewusst eigene Klasse (nicht parametrisiert), damit
/// RDM76Kb-spezifische Anpassungen die anderen Formate nicht berühren. Übergangslösung, bis ILLIG
/// einen standardisierten Export liefert.
/// </summary>
public static partial class MaximalstuecklisteRdm76kbTxtParser
{
    private const int ErsteHierarchieSpalte = 0;
    private const int LetzteHierarchieSpalte = 16;
    private const int KurztextSpalte = 17;
    private const int MengeSpalte = 18;
    private const int EinheitSpalte = 22;

    [GeneratedRegex(@"^(\d{4})\s[A-Z](?:\s+(\d+))?$")]
    private static partial Regex PositionsToken();

    [GeneratedRegex(@"^\S+\s+\d+\s+\d+\s+\d+$|^\S+\s*/\s*\d+\s+\S+\s+\d+$")]
    private static partial Regex WurzelToken();

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
            var mengeRoh = SpalteOderLeer(felder, MengeSpalte);
            var token = felder[hierarchieSpalte.Value].Trim();
            if (!TryParseMenge(mengeRoh, out var menge))
            {
                if (!istWurzelzeile)
                    continue; // Selbstdeklaration einer bereits referenzierten Baugruppe — kein eigener Knoten.
                if (!WurzelToken().IsMatch(token))
                    continue; // Titel-/Kopfzeile vor der eigentlichen Wurzel, keine echte Position.
                menge = 0m; // Die Wurzelzeile selbst hat wie eine Selbstdeklaration keine Menge.
            }

            var artikelnummer = istWurzelzeile ? token.Split(' ')[0] : ExtrahiereArtikelnummer(token);
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
        for (var spalte = ErsteHierarchieSpalte; spalte <= LetzteHierarchieSpalte && spalte < felder.Length; spalte++)
        {
            if (!string.IsNullOrWhiteSpace(felder[spalte]))
                return spalte;
        }
        return null;
    }

    private static string SpalteOderLeer(string[] felder, int spalte) =>
        spalte < felder.Length ? felder[spalte] : "";

    private static bool TryParseMenge(string roh, out decimal menge) =>
        decimal.TryParse(roh.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out menge);

    private static string ExtrahiereArtikelnummer(string token)
    {
        var match = PositionsToken().Match(token);
        if (!match.Success)
            return token; // Wurzelzeile hat kein "<Pos> <Typ>"-Format, sondern direkt die Artikelnummer.
        return match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value;
    }
}
