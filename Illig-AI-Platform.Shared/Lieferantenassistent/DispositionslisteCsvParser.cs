using System.Globalization;
using Illig_AI_Platform.Shared.Services;

namespace Illig_AI_Platform.Shared.Lieferantenassistent;

/// <summary>
/// Parst den SAP-Report "Einkaufsbelege zum Lieferant" (CSV, ';'-getrennt, deutsches
/// Zahlenformat). Zwischensummen-Zeilen pro Lieferant (ohne Einkaufsbeleg) werden
/// übersprungen — nur Detailzeilen mit gefülltem Einkaufsbeleg werden übernommen.
/// </summary>
public static class DispositionslisteCsvParser
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("de-DE");

    public static List<Dispositionsposition> Parse(string inhalt)
    {
        var ergebnis = new List<Dispositionsposition>();

        foreach (var felder in CsvZeilenTokenizer.Zeilen(inhalt).Skip(1))
        {
            if (felder.Length < 20 || string.IsNullOrWhiteSpace(felder[3]))
                continue;

            var kreditorTeile = felder[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (kreditorTeile.Length == 0 ||
                !int.TryParse(kreditorTeile[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kreditor))
                continue;

            var lieferdatum = ParseDatum(felder[6])
                ?? throw new FormatException($"Lieferdatum fehlt für Einkaufsbeleg {felder[3]}/{felder[4]}.");

            ergebnis.Add(new Dispositionsposition
            {
                LieferantKreditor = kreditor,
                Einkaeufergruppe = LeerAlsNull(felder[2]),
                Einkaufsbeleg = felder[3],
                Position = felder[4],
                // Eindeutig je Einteilung (Teillieferung), nicht nur je Position — siehe Kommentar
                // auf Dispositionsposition.Schluessel.
                Schluessel = $"{felder[3]}/{felder[4]}/{lieferdatum:yyyy-MM-dd}",
                Belegdatum = ParseDatum(felder[5]),
                Lieferdatum = lieferdatum,
                Auftragsbestaetigung = LeerAlsNull(felder[7]),
                Material = LeerAlsNull(felder[8]),
                Kurztext = felder[9],
                Werk = LeerAlsNull(felder[10]),
                Bestellmenge = ParseDezimal(felder[11]),
                Bestellmengeneinheit = LeerAlsNull(felder[12]),
                Nettopreis = ParseDezimal(felder[13]),
                Waehrung = LeerAlsNull(felder[14]),
                Preiseinheit = ParseDezimal(felder[15]),
                Einteilungsmenge = ParseDezimal(felder[16]),
                GelieferteMenge = ParseDezimal(felder[17]),
                NochZuLiefernMenge = ParseDezimal(felder[18]),
                MengeInLagerME = ParseDezimal(felder[19]),
                ImportiertAm = DateTime.UtcNow
            });
        }

        return ergebnis;
    }

    private static string? LeerAlsNull(string wert) => string.IsNullOrWhiteSpace(wert) ? null : wert;

    private static DateOnly? ParseDatum(string wert) =>
        DateOnly.TryParseExact(wert, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var datum)
            ? datum
            : null;

    private static decimal ParseDezimal(string wert) =>
        string.IsNullOrWhiteSpace(wert) ? 0m : decimal.Parse(wert, NumberStyles.Number, Kultur);
}
