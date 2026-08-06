using System.Text.RegularExpressions;
using System.Globalization;

namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Liest Zahlungsbedingungen (vollständig, nicht nur die erste Zeile wie das
/// Document-Intelligence-Feld PaymentTerm), Incoterm (+ Ort bei CPT) und Versandbedingung
/// aus dem vollen erkannten Angebotstext. Arbeitet unabhängig vom Document-Intelligence-
/// Modell direkt auf dem zusammenhängenden, beschrifteten Textblock
/// ("terms of payment:" / "terms of delivery:" / "type of transport:" ...).
/// Siehe Spec, Abschnitt „Vertriebsbedingungen-Parser".
/// </summary>
public static class VertriebsbedingungenParser
{
    private static readonly string[] ZahlungsbedingungLabels = ["terms of payment:", "zahlungsbedingung:", "zahlungsbedingungen:"];
    private static readonly string[] LieferbedingungLabels = ["terms of delivery:", "lieferbedingung:", "lieferbedingungen:"];
    private static readonly string[] VersandartLabels = ["type of transport:", "versandart:"];

    private static readonly string[] AbschnittsEndmuster =
    [
        @"\bILLIG\s+(?:packaging\s+solutions|Maschinenbau)\s+GmbH\b",
        @"\b(?:Geschäftsführer|Managing\s+Directors)\s*:",
        @"\b(?:Bankverbindung|Bank\s+details)\b",
    ];

    private static readonly string[] AlleLabels =
    [
        .. ZahlungsbedingungLabels, .. LieferbedingungLabels, .. VersandartLabels,
        "delivery time:", "liefertermin:", "lieferzeit:", "shipping address:", "versandadresse:", "versandanschrift:",
        "unloading point:", "entladestelle:",
    ];

    // Ohne weiteres Label als Ende (letzter Abschnitt im Dokument) wird die Länge begrenzt,
    // damit nicht versehentlich unzusammenhängender Text späterer Seiten (technische Daten,
    // AGB) mit in den Abschnitt gerät.
    private const int MaxAbschnittsLaengeOhneEndLabel = 500;

    public static VertriebsbedingungenErgebnis Parse(string volltext)
    {
        var zahlungsabschnitt = ExtrahiereAbschnitt(
            volltext, ZahlungsbedingungLabels, ueberSeitenumbruchFortsetzen: true);
        var (zahlungsbedingungen, zahlungsplan) = TeileZahlungsabschnitt(zahlungsabschnitt);
        var lieferbedingungAbschnitt = ExtrahiereAbschnitt(volltext, LieferbedingungLabels);

        var (incoterm, incotermOrt) = ErmittleIncoterm(lieferbedingungAbschnitt);
        var versandbedingung = ErmittleVersandart(volltext);
        var zahlungsbedingungCode = ErmittleZahlungsbedingungCode(zahlungsabschnitt);

        return new VertriebsbedingungenErgebnis(
            zahlungsbedingungen, incoterm, incotermOrt, versandbedingung,
            zahlungsbedingungCode, zahlungsplan,
            ExtrahiereEinzeiligesFeld(volltext, "contact", "ansprechpartner", "salesperson", "verkäufer"),
            ErmittleLiefertermin(volltext),
            ExtrahiereGueltigBis(volltext));
    }

    // Sucht das erste bekannte Start-Label und liefert den Text bis zum nächsten bekannten
    // Label (oder bis zur Längengrenze), getrimmt. Null, wenn kein Start-Label gefunden wurde.
    private static string? ExtrahiereAbschnitt(
        string volltext, string[] startLabels, bool ueberSeitenumbruchFortsetzen = false)
    {
        var startMatch = startLabels
            .Select(label => Regex.Match(volltext, Regex.Escape(label), RegexOptions.IgnoreCase))
            .Where(m => m.Success)
            .OrderBy(m => m.Index)
            .FirstOrDefault();

        if (startMatch is null)
            return null;

        var inhaltStart = startMatch.Index + startMatch.Length;
        var rest = volltext[inhaltStart..];

        var labelEndeOffset = AlleLabels
            .Select(label => Regex.Match(rest, Regex.Escape(label), RegexOptions.IgnoreCase))
            .Where(m => m.Success)
            .Select(m => (int?)m.Index)
            .OrderBy(i => i)
            .FirstOrDefault();

        var footerEndeOffset = AbschnittsEndmuster
            .Select(muster => Regex.Match(rest, muster, RegexOptions.IgnoreCase))
            .Where(m => m.Success)
            .Select(m => (int?)m.Index)
            .OrderBy(i => i)
            .FirstOrDefault();

        // Ein Zahlungsplan kann am Seitenende beginnen und auf der Folgeseite fortgesetzt
        // werden. Wenn dort ein fachliches Folgelabel existiert, ist dieses die verlässliche
        // Abschnittsgrenze. Wiederholte Footer und Seitenköpfe innerhalb des Abschnitts werden
        // anschließend entfernt. Ohne Folgelabel bleibt der Footer weiterhin die harte Grenze,
        // damit Rechtstexte nicht in einen Feldwert laufen.
        var fruehestesEnde = new[] { labelEndeOffset, footerEndeOffset }
            .Where(offset => offset.HasValue)
            .Select(offset => offset!.Value)
            .DefaultIfEmpty(-1)
            .Min();
        var inhaltEnde = ueberSeitenumbruchFortsetzen && labelEndeOffset.HasValue
            ? labelEndeOffset.Value
            : fruehestesEnde >= 0
                ? fruehestesEnde
                : Math.Min(MaxAbschnittsLaengeOhneEndLabel, rest.Length);

        var inhalt = rest[..inhaltEnde].Trim();
        return inhalt.Length == 0 ? null : inhalt;
    }

    private static (string? Incoterm, string? Ort) ErmittleIncoterm(string? abschnitt)
    {
        if (abschnitt is null)
            return (null, null);

        var match = Regex.Match(abschnitt, @"\b(FCA|CPT)\b", RegexOptions.IgnoreCase);
        if (!match.Success)
            return (null, null);

        var incoterm = match.Value.ToUpperInvariant();
        if (incoterm != "CPT")
            return (incoterm, null);

        // Bei CPT den Rest der Zeile nach dem Code als Ort erfassen (bewusst nicht weiter
        // zerlegt, z. B. um eine vorangestellte Incoterm-Erläuterung wie "Carriage paid to" —
        // ohne echtes CPT-Beispieldokument ist ein präziseres Parsing nicht belastbar).
        var restZeile = abschnitt[(match.Index + match.Length)..];
        var zeilenEndeIndex = restZeile.IndexOf('\n');
        var ort = (zeilenEndeIndex >= 0 ? restZeile[..zeilenEndeIndex] : restZeile).Trim().Trim(',').Trim();
        ort = Regex.Replace(ort, @"^(?:carriage\s+paid\s+to|frachtfrei)\s+", "", RegexOptions.IgnoreCase).Trim();

        return (incoterm, ort.Length == 0 ? null : ort);
    }

    private static string? ErmittleZahlungsbedingungCode(string? zahlungsbedingungen)
    {
        if (string.IsNullOrWhiteSpace(zahlungsbedingungen))
            return null;

        var explizit = Regex.Match(zahlungsbedingungen, @"\b(A14|A30|ALC)\b", RegexOptions.IgnoreCase);
        if (explizit.Success)
            return explizit.Value.ToUpperInvariant();
        if (Regex.IsMatch(zahlungsbedingungen, @"\b(akkreditiv|letter\s+of\s+credit)\b", RegexOptions.IgnoreCase))
            return "ALC";
        if (Regex.IsMatch(zahlungsbedingungen, @"\b14\s*(?:d|t\.?|tage|days)\b", RegexOptions.IgnoreCase))
            return "A14";
        if (Regex.IsMatch(zahlungsbedingungen, @"\b30\s*(?:d|t\.?|tage|days)\b", RegexOptions.IgnoreCase))
            return "A30";
        return null;
    }

    private static (string? Zahlungsbedingungen, string? Zahlungsplan) TeileZahlungsabschnitt(string? abschnitt)
    {
        if (string.IsNullOrWhiteSpace(abschnitt))
            return (null, null);

        var normalisiert = BereinigeZahlungsabschnitt(abschnitt);

        // In ILLIG-Angeboten steht zuerst das Zahlungsziel und danach der Ratenplan.
        // Der erste Prozentwert ist dabei die stabile Grenze. Das funktioniert auch,
        // wenn Document Intelligence Zahlungsziel und erste Rate in eine Zeile schreibt.
        var prozentStart = Regex.Match(normalisiert, @"(?<!\d)(?:100|[1-9]?\d)\s*%");
        if (prozentStart.Success)
            return (
                LeerZuNull(normalisiert[..prozentStart.Index]),
                LeerZuNull(normalisiert[prozentStart.Index..]));

        // Check for proforma payment plan (e.g. Total amount payable against Proforma Invoice, net)
        var proformaMatch = Regex.Match(normalisiert, @"(?i)\b(?:(?:total\s+amount\s+)?payable\s+against|(?:gesamtbetrag\s+)?zahlbar\s+gegen)\s+pro\s*forma(?:\s*(?:invoice|rechnung))?\b");
        if (proformaMatch.Success)
        {
            var splitIndex = proformaMatch.Index;
            if (splitIndex == 0)
            {
                return (normalisiert, normalisiert);
            }
            return (
                LeerZuNull(normalisiert[..splitIndex]),
                LeerZuNull(normalisiert[splitIndex..]));
        }

        // Fallback für seltene Ratenpläne mit absoluten Beträgen statt Prozenten.
        var zeilen = normalisiert.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var betragStart = Array.FindIndex(zeilen, 1,
            zeile => Regex.IsMatch(zeile, @"\b(?:EUR|USD|CHF|GBP)\b", RegexOptions.IgnoreCase));
        if (betragStart >= 0)
            return (
                LeerZuNull(string.Join(Environment.NewLine, zeilen[..betragStart])),
                LeerZuNull(string.Join(Environment.NewLine, zeilen[betragStart..])));

        return (normalisiert, null);
    }

    private static string BereinigeZahlungsabschnitt(string abschnitt)
    {
        var zeilen = abschnitt.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var ergebnis = new List<string>();
        var ueberspringtSeitenrahmen = false;

        foreach (var rohzeile in zeilen)
        {
            var zeile = rohzeile.Trim();
            if (zeile.Length == 0)
                continue;

            if (IstWiederholterSeitenkopf(zeile))
            {
                ueberspringtSeitenrahmen = true;
                continue;
            }

            var footer = FindeSeitenrahmenStart(zeile);
            if (footer >= 0)
            {
                var fachlicherAnteil = zeile[..footer].Trim();
                if (!ueberspringtSeitenrahmen && fachlicherAnteil.Length > 0)
                    ergebnis.Add(fachlicherAnteil);

                ueberspringtSeitenrahmen = true;
                continue;
            }

            if (ueberspringtSeitenrahmen)
            {
                // Der Zahlungsplan wird auf der neuen Seite typischerweise mit der nächsten
                // Rate fortgesetzt. Auch bei zusammengezogenen Layoutzeilen wird nur der
                // fachliche Teil ab dem Prozentwert übernommen.
                var fortsetzung = Regex.Match(zeile, @"(?<!\d)(?:100|[1-9]?\d)\s*%");
                if (!fortsetzung.Success)
                    continue;

                zeile = zeile[fortsetzung.Index..].Trim();
                ueberspringtSeitenrahmen = false;
            }

            ergebnis.Add(zeile);
        }

        return string.Join("\n", ergebnis).Trim();
    }

    private static bool IstWiederholterSeitenkopf(string zeile) =>
        Regex.IsMatch(zeile, @"\bquotation\s+no\.?\s*:.*\bpage\s*:", RegexOptions.IgnoreCase) ||
        Regex.IsMatch(zeile, @"\bAngebots(?:nr\.?|nummer)\s*:.*\bSeite\s*:", RegexOptions.IgnoreCase);

    private static int FindeSeitenrahmenStart(string zeile)
    {
        var muster = new[]
        {
            @"\bILLIG\s+(?:packaging\s+solutions|Maschinenbau)\s+GmbH\b",
            @"\b(?:Geschäftsführer|Managing\s+Directors)\s*:",
            @"\b(?:Bankverbindung|Bank\s+details)\b"
        };

        return muster
            .Select(pattern => Regex.Match(zeile, pattern, RegexOptions.IgnoreCase))
            .Where(match => match.Success)
            .Select(match => match.Index)
            .DefaultIfEmpty(-1)
            .Min();
    }

    private static string? LeerZuNull(string wert)
    {
        var getrimmt = wert.Trim();
        return getrimmt.Length == 0 ? null : getrimmt;
    }

    private static string? ExtrahiereEinzeiligesFeld(string volltext, params string[] labels)
    {
        foreach (var label in labels)
        {
            // Den Wert zuerst ausschließlich in derselben Zeile lesen. Das Layout-Modell
            // kann bei zweispaltigen SAP-Kopfzeilen Beschriftung und Wert aber auch in zwei
            // direkt aufeinanderfolgende Textzeilen aufteilen.
            var match = Regex.Match(volltext,
                $@"(?im)^[ \t]*{Regex.Escape(label)}[ \t]*:[ \t]*(?<wert>[^\r\n]*)");
            if (!match.Success)
                continue;

            var wert = match.Groups["wert"].Value.Trim();
            if (wert.Length > 0)
                return wert;

            var rest = volltext[(match.Index + match.Length)..];
            if (rest.StartsWith("\r\n", StringComparison.Ordinal))
                rest = rest[2..];
            else if (rest.StartsWith('\n') || rest.StartsWith('\r'))
                rest = rest[1..];
            else
                continue;

            var zeilenEnde = rest.IndexOfAny(['\r', '\n']);
            var folgezeile = (zeilenEnde >= 0 ? rest[..zeilenEnde] : rest).Trim();
            if (folgezeile.Length == 0 || IstBeschriftung(folgezeile))
                continue;

            return folgezeile;
        }
        return null;
    }

    private static bool IstBeschriftung(string wert) =>
        AlleLabels.Any(label =>
            wert.StartsWith(label.TrimEnd(':'), StringComparison.OrdinalIgnoreCase)) ||
        Regex.IsMatch(wert, @"^[\p{L}][\p{L}\d\s./_-]{0,40}\s*:", RegexOptions.IgnoreCase);

    private static string? ErmittleVersandart(string volltext)
    {
        foreach (var label in VersandartLabels)
        {
            var labelOhneDoppelpunkt = label.TrimEnd(':');
            foreach (Match match in Regex.Matches(
                         volltext,
                         $@"(?im)^[ \t]*{Regex.Escape(labelOhneDoppelpunkt)}(?=[ \t]*:|[ \t]+|$)[ \t]*:?[ \t]*(?<wert>[^\r\n]*)"))
            {
                var wert = match.Groups["wert"].Value.Trim();
                if (IstPlausibleVersandart(wert))
                    return wert;

                // Nur die unmittelbar folgende Zeile ist ohne Positionsdaten sicher
                // zuzuordnen. Weiter entfernte Zeilen können bereits Rechtstext sein.
                var rest = volltext[(match.Index + match.Length)..];
                if (rest.StartsWith("\r\n", StringComparison.Ordinal))
                    rest = rest[2..];
                else if (rest.StartsWith('\n') || rest.StartsWith('\r'))
                    rest = rest[1..];
                else
                    continue;

                var zeilenEnde = rest.IndexOfAny(['\r', '\n']);
                var folgezeile = (zeilenEnde >= 0 ? rest[..zeilenEnde] : rest).Trim();
                if (IstPlausibleVersandart(folgezeile))
                    return folgezeile;
            }
        }

        return null;
    }

    private static bool IstPlausibleVersandart(string wert) =>
        !string.IsNullOrWhiteSpace(wert) &&
        wert.Length <= 120 &&
        wert is not "-" and not "–" and not "—" &&
        !IstBeschriftung(wert);

    private static string? ErmittleLiefertermin(string volltext)
    {
        var wert = ExtrahiereEinzeiligesFeld(
            volltext, "delivery time", "lieferzeit", "liefertermin", "delivery date");
        if (wert is null)
            return null;

        // SAP-Angebote enthalten teilweise nur eine Druckvorlagen-Anweisung statt
        // eines konkreten Termins. Auch ein nachgelagertes Adresslabel ist kein Wert.
        if (Regex.IsMatch(wert,
                @"^(?:del(?:ivery)?\.?\s*time\s+in\s+months|lieferzeit\s+in\s+monaten|shipping\s+address|versand(?:adresse|anschrift))\b",
                RegexOptions.IgnoreCase))
            return null;

        return wert;
    }

    private static DateTime? ExtrahiereGueltigBis(string volltext)
    {
        var rohwert = ExtrahiereEinzeiligesFeld(volltext, "valid to", "gültig bis", "gueltig bis", "valid until");
        if (rohwert is null)
            return null;

        var normalisiert = rohwert.Trim().TrimEnd('.');
        var kulturen = new[] { CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("en-GB"), CultureInfo.InvariantCulture };
        foreach (var kultur in kulturen)
        {
            if (DateTime.TryParse(normalisiert, kultur, DateTimeStyles.AllowWhiteSpaces, out var datum))
                return datum.Date;
        }
        return null;
    }
}
