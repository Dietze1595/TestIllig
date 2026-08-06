using System.Globalization;
using Illig_AI_Platform.Shared.Services;

namespace Illig_AI_Platform.Shared.Lieferantenassistent;

public static class LieferantenstammdatenCsvParser
{
    public static List<Lieferant> Parse(string inhalt)
    {
        var ergebnis = new List<Lieferant>();

        foreach (var felder in CsvZeilenTokenizer.Zeilen(inhalt).Skip(1))
        {
            if (felder.Length < 7 || !GanzzahlVersuchen(felder[0], out var kreditor))
                continue;

            ergebnis.Add(new Lieferant
            {
                Kreditor = kreditor,
                Land = LeerAlsNull(felder[1]),
                Name = felder[2],
                Ort = LeerAlsNull(felder[3]),
                Postleitzahl = OptionaleGanzzahl(felder[4]),
                Strasse = LeerAlsNull(felder[5]),
                AdressNummer = OptionaleGanzzahl(felder[6])
            });
        }

        return ergebnis;
    }

    private static string? LeerAlsNull(string wert) => string.IsNullOrWhiteSpace(wert) ? null : wert;

    private static int? OptionaleGanzzahl(string wert) =>
        GanzzahlVersuchen(wert, out var zahl) ? zahl : null;

    private static bool GanzzahlVersuchen(string wert, out int zahl) =>
        int.TryParse(wert.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out zahl);
}
