using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Auftragsanlage;

public static class AngebotsadressenPruefung
{
    public static bool StimmenUeberein(string? kundenadresse, string? lieferadresse)
    {
        var kunde = Token(kundenadresse);
        var lieferung = Token(lieferadresse);

        // Kurze Fragmente wie nur ein Ort oder eine Postleitzahl sind kein belastbarer Vergleich.
        return kunde.Count >= 3 && lieferung.Count >= 3 && kunde.All(lieferung.Contains);
    }

    private static HashSet<string> Token(string? adresse)
    {
        if (string.IsNullOrWhiteSpace(adresse))
            return [];

        var wert = adresse.Replace("ß", "ss", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
        wert = Regex.Replace(wert,
            @"\b(?:VEREINIGTE\s+STAATEN\s+VON\s+AMERIKA|UNITED\s+STATES\s+OF\s+AMERICA|UNITED\s+STATES|U\.?S\.?A\.?)\b",
            " USA ");
        wert = Regex.Replace(wert, @"\b(?:DEUTSCHLAND|GERMANY)\b", " GERMANY ");
        wert = Regex.Replace(wert, @"\b(?:SCHWEIZ|SWITZERLAND)\b", " SWITZERLAND ");
        wert = Regex.Replace(wert, @"\b(?:BRASILIEN|BRAZIL)\b", " BRAZIL ");
        wert = Regex.Replace(wert, @"\b(?:UNGARN|HUNGARY)\b", " HUNGARY ");
        wert = Regex.Replace(wert, @"\bOHIO\b", " OH ");
        wert = Regex.Replace(wert, @"\b(?:STREET|ST\.?)\b", " STREET ");
        wert = Regex.Replace(wert, @"\b(?:DRIVE|DR\.?)\b", " DRIVE ");
        wert = Regex.Replace(wert, @"\b(?:ROAD|RD\.?)\b", " ROAD ");
        wert = Regex.Replace(wert, @"\b(?:STRASSE|STR\.?)\b", " STRASSE ");

        var ohneDiakritika = new string(wert.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        return Regex.Matches(ohneDiakritika, @"[A-Z0-9]+")
            .Select(match => match.Value)
            .Where(token => token.Length > 1 || char.IsDigit(token[0]))
            .ToHashSet(StringComparer.Ordinal);
    }
}
