using System.Globalization;
using Illig_AI_Platform.Shared.Services;

namespace Illig_AI_Platform.Shared.Lieferantenassistent;

/// <summary>Parst den Export "Mail-Adresse aus Lieferantenstammsatz" (CSV, ';'-getrennt).</summary>
public static class LieferantEmailAdresseCsvParser
{
    public static List<LieferantEmailAdresse> Parse(string inhalt)
    {
        var ergebnis = new List<LieferantEmailAdresse>();

        foreach (var felder in CsvZeilenTokenizer.Zeilen(inhalt).Skip(1))
        {
            if (felder.Length < 2 ||
                !int.TryParse(felder[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var adressNummer))
                continue;

            ergebnis.Add(new LieferantEmailAdresse
            {
                AdressNummer = adressNummer,
                EmailAdresse = felder[1],
                IstStandard = felder.Length > 2 && felder[2].Trim().Equals("X", StringComparison.OrdinalIgnoreCase)
            });
        }

        return ergebnis;
    }
}
