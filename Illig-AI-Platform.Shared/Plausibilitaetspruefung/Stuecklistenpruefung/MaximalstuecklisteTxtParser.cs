using System.Globalization;
using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public record RohPosition(
    string Artikelnummer,
    string Bezeichnung,
    decimal Menge,
    string Einheit,
    List<RohPosition> Kinder);

/// <summary>
/// Parst den SAP-Mehrstufen-Stückliste-Export (ISO-8859-1, tab-getrennt). Kurztext/Menge/
/// Komp.-ME stehen an festen Tab-Spalten (17/18/22), unabhängig von der Baumtiefe — die
/// ergibt sich daraus, welche der Spalten 0-16 den Positions-/Typ-Token trägt. Zeilen ohne
/// Menge (Spalte 18 leer) sind Selbstdeklarationen einer bereits referenzierten Baugruppe
/// und werden übersprungen, nicht als eigener Knoten aufgenommen — <b>außer</b> die allererste
/// solche Zeile, die (mangels vorheriger Referenz) die Wurzel des Baums selbst ist und trotz
/// fehlender Menge einen Knoten erzeugt. Siehe Design-Spec, Abschnitt
/// "Maximalstückliste (SAP-Export, .txt)".
/// </summary>
public static partial class MaximalstuecklisteTxtParser
{
    private const int ErsteHierarchieSpalte = 0;
    private const int LetzteHierarchieSpalte = 16;
    private const int KurztextSpalte = 17;
    private const int MengeSpalte = 18;
    private const int EinheitSpalte = 22;

    // Positions-/Typ-Token: "0100 L 9237787" (mit Artikelnummer) oder "0010 T" (ohne, reine
    // Text-/Titelzeile — bekommt als Ersatz-Artikelnummer die Positionsnummer selbst).
    [GeneratedRegex(@"^(\d{4})\s[A-Z](?:\s+(\d+))?$")]
    private static partial Regex PositionsToken();

    // Wurzel-/Selbstdeklarationstoken, zwei beobachtete Formen: "<Artikelnummer> 0001 1 01"
    // (Maximalstückliste) oder "<Auftragsnr> / <Position> <Artikelnummer> <Menge>" (auftrags-
    // bezogene Stückliste, z. B. "11055894 / 40 9209307 1"). Grenzt echte Wurzelzeilen von der
    // Titel- ("Produktstruktur: Gültigkeitsdatum ...") bzw. Kopfzeile
    // ("Produktstruktur"-Spaltenüberschrift) am Dateianfang ab, die sonst mangels vorheriger
    // Referenz fälschlich als Wurzel durchgehen würden.
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
                    continue;
                if (!WurzelToken().IsMatch(token))
                    continue;
                menge = 0m;
            }

            // Wurzelzeile: Format "<Artikelnummer> 0001 1 01", keine "<Pos> <Typ>"-Struktur.
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
            return token;
        return match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value;
    }
}
