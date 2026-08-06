using System.Text;
using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public record AuftragsinformationLayoutZeile(string Content, IReadOnlyList<float> Polygon);

/// <summary>
/// Rekonstruiert Positions-/Merkmals-Paare aus den sichtbaren Seitenkoordinaten. Das ist noetig,
/// wenn Document Intelligence mehrere Positionszellen zusammenfasst oder die rechte Tabellenspalte
/// vor der linken Spalte in den globalen Lesetext schreibt.
/// </summary>
public static partial class AuftragsinformationLayoutParser
{
    [GeneratedRegex(@"^\d{1,4}/\d{1,4}(?:-[A-Z])?$")]
    private static partial Regex Positionszeile();

    [GeneratedRegex(@"^\d{5,8}(?:\s+.*)?$")]
    private static partial Regex Merkmalszeile();

    [GeneratedRegex(@"\s+[\p{L}/-]*Varianten\s*:\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex AngehaengteVariantenUeberschrift();

    public static string? ErzeugePositionsText(
        IEnumerable<IReadOnlyList<AuftragsinformationLayoutZeile>> seiten)
    {
        var paare = new List<(string Position, string Merkmal, IReadOnlyList<string> Beschreibung)>();

        foreach (var seite in seiten)
        {
            var zeilen = seite
                .Where(z => z.Polygon.Count >= 4 && !string.IsNullOrWhiteSpace(z.Content))
                .Select((zeile, index) => new LayoutEintrag(
                    index,
                    zeile.Content.Trim(),
                    MinX(zeile.Polygon),
                    MaxX(zeile.Polygon),
                    MinY(zeile.Polygon),
                    MaxY(zeile.Polygon)))
                .ToList();
            var positionen = zeilen
                .Where(z => Positionszeile().IsMatch(z.Content))
                .OrderBy(z => z.MitteY)
                .ToList();
            var merkmale = zeilen
                .Where(z => Merkmalszeile().IsMatch(z.Content))
                .ToList();
            var verwendeteMerkmale = new HashSet<int>();

            for (var i = 0; i < positionen.Count; i++)
            {
                var position = positionen[i];
                var toleranz = Math.Max(0.08f, position.Hoehe * 0.75f);
                var merkmal = merkmale
                    .Where(m => !verwendeteMerkmale.Contains(m.Index)
                                && m.MinX >= position.MaxX - 0.08f
                                && Math.Abs(m.MitteY - position.MitteY) <= toleranz)
                    .OrderBy(m => Math.Abs(m.MitteY - position.MitteY))
                    .ThenBy(m => m.MinX)
                    .FirstOrDefault();
                if (merkmal is null)
                    continue;

                verwendeteMerkmale.Add(merkmal.Index);
                var naechstePositionY = i + 1 < positionen.Count
                    ? positionen[i + 1].MitteY
                    : float.MaxValue;
                var beschreibung = zeilen
                    .Where(z => z.Index != merkmal.Index
                                && z.MitteY > merkmal.MitteY + 0.02f
                                && z.MitteY < naechstePositionY - 0.02f
                                && z.MinX >= merkmal.MinX - 0.08f
                                && !Positionszeile().IsMatch(z.Content)
                                && !Merkmalszeile().IsMatch(z.Content)
                                && !IstVariantenUeberschrift(z.Content)
                                && !IstFusszeile(z.Content))
                    .OrderBy(z => z.MitteY)
                    .ThenBy(z => z.MinX)
                    .Select(z => z.Content)
                    .ToList();

                paare.Add((position.Content, BereinigeMerkmalszeile(merkmal.Content), beschreibung));
            }
        }

        if (paare.Count == 0)
            return null;

        var text = new StringBuilder("Pos.\nVertriebsmerkmale\n");
        foreach (var paar in paare)
        {
            text.AppendLine(paar.Position);
            text.AppendLine(paar.Merkmal);
            foreach (var beschreibung in paar.Beschreibung)
                text.AppendLine(beschreibung);
        }

        return text.ToString();
    }

    private static string BereinigeMerkmalszeile(string content) =>
        AngehaengteVariantenUeberschrift().Replace(content, "").Trim();

    private static bool IstVariantenUeberschrift(string content) =>
        Regex.IsMatch(content.Trim(), @"^[\p{L}/ -]*Varianten\s*:\s*$", RegexOptions.IgnoreCase);

    private static bool IstFusszeile(string content) =>
        content.StartsWith("Kd.Nr.", StringComparison.OrdinalIgnoreCase)
        || content.StartsWith("Auftragsnr", StringComparison.OrdinalIgnoreCase)
        || content.StartsWith("Datum:", StringComparison.OrdinalIgnoreCase)
        || content.StartsWith("Serialnr", StringComparison.OrdinalIgnoreCase)
        || content.StartsWith("Termin:", StringComparison.OrdinalIgnoreCase)
        || content.StartsWith("Seite:", StringComparison.OrdinalIgnoreCase);

    private static float MinX(IReadOnlyList<float> polygon) =>
        Enumerable.Range(0, polygon.Count / 2).Min(index => polygon[index * 2]);

    private static float MaxX(IReadOnlyList<float> polygon) =>
        Enumerable.Range(0, polygon.Count / 2).Max(index => polygon[index * 2]);

    private static float MinY(IReadOnlyList<float> polygon) =>
        Enumerable.Range(0, polygon.Count / 2).Min(index => polygon[index * 2 + 1]);

    private static float MaxY(IReadOnlyList<float> polygon) =>
        Enumerable.Range(0, polygon.Count / 2).Max(index => polygon[index * 2 + 1]);

    private sealed record LayoutEintrag(
        int Index,
        string Content,
        float MinX,
        float MaxX,
        float MinY,
        float MaxY)
    {
        public float MitteY => (MinY + MaxY) / 2f;
        public float Hoehe => MaxY - MinY;
    }
}
