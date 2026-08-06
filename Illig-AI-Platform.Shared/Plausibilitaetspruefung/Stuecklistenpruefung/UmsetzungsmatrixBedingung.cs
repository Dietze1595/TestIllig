using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Interpretiert Bedingungsausdrücke aus der Umsetzungsmatrix (Merkmalsnummer-Kombinationen,
/// die über Aufnahme einer Stücklistenposition entscheiden). Beobachtete Notation: "/"/"oder"
/// (ODER), "U"/"und" (UND), "N"/"nicht" (NICHT, Präfix), runde Klammern zur Gruppierung.
/// Siehe docs/superpowers/specs/2026-07-08-stuecklistenpruefung-umsetzungsmatrix-design.md.
/// </summary>
public sealed partial class UmsetzungsmatrixBedingung
{
    private readonly Func<IReadOnlyCollection<string>, bool> _auswerten;

    private UmsetzungsmatrixBedingung(Func<IReadOnlyCollection<string>, bool> auswerten) =>
        _auswerten = auswerten;

    public bool Erfuellt(IReadOnlyCollection<string> merkmalsnummern) => _auswerten(merkmalsnummern);

    public static UmsetzungsmatrixBedingung Parse(string ausdruck)
    {
        if (string.IsNullOrWhiteSpace(ausdruck))
            throw new ArgumentException("Bedingungsausdruck darf nicht leer sein.", nameof(ausdruck));

        var tokens = Tokenisieren(ausdruck);
        var position = 0;
        var auswerten = ParseOder(tokens, ref position);

        if (position != tokens.Count)
            throw new ArgumentException($"Unerwartetes Token am Ende: '{ausdruck}'.", nameof(ausdruck));

        return new UmsetzungsmatrixBedingung(auswerten);
    }

    [GeneratedRegex(@"\(|\)|[^\s()]+")]
    private static partial Regex TokenPattern();

    private static List<string> Tokenisieren(string ausdruck)
    {
        // "/" kommt in der kompakten Notation ohne umgebende Leerzeichen vor — zur Sicherheit
        // wird der Operator vor der Tokenisierung mit Leerzeichen umgeben. "(" und ")" isoliert
        // TokenPattern bereits selbst, auch ohne umgebende Leerzeichen.
        var normalisiert = ausdruck.Replace("/", " / ");
        return TokenPattern().Matches(normalisiert).Select(m => m.Value).ToList();
    }

    private static Func<IReadOnlyCollection<string>, bool> ParseOder(List<string> tokens, ref int position)
    {
        var links = ParseUnd(tokens, ref position);
        while (position < tokens.Count && IstOderToken(tokens[position]))
        {
            position++;
            var rechts = ParseUnd(tokens, ref position);
            var vorher = links;
            links = m => vorher(m) || rechts(m);
        }
        return links;
    }

    private static Func<IReadOnlyCollection<string>, bool> ParseUnd(List<string> tokens, ref int position)
    {
        var links = ParseUnaer(tokens, ref position);
        while (position < tokens.Count && IstUndToken(tokens[position]))
        {
            position++;
            var rechts = ParseUnaer(tokens, ref position);
            var vorher = links;
            links = m => vorher(m) && rechts(m);
        }
        return links;
    }

    private static Func<IReadOnlyCollection<string>, bool> ParseUnaer(List<string> tokens, ref int position)
    {
        if (position < tokens.Count && IstNichtToken(tokens[position]))
        {
            position++;
            var innen = ParseUnaer(tokens, ref position);
            return m => !innen(m);
        }
        return ParsePrimaer(tokens, ref position);
    }

    private static Func<IReadOnlyCollection<string>, bool> ParsePrimaer(List<string> tokens, ref int position)
    {
        if (position >= tokens.Count)
            throw new ArgumentException("Unerwartetes Ende des Bedingungsausdrucks.");

        if (tokens[position] == "(")
        {
            position++;
            var innen = ParseOder(tokens, ref position);
            if (position >= tokens.Count || tokens[position] != ")")
                throw new ArgumentException("Schließende Klammer erwartet.");
            position++;
            return innen;
        }

        var merkmalsnummer = tokens[position];
        position++;
        return m => m.Contains(merkmalsnummer);
    }

    private static bool IstOderToken(string token) =>
        token == "/" || string.Equals(token, "oder", StringComparison.OrdinalIgnoreCase);

    private static bool IstUndToken(string token) =>
        token == "U" || string.Equals(token, "und", StringComparison.OrdinalIgnoreCase);

    private static bool IstNichtToken(string token) =>
        token == "N" || string.Equals(token, "nicht", StringComparison.OrdinalIgnoreCase);
}
