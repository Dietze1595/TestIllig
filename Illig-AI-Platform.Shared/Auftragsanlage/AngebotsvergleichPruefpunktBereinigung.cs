namespace Illig_AI_Platform.Shared.Auftragsanlage;

public record AngebotsvergleichPruefpunkt(
    string Bereich,
    string Status,
    string Text,
    string? Pruefschluessel = null);

public record BereinigteAngebotsvergleichPruefpunkte(
    IReadOnlyList<string> Uebereinstimmungen,
    IReadOnlyList<string> Abweichungen);

/// <summary>
/// Erzwingt die fachliche Exklusivität je konkretem Prüfthema. Ein Bereich darf mehrere
/// unabhängige Befunde enthalten, beispielsweise einen übereinstimmenden Proforma-Hinweis
/// und ein abweichendes Zahlungsziel. Nur derselbe Sachverhalt darf nicht gleichzeitig
/// als Übereinstimmung und Abweichung erscheinen.
/// </summary>
public static class AngebotsvergleichPruefpunktBereinigung
{
    private static readonly IReadOnlyDictionary<string, string> Bereichsnamen =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["angebotsbezug"] = "Angebotsbezug",
            ["kunde_lieferanschrift"] = "Kunde/Lieferanschrift",
            ["leistungsumfang"] = "Leistungsumfang",
            ["preis_waehrung"] = "Preis/Währung",
            ["zahlungsbedingungen"] = "Zahlungsbedingungen",
            ["lieferbedingungen"] = "Lieferbedingungen",
            ["abnahme_gewaehrleistung"] = "Abnahme/Gewährleistung",
            ["vertragsrisiken"] = "Vertragsrisiken",
        };

    public static BereinigteAngebotsvergleichPruefpunkte Bereinigen(
        IReadOnlyList<AngebotsvergleichPruefpunkt>? pruefpunkte)
    {
        var gueltig = (pruefpunkte ?? [])
            .Where(p => Bereichsnamen.ContainsKey(p.Bereich) && !string.IsNullOrWhiteSpace(p.Text))
            .ToArray();
        var abweichendePruefthemen = gueltig
            .Where(p => IstStatus(p, "abweichung"))
            .Select(ErmittlePruefthema)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var uebereinstimmungen = Zusammenfassen(gueltig
            .Where(p => IstStatus(p, "uebereinstimmung") &&
                        !abweichendePruefthemen.Contains(ErmittlePruefthema(p))));
        var abweichungen = Zusammenfassen(gueltig.Where(p => IstStatus(p, "abweichung")));

        return new BereinigteAngebotsvergleichPruefpunkte(uebereinstimmungen, abweichungen);
    }

    private static bool IstStatus(AngebotsvergleichPruefpunkt punkt, string status) =>
        string.Equals(punkt.Status, status, StringComparison.OrdinalIgnoreCase);

    private static string ErmittlePruefthema(AngebotsvergleichPruefpunkt punkt)
    {
        var schluessel = string.IsNullOrWhiteSpace(punkt.Pruefschluessel)
            ? punkt.Bereich
            : punkt.Pruefschluessel.Trim();
        return $"{punkt.Bereich.Trim()}::{schluessel}";
    }

    private static string[] Zusammenfassen(IEnumerable<AngebotsvergleichPruefpunkt> pruefpunkte) =>
        pruefpunkte
            .GroupBy(punkt => punkt.Bereich, StringComparer.OrdinalIgnoreCase)
            .Select(gruppe =>
            {
                var bereich = Bereichsnamen[gruppe.Key];
                var texte = gruppe
                    .Select(punkt => TextOhneBereichspraefix(punkt, bereich))
                    .Where(text => text.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(SatzBeenden);
                return $"{bereich}: {string.Join(" ", texte)}";
            })
            .Where(text => !text.EndsWith(": ", StringComparison.Ordinal))
            .Take(12)
            .ToArray();

    private static string TextOhneBereichspraefix(
        AngebotsvergleichPruefpunkt punkt, string bereich)
    {
        var text = punkt.Text.Trim();
        return text.StartsWith($"{bereich}:", StringComparison.OrdinalIgnoreCase)
            ? text[(bereich.Length + 1)..].Trim()
            : text;
    }

    private static string SatzBeenden(string text) =>
        text.EndsWith('.') || text.EndsWith('!') || text.EndsWith('?')
            ? text
            : $"{text}.";
}
