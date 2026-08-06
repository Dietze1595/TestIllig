using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Deterministischer Parser für die beschrifteten Felder der von SAP erzeugten
/// ILLIG-Angebote. Er arbeitet auf dem Lesetext von Document Intelligence prebuilt-layout.
/// </summary>
public static class AngebotsLayoutParser
{
    public static ExtrahierteAngebotsdaten Parse(string volltext)
    {
        var bedingungen = VertriebsbedingungenParser.Parse(volltext);
        var (kundenname, kundenadresse) = ExtrahiereKunde(volltext);

        return new ExtrahierteAngebotsdaten(
            Nummer: ExtrahiereAngebotsnummer(volltext),
            Kundenname: kundenname,
            Kundenadresse: kundenadresse,
            Zahlungsbedingungen: bedingungen.Zahlungsbedingungen,
            Positionen: [],
            Volltext: volltext,
            ZahlungsbedingungCode: bedingungen.ZahlungsbedingungCode,
            Zahlungsplan: bedingungen.Zahlungsplan,
            Verkaeufer: bedingungen.Verkaeufer,
            Liefertermin: bedingungen.Liefertermin,
            GueltigBis: bedingungen.GueltigBis,
            Gesamtpreis: ExtrahiereGesamtpreis(volltext),
            Lieferadresse: ExtrahiereLieferadresse(volltext));
    }

    private static string? ExtrahiereLieferadresse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var start = Regex.Match(text,
            @"(?im)^[ \t]*(?:shipping\s+address|delivery\s+address|ship\s+to|lieferadresse|lieferanschrift)[ \t]*:[ \t]*(?<wert>[^\r\n]*)");
        if (!start.Success)
            return null;

        var zeilen = new List<string>();
        var wertDerselbenZeile = start.Groups["wert"].Value.Trim();
        if (wertDerselbenZeile.Length > 0)
            zeilen.Add(wertDerselbenZeile);

        var rest = text[(start.Index + start.Length)..].Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (var rohzeile in rest.Split('\n').Take(12))
        {
            var zeile = rohzeile.Trim();
            if (zeile.Length == 0)
                continue;
            if (Regex.IsMatch(zeile,
                    @"(?i)^(?:type\s+of\s+transport|versandart|unloading\s+point|entladestelle|terms\s+of\s+(?:delivery|payment)|lieferbedingungen?|zahlungsbedingungen?|delivery\s+time|liefertermin|quotation\s+no\.?|angebotsnr\.?)\s*:"))
                break;
            if (Regex.IsMatch(zeile, @"(?i)^We,\s+the\s+company\s+ILLIG\b"))
                break;

            zeilen.Add(zeile);
        }

        var lieferadresse = Verdichte(string.Join(" ", zeilen));
        return lieferadresse.Length == 0 ? null : lieferadresse;
    }

    private static string? ExtrahiereGesamtpreis(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        const string label =
            @"(?:gesamtpreis|angebotssumme|gesamtsumme|nettowert|endbetrag|" +
            @"nett(?:o|ogesamt)(?:preis|betrag|summe)?|" +
            @"total\s+(?:net\s+)?price|net\s+total|total\s+amount|quotation\s+total|grand\s+total)";
        const string waehrung = @"(?:EUR|USD|CHF|GBP|€|\$|E)";
        const string betrag =
            @"(?:\d{1,3}(?:[ .'\u00A0]\d{3})+[,.]\d{2}|" +
            @"\d{1,3}(?:,\d{3})+\.\d{2}|" +
            @"\d+[,.]\d{2})";
        var wert = $@"(?:(?<vor>{waehrung})\s*)?(?<betrag>{betrag})(?:\s*(?<nach>{waehrung}))?";

        var nachLabel = Regex.Match(text,
            $@"(?im)^\s*{label}\s*:?\s*(?:\r?\n\s*)?{wert}");
        if (nachLabel.Success)
            return FormatierePreis(nachLabel);

        // Das Layout-Modell kann den Wert bei leicht versetzten Grundlinien vor der
        // Beschriftung ausgeben. Deshalb wird auch die umgekehrte Reihenfolge akzeptiert.
        var vorLabel = Regex.Match(text,
            $@"(?im)^\s*{wert}\s*(?:\r?\n\s*)?{label}\s*:?\s*$");
        if (vorLabel.Success)
            return FormatierePreis(vorLabel);

        // Tabellenzellen werden vom Layout-Modell nicht immer zeilenweise ausgegeben.
        // Beim Greiner-Angebot kann deshalb z. B. erst "Nettowert Endbetrag" und danach
        // der Werteblock folgen. In einem kleinen lokalen Fenster wird dann der räumlich
        // nächstgelegene Betrag verwendet; Datums- oder Prozentwerte passen nicht auf das
        // bewusst enge Preisformat mit zwei Nachkommastellen.
        foreach (Match labelMatch in Regex.Matches(text, label, RegexOptions.IgnoreCase))
        {
            var fensterStart = Math.Max(0, labelMatch.Index - 250);
            var fensterEnde = Math.Min(text.Length, labelMatch.Index + labelMatch.Length + 500);
            var fenster = text[fensterStart..fensterEnde];
            var kandidaten = Regex.Matches(fenster, wert, RegexOptions.IgnoreCase)
                .Cast<Match>()
                .OrderBy(match => Math.Abs(
                    fensterStart + match.Index - (labelMatch.Index + labelMatch.Length)))
                .ToList();

            var mitWaehrung = kandidaten.FirstOrDefault(match =>
                match.Groups["vor"].Success || match.Groups["nach"].Success);
            if (mitWaehrung is not null)
                return FormatierePreis(mitWaehrung);
        }

        return null;
    }

    private static string FormatierePreis(Match match)
    {
        var betrag = Regex.Replace(match.Groups["betrag"].Value, @"\s+", "");
        var waehrung = match.Groups["vor"].Success
            ? match.Groups["vor"].Value
            : match.Groups["nach"].Value;
        if (string.Equals(waehrung, "E", StringComparison.OrdinalIgnoreCase))
            waehrung = "EUR";
        return string.IsNullOrWhiteSpace(waehrung) ? betrag : $"{betrag} {waehrung}";
    }

    private static string? ExtrahiereAngebotsnummer(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        const string label = @"(?:quotation\s+no\.?|angebotsnr\.?|angebotsnummer)";
        const string nummer = @"(?<wert>\d{7,10})";

        // Das Layout-Modell kann den rechts stehenden Wert je nach minimaler
        // Abweichung der Grundlinie vor oder nach der Beschriftung ausgeben.
        var nachLabel = Regex.Match(text,
            $@"(?im)^\s*{label}[ \t]*:?[ \t]*(?:\r?\n[ \t]*)?{nummer}\b");
        if (nachLabel.Success)
            return nachLabel.Groups["wert"].Value;

        var vorLabel = Regex.Match(text,
            $@"(?im)^\s*{nummer}[ \t]*(?:\r?\n[ \t]*)?{label}[ \t]*:?\s*$");
        if (vorLabel.Success)
            return vorLabel.Groups["wert"].Value;

        // ILLIG-Angebotsnummern beginnen mit 50 und stehen im Kopfbereich. Dieser
        // enge Fallback greift, falls Document Intelligence die Beschriftung nicht
        // erkannt hat, und betrachtet bewusst nicht den späteren Positionstext.
        var kopfbereich = text[..Math.Min(text.Length, 2_000)];
        var fallback = Regex.Match(kopfbereich, @"(?m)\b(?<wert>50\d{6})\b");
        return fallback.Success ? fallback.Groups["wert"].Value : null;
    }

    private static string? ExtrahiereEinzeilig(string text, params string[] labels)
    {
        foreach (var label in labels)
        {
            var match = Regex.Match(text, $@"(?im)^\s*{Regex.Escape(label)}\s*:\s*(?<wert>[^\r\n]+)");
            if (match.Success)
                return match.Groups["wert"].Value.Trim();
        }
        return null;
    }

    // Rechtsform-Kürzel als Grenze zwischen Firmenname und Adresse in der Kundenzeile
    // ("Greiner Packaging AG Rheinstraße 38 …", "Jász-Plasztik Kft. JÁSZBERÉNY …"): Der Name
    // reicht bis einschließlich des Kürzels, der Rest ist die Adresse.
    private const string RechtsformPattern =
        @"(?<![\w])(?:S\.?\s*de\s*R\.?\s*L\.?\s*de\s*C\.?\s*V\.?|S\.?\s*A\.?\s*de\s*C\.?\s*V\.?|" +
        @"GmbH(?:\s*&\s*Co\.?\s*KG)?|mbH|AG|KGaA|KG|OHG|SE|Kft\.?|Zrt\.?|Nyrt\.?|Bt\.?|" +
        @"Sp\.?\s*z\s*o\.?\s*o\.?|S\.A\.S\.?|S\.A\.?|S\.L\.?|S\.p\.A\.?|S\.r\.l\.?|SARL|SRL|" +
        @"Ltda\.?|Limited|Ltd\.?|PLC|Inc\.?|Corp\.?|LLC|LLP|FZCO|FZE|B\.V\.?|N\.V\.?|" +
        @"d\.o\.o\.?|s\.r\.o\.?|a\.s\.?)(?![\w])";

    /// <summary>
    /// Liest Kundenname und -adresse aus dem Kopfbereich. ILLIG ist der Absender (Briefkopf),
    /// NICHT der Kunde — die Kundenzeile steht zwischen der ILLIG-Absenderzeile und dem
    /// Beschriftungsblock ("Angebotsnr.:" / "quotation no.:"). Anker ist dieser stabile
    /// Beschriftungsblock; von dort wird nach oben die Kundenzeile gesucht, wobei Titel
    /// ("Angebot"/"Quotation") und ILLIG-Absenderzeile übersprungen werden. Die ILLIG-Zeile wird
    /// whitespace-normalisiert erkannt, da sie im PDF oft in Sperrschrift ("I L L I G …") gesetzt
    /// ist — genau daran scheiterte die frühere Exact-String-Filterung, sodass ILLIG als Kunde
    /// gespeichert wurde. Name und Adresse werden getrennt gespeichert: die Kundenzeile wird an
    /// der Rechtsform (AG, Kft., GmbH …) geteilt.
    /// </summary>
    private static (string? Name, string? Adresse) ExtrahiereKunde(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, null);

        var zeilen = text.Replace("\r\n", "\n").Split('\n');

        var ankerIndex = Array.FindIndex(zeilen, z =>
            Regex.IsMatch(z, @"(?i)\b(?:Angebotsnr|Angebotsnummer|quotation\s*no|customer\s*no|Kundennr)\b"));
        if (ankerIndex < 0)
            return (null, null);

        // Der Kundenblock steht zwischen der ILLIG-Absenderzeile/dem Trennstrich (oben) und dem
        // Beschriftungsblock (unten). Er kann eine Zeile ("Name Straße Ort Land") oder mehrere
        // Zeilen sein (Name, dann Adresse je Zeile). Der Titel ("Angebot"/"Quotation") kann
        // dazwischen liegen und wird übersprungen; Absenderzeile und Trennstrich begrenzen den
        // Block nach oben und beenden das Einsammeln, damit ILLIG nicht mitgelesen wird.
        var block = new List<string>();
        for (var j = ankerIndex - 1; j >= 0 && block.Count < 8; j--)
        {
            var zeile = zeilen[j].Trim();
            if (zeile.Length == 0)
            {
                if (block.Count > 0)
                    break;
                continue;
            }
            if (IstTitel(zeile))
                continue;
            if (IstIlligAbsender(zeile) || IstTrenner(zeile))
                break;
            block.Insert(0, zeile);
        }

        return block.Count == 0 ? (null, null) : ZerlegeKunde(Verdichte(string.Join(" ", block)));
    }

    private static (string? Name, string? Adresse) ZerlegeKunde(string zeile)
    {
        if (zeile.Length == 0)
            return (null, null);

        int schnitt;
        if (Regex.Match(zeile, RechtsformPattern, RegexOptions.IgnoreCase) is { Success: true } rf)
            schnitt = rf.Index + rf.Length;
        else if (Regex.Match(zeile,
                     @"\s(?=(?:TAX\s*ID|VAT(?:\s*(?:ID|NO\.?|NUMBER))?|USt-IdNr\.?|RFC)\s*:)",
                     RegexOptions.IgnoreCase) is { Success: true, Index: > 0 } steuerkennung)
            schnitt = steuerkennung.Index;
        else if (Regex.Match(zeile, @"\s\d") is { Success: true, Index: > 0 } zahl)
            schnitt = zahl.Index;
        else
            return (zeile, null);

        var name = zeile[..schnitt].Trim();
        var adresse = zeile[schnitt..].Trim().TrimStart(',', ';').Trim();
        return (name.Length > 0 ? name : null, adresse.Length > 0 ? adresse : null);
    }

    private static bool IstTitel(string zeile) =>
        Regex.IsMatch(zeile, @"(?i)^(?:angebot|quotation)$");

    private static bool IstTrenner(string zeile) =>
        Regex.IsMatch(zeile, @"^[_\-\s]{5,}$");

    // Erkennt die ILLIG-Absenderzeile auch in Sperrschrift: Trenn-/Leerzeichen werden entfernt,
    // dann wird auf charakteristische Bestandteile geprüft.
    private static bool IstIlligAbsender(string zeile)
    {
        var norm = Regex.Replace(zeile, @"[\s_.\-·•]", "").ToUpperInvariant();
        return norm.Contains("ILLIGPACKAGING")
            || norm.Contains("ROBERTBOSCH")
            || (norm.Contains("ILLIG") && norm.Contains("HEILBRONN"));
    }

    private static string Verdichte(string text) =>
        Regex.Replace(text, @"\s+", " ").Trim();
}
