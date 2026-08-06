using System.Text;

namespace Illig_AI_Platform.Shared.Services;

/// <summary>
/// Zerlegt CSV-Text in Zeilen aus Feldern, RFC-4180-artig: ein in Anführungszeichen stehendes
/// Feld darf das Trennzeichen, Zeilenumbrüche und (als "") ein eingebettetes Anführungszeichen
/// enthalten. Unquotierte Zeilen verhalten sich exakt wie das bisherige naive
/// <c>Split(trennzeichen)</c>/<c>Split('\n')</c> — bestehende SAP-Exporte ohne Quoting werden
/// also identisch geparst wie vorher, aber ein Semikolon oder Zeilenumbruch in einem freien
/// Textfeld (z. B. Kurztext, Name) verschiebt nicht mehr stillschweigend alle folgenden Spalten.
/// </summary>
public static class CsvZeilenTokenizer
{
    public static IEnumerable<string[]> Zeilen(string inhalt, char trennzeichen = ';')
    {
        var feld = new StringBuilder();
        var zeile = new List<string>();
        var inAnfuehrungszeichen = false;

        var i = 0;
        while (i < inhalt.Length)
        {
            var zeichen = inhalt[i];

            if (inAnfuehrungszeichen)
            {
                if (zeichen == '"')
                {
                    if (i + 1 < inhalt.Length && inhalt[i + 1] == '"')
                    {
                        feld.Append('"');
                        i += 2;
                        continue;
                    }
                    inAnfuehrungszeichen = false;
                    i++;
                    continue;
                }
                if (zeichen == '\r')
                {
                    // CRLF auch innerhalb quotierter Felder auf '\n' normalisieren — konsistent
                    // zur Behandlung außerhalb (siehe unten), damit ein mehrzeiliges Feld aus
                    // einem Windows-/SAP-Export kein '\r' in den Wert schleppt.
                    i++;
                    continue;
                }
                feld.Append(zeichen);
                i++;
                continue;
            }

            if (zeichen == '"' && feld.Length == 0)
            {
                inAnfuehrungszeichen = true;
                i++;
                continue;
            }

            if (zeichen == trennzeichen)
            {
                zeile.Add(feld.ToString());
                feld.Clear();
                i++;
                continue;
            }

            if (zeichen == '\r')
            {
                i++;
                continue;
            }

            if (zeichen == '\n')
            {
                zeile.Add(feld.ToString());
                feld.Clear();
                yield return [.. zeile];
                zeile.Clear();
                i++;
                continue;
            }

            feld.Append(zeichen);
            i++;
        }

        if (feld.Length > 0 || zeile.Count > 0)
        {
            zeile.Add(feld.ToString());
            yield return [.. zeile];
        }
    }
}
