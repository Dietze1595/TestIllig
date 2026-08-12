using System.Globalization;
using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public static partial class AuftragsinformationParser
{
    [GeneratedRegex(@"^Pos\.$")]
    private static partial Regex TableHeaderPosLine();

    [GeneratedRegex(@"^Vertriebsmerkmale$")]
    private static partial Regex TableHeaderVertriebsmerkmaleLine();

    // In den seitenweisen Layout-Zeilen fasst Document Intelligence beide sichtbaren
    // Tabellenueberschriften teilweise zu einer Zeile zusammen. Horizontale PDF-Trennlinien
    // koennen dabei als Unterstriche bzw. Bindestriche am Zeilenende enthalten sein.
    [GeneratedRegex(@"^Pos\.\s+Vertriebsmerkmale(?:\s*[_-]{3,})?$")]
    private static partial Regex CombinedTableHeaderLine();

    // Positionszeile: entweder allein ("40/20", "100/10-A") oder mit direkt angehängter
    // Merkmalsnummer auf derselben Zeile ("40/240 021746") — beide Formen kommen im selben
    // Dokument vor. Die maximal 4-stellige Position lässt sich klar von der 5-8-stelligen
    // Merkmalsnummer unterscheiden, sodass keine Verwechslungsgefahr besteht.
    [GeneratedRegex(@"^(\d{1,4}(?:/\d{1,4})?(?:-[A-Z])?)(?:\s+(\d{5,8}))?$")]
    private static partial Regex PositionLine();

    // Merkmalsnummer am Zeilenanfang, optional gefolgt von Beschreibungstext auf derselben
    // Zeile ("014936 Dynamische Prozessoptimierung..." oder auch nur "9209425" allein).
    [GeneratedRegex(@"^(\d{5,8})(?:\s+(.*))?$")]
    private static partial Regex LeadingMerkmalsnummer();

    // Merkmalsnummer am Zeilenende mit vorangestelltem Text ("Mit folgender Konfiguration: 024933").
    [GeneratedRegex(@"^(.*?)\s+(\d{5,8})$")]
    private static partial Regex TrailingMerkmalsnummer();

    [GeneratedRegex(@"^Auftragsnr\s*\.?\s*:\s*(.*)$")]
    private static partial Regex AuftragsnummerLine();

    [GeneratedRegex(@"^Kd\.Nr\.\s*(.*)$")]
    private static partial Regex KundennummerLine();

    [GeneratedRegex(@"^Serialnr\s*\.?\s*:\s*(.*)$")]
    private static partial Regex SerialnrLine();

    // Kundennummer ist immer rein numerisch (siehe echte Beispiele: "708555", "250154",
    // "716864") — bei Export-Aufträgen verschmilzt Azure die Kd.Nr. mit der kompletten
    // Kundenadresse zu einer Zeile, daher wird hier bewusst nur die führende Ziffernfolge
    // übernommen statt der ganzen Zeile.
    [GeneratedRegex(@"^(\d+)")]
    private static partial Regex FuehrendeZiffern();

    [GeneratedRegex(@"^Datum:\s*(.*)$")]
    private static partial Regex DatumLine();

    [GeneratedRegex(@"^Auftragsinformation$")]
    private static partial Regex AuftragsinformationTitelLine();

    // "Sonderoption:" steht nicht immer allein auf einer Zeile — teils am Ende einer
    // Beschreibungszeile ("...spannungsfrei. Sonderoption:") oder direkt vor der Merkmalsnummer
    // derselben Zeile ("Sonderoption: 020182 Sonderablauf..."). Wird als Teilstring erkannt und
    // NUR dieser reine Marker aus dem Beschreibungstext entfernt (nicht die ganze Zeile).
    [GeneratedRegex(@"Sonderoption\s*:")]
    private static partial Regex SonderoptionMarker();

    // Überschrift, die die zugehörige(n) Position(en) als Sonderoption ausweist — breiter als der
    // reine "Sonderoption:"-Marker: jede mit ":" endende Kopfzeile, die "Sonder…" (DE, z. B.
    // "Bedienerführung-Sondersprache:") oder "Special…" (EN, z. B. "Documentation-Special
    // language:") enthält. Nur für die KLASSIFIZIERUNG (Sonderoption ja/nein), nicht fürs Bereinigen
    // der Beschreibung. Wortgrenze + case-insensitiv, damit z. B. "besonders" nicht fälschlich
    // matcht; eine reine "…-Varianten:"-/Standardsprache-Überschrift wird bewusst NICHT erfasst.
    [GeneratedRegex(@"\b(?:Sonder\p{L}*|Special(?:\s+\p{L}+)*)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex SonderUeberschrift();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MehrfachLeerzeichen();

    [GeneratedRegex(@"Kd\.Nr\.")]
    private static partial Regex KdNrMarker();

    // Grenze zwischen Firmenname und Adresse: der Adressteil beginnt bei einem dieser Marker —
    // Postfach/Box (PO Box, Postfach, Boîte, BP) oder Straßen-Kennung (ul. [PL], C/ [ES],
    // Str./Straße [DE], Via/Rua/Rue/Calle). Damit werden Name und Adresse getrennt gespeichert.
    [GeneratedRegex(@"\bP\.?\s*O\.?\s*Box\b|\bPostfach\b|\bBo[iî]te\b|\bBP\b|\bul\.|\bStra[sß]e\b|\bStr\.|\bVia\b|\bRua\b|\bRue\b|\bCalle\b|C/", RegexOptions.IgnoreCase)]
    private static partial Regex AdressStartMarker();

    [GeneratedRegex(@"\s\d")]
    private static partial Regex ErsteAdressZahl();

    /// <summary>
    /// Parses the plain-text content of an "Auftragsinformation" document as returned by Azure
    /// Document Intelligence (prebuilt-layout). Azure splits label and value onto separate lines
    /// for most fields ("Auftragsnr .:" then "11055627 / 40" on the next line) and also splits the
    /// table header ("Pos." then "Vertriebsmerkmale") — this parser accounts for both. There are no
    /// divider/rule lines in the real Content (unlike an earlier, incorrect assumption), so table
    /// rows are collected from the first header onward with no explicit closing marker; footer text
    /// between pages (customer address, repeated machine-type line) can leak into the description of
    /// the preceding Merkmal as noise — accepted since Beschreibung is reviewed/edited by the user.
    /// Within a row, the Merkmalsnummer can appear alone on its own line, at the start of a longer
    /// description line, at the end of one with leading text, or combined with the position code on
    /// one line — all four forms occur in the same real document. If the table header is never
    /// found, or no row satisfies the Merkmalsnummer pattern, <see cref="DokumentAnalyseErgebnis.Merkmale"/>
    /// comes back as an empty list with no distinct signal for "no table present" vs. "structural
    /// mismatch" — callers relying on this for production data should treat an empty
    /// <c>Merkmale</c> list alongside a non-empty <c>Auftragsnummer</c> as worth a manual look.
    /// </summary>
    public static DokumentAnalyseErgebnis Parse(
        string content,
        string? layoutContent = null,
        string? tabellenLayoutContent = null,
        string? positionsLayoutContent = null)
    {
        var lines = content
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(l => l.Trim())
            .ToList();

        var auftragsnummer = ExtractLabeledValue(lines, AuftragsnummerLine()) ?? "";
        var kundennummerRoh = ExtractLabeledValue(lines, KundennummerLine()) ?? "";
        var kundennummer = FuehrendeZiffern().Match(kundennummerRoh) is { Success: true } ziffernTreffer
            ? ziffernTreffer.Value
            : kundennummerRoh;
        var datum = ExtractDatum(lines);
        var maschinentyp = ExtractMaschinentyp(lines);
        var (kundenname, kundenadresse) = ExtrahiereKundendaten(lines, kundennummer);
        var (merkmale, sonderoptionen) = ParseMerkmale(ExtractTabellenZeilen(lines));

        // operation.Value.Content folgt in erkannten Tabellen teilweise der Zell- statt der
        // sichtbaren Zeilenreihenfolge. Dadurch koennen einzelne Positions-/Merkmalsnummer-Paare
        // getrennt oder in den falschen Block einsortiert werden. Die seitenweisen Layout-Zeilen
        // behalten diese Paare dagegen gemeinsam (z. B. "40/80 017364"). Wie bei der
        // Angebotspruefung werden sie deshalb zusaetzlich ausgewertet und nur fehlende Treffer
        // ergaenzt.
        var layoutQuellen = new[] { positionsLayoutContent, tabellenLayoutContent, layoutContent }
            .Where(layout => !string.IsNullOrWhiteSpace(layout)
                             && !string.Equals(content, layout, StringComparison.Ordinal))
            .Cast<string>()
            .ToList();
        if (layoutQuellen.Count > 0)
        {
            // Jede Quelle wird separat geparst. Andernfalls koennen Kopf-/Fussdaten am Anfang
            // einer Folgetabelle an die letzte Position der vorherigen Quelle angehaengt werden.
            var layoutTreffer = new List<(ErkanntesMerkmal Merkmal, bool IstSonderoption)>();
            foreach (var layoutQuelle in layoutQuellen)
            {
                var layoutLines = layoutQuelle
                    .Replace("\r\n", "\n")
                    .Split('\n')
                    .Select(l => l.Trim())
                    .ToList();
                var (layoutMerkmale, layoutSonderoptionen) =
                    ParseMerkmale(ExtractTabellenZeilen(layoutLines));
                layoutTreffer.AddRange(layoutMerkmale.Select(m => (m, false)));
                layoutTreffer.AddRange(layoutSonderoptionen.Select(m => (m, true)));
            }
            ErgaenzeLayoutMerkmale(merkmale, sonderoptionen, layoutTreffer);
        }
        var istAuftragsinformation = lines.Any(l => AuftragsinformationTitelLine().IsMatch(l));

        return new DokumentAnalyseErgebnis(
            auftragsnummer, kundennummer, datum, maschinentyp, merkmale, sonderoptionen,
            Kundenname: kundenname, Kundenadresse: kundenadresse,
            IstAuftragsinformation: istAuftragsinformation);
    }

    /// <summary>
    /// Extrahiert Kundenname und -adresse aus dem Kopfbereich. ILLIG ist der Absender (steht im
    /// Briefkopf), der eigentliche Kunde steht bei der Kundennummer. Der Name wird bevorzugt aus
    /// der kurzen Referenz "&lt;NAME&gt;, &lt;Ort&gt;" gebildet, die zur <b>Hauptkundennummer</b>
    /// (siehe <c>kundennummer</c>) gehört — sie hängt entweder am Ende der vollen Adresszeile
    /// ("… POLEN Kd.Nr. 708555 GUILLIN, Olesnica") oder steht als eigene Zeile unter einer bloßen
    /// "Kd.Nr."-Zeile ("Kd.Nr. 712082" / "PLASZOM, Orleans - SC"). Weil ausschließlich nach der
    /// Hauptkundennummer gesucht wird, wird bei Export-Aufträgen mit abweichendem Warenempfänger
    /// dessen zweite Kd.Nr. samt Kurzreferenz (z. B. "Kd.Nr. 717220 AL-SULAYMANIA, …") korrekt
    /// ignoriert. Fehlt eine passende Kurzreferenz (Export-Fall Malico), wird als Name der
    /// führende Firmenteil der Adresszeile verwendet. Kundenname und -adresse werden getrennt
    /// gespeichert: die volle Zeile bei der ersten Kd.Nr.-Fundstelle wird (nach Abschneiden eines
    /// etwaigen "Kd.Nr. …"-Anhangs) am Adressbeginn in Firmenname und reine Adresse
    /// (Straße/Postfach + Ort + Land, ohne Firmenname) geteilt. Wird nichts gefunden, bleiben
    /// beide Werte <c>null</c> und die Anzeige fällt wie bisher auf die Kundennummer zurück.
    /// </summary>
    private static (string? Kundenname, string? Kundenadresse) ExtrahiereKundendaten(
        List<string> lines, string kundennummer)
    {
        if (string.IsNullOrEmpty(kundennummer))
            return (null, null);

        var kdNrMitNummer = new Regex($@"Kd\.Nr\.\s*{Regex.Escape(kundennummer)}\b\s*(.*)$");
        string? kurzreferenz = null;
        string? volleAdresszeile = null;

        for (var i = 0; i < lines.Count && (kurzreferenz is null || volleAdresszeile is null); i++)
        {
            var treffer = kdNrMitNummer.Match(lines[i]);
            if (!treffer.Success)
                continue;

            var rest = Verdichte(treffer.Groups[1].Value);

            if (kurzreferenz is null)
            {
                if (IstKurzreferenz(rest))
                    kurzreferenz = rest;
                else if (rest.Length == 0
                         && NaechsteNichtLeereZeile(lines, i) is { } folgeZeile
                         && IstKurzreferenz(folgeZeile))
                    kurzreferenz = folgeZeile.Trim();
            }

            if (volleAdresszeile is null)
            {
                var kandidat = rest.Length > 0 && !IstKurzreferenz(rest)
                    ? rest
                    : rest.Length == 0
                        ? NaechsteAdresszeile(lines, i)
                        : null;
                if (kandidat is not null)
                    volleAdresszeile = BereinigeAdresse(kandidat);
            }
        }

        // Name und Adresse getrennt speichern: die volle Zeile "<Firmenname> <Straße/Postfach …>"
        // wird aufgeteilt. Bevorzugter Name ist die "<NAME>, <Ort>"-Kurzreferenz zum Hauptkunden;
        // fehlt sie (Export-Fall, z. B. Malico), dient der führende Firmenname als Name.
        var (firma, reineAdresse) = ZerlegeFirmaUndAdresse(volleAdresszeile);
        return (kurzreferenz ?? firma, reineAdresse);
    }

    // Kurzreferenz "<NAME>, <Ort>": kurz, mit Komma, ohne Ziffer — grenzt sich damit klar von der
    // vollen Adresszeile ab (die Straßennummern/PLZ/Postfachnummern enthält).
    private static bool IstKurzreferenz(string text)
    {
        text = text.Trim();
        return text.Length is > 0 and <= 40
            && text.Contains(',')
            && !text.Any(char.IsDigit);
    }

    private static string? NaechsteNichtLeereZeile(List<string> lines, int ab)
    {
        for (var j = ab + 1; j < lines.Count; j++)
            if (!string.IsNullOrWhiteSpace(lines[j]))
                return lines[j];
        return null;
    }

    // Die Adresszeile folgt direkt auf eine bloße "Kd.Nr."-Zeile und enthält immer mindestens eine
    // Ziffer (Hausnummer/PLZ) — anders als eine reine Kurzreferenz-Zeile, die so übersprungen wird.
    private static string? NaechsteAdresszeile(List<string> lines, int ab) =>
        NaechsteNichtLeereZeile(lines, ab) is { } folge && folge.Any(char.IsDigit) ? folge : null;

    private static string BereinigeAdresse(string kandidat)
    {
        var marker = KdNrMarker().Match(kandidat);
        return Verdichte(marker.Success ? kandidat[..marker.Index] : kandidat);
    }

    // Trennt "<Firmenname> <Adresse>" am ersten Adress-Startmarker (Postfach/Box/Straße …) bzw.,
    // als Rückfall, an der ersten Zahl (Haus-/Postfachnummer). So landen Name und Adresse in
    // getrennten Feldern statt gemeinsam in einem. Ohne erkennbaren Adressbeginn bleibt die
    // Adresse leer und die ganze Zeile gilt als Firmenname.
    private static (string? Firma, string? Adresse) ZerlegeFirmaUndAdresse(string? volleZeile)
    {
        if (string.IsNullOrWhiteSpace(volleZeile))
            return (null, null);

        if (AdressStartIndex(volleZeile) is not int idx || idx <= 0)
            return (volleZeile, null);

        var firma = volleZeile[..idx].Trim();
        var adresse = volleZeile[idx..].Trim();
        return (firma.Length > 0 ? firma : null, adresse.Length > 0 ? adresse : null);
    }

    private static int? AdressStartIndex(string text)
    {
        var kandidaten = new List<int>();
        if (AdressStartMarker().Match(text) is { Success: true, Index: > 0 } marker)
            kandidaten.Add(marker.Index);
        if (ErsteAdressZahl().Match(text) is { Success: true, Index: > 0 } zahl)
            kandidaten.Add(zahl.Index);
        return kandidaten.Count > 0 ? kandidaten.Min() : null;
    }

    private static string Verdichte(string text) =>
        MehrfachLeerzeichen().Replace(text, " ").Trim();

    /// <summary>
    /// Sucht die erste Zeile, die auf <paramref name="labelPattern"/> passt. Enthält die Zeile den
    /// Wert bereits inline (Gruppe 1 nicht leer), wird dieser zurückgegeben — sonst gilt die nächste
    /// nicht-leere Zeile als Wert (Azure trennt Label/Wert häufig auf zwei Zeilen).
    /// </summary>
    private static string? ExtractLabeledValue(List<string> lines, Regex labelPattern)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var match = labelPattern.Match(lines[i]);
            if (!match.Success)
                continue;

            var inline = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(inline))
                return inline;

            for (var j = i + 1; j < lines.Count; j++)
            {
                if (!string.IsNullOrWhiteSpace(lines[j]))
                    return lines[j].Trim();
            }
        }
        return null;
    }

    private static DateOnly? ExtractDatum(List<string> lines)
    {
        var rohwert = ExtractLabeledValue(lines, DatumLine());
        if (rohwert is null)
            return null;

        return DateOnly.TryParseExact(
            rohwert, "d. MMMM yyyy", CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out var datum)
            ? datum
            : null;
    }

    // Im festen Template steht der Maschinentyp-Titel normalerweise in der Zeile direkt vor
    // "Auftragsnr .:" (Seite 1) bzw. direkt vor "Serialnr .:" (Fußzeile jeder Folgeseite).
    // Bei Export-Aufträgen mit abweichendem Warenempfänger verschmilzt Azure Document
    // Intelligence auf Seite 1 aber gelegentlich einen zweiten "Kd.Nr."-Verweis und einen
    // Ortsnamen mit in dieselbe Zeile wie der Maschinentyp — die Seite-1-Fundstelle ist dann
    // verunreinigt. Der echte Maschinentyp wiederholt sich jedoch sauber auf jeder Folgeseite;
    // deshalb werden alle Fundstellen im ganzen Dokument gesammelt und der am häufigsten
    // identisch wiederkehrende Wert gewinnt (bei Gleichstand die zuerst gefundene Variante) —
    // eine einmalige, verunreinigte Fundstelle wird so automatisch von den vielen sauberen
    // Wiederholungen ausgestochen.
    private static string ExtractMaschinentyp(List<string> lines)
    {
        var kandidaten = new List<string>();

        for (var i = 0; i < lines.Count; i++)
        {
            if (!AuftragsnummerLine().IsMatch(lines[i]))
                continue;
            if (VorherigeNichtLeereZeile(lines, i) is { } ersterKandidat)
                kandidaten.Add(ersterKandidat);
            break; // nur die erste Auftragsnr.-Fundstelle (Seite 1) zählt für diese Methode
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (!SerialnrLine().IsMatch(lines[i]))
                continue;
            if (VorherigeNichtLeereZeile(lines, i) is { } folgeKandidat)
                kandidaten.Add(folgeKandidat);
        }

        if (kandidaten.Count == 0)
            return "";

        return kandidaten
            .Select((wert, index) => (wert, index))
            .GroupBy(x => x.wert)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Min(x => x.index))
            .First().Key;
    }

    private static string? VorherigeNichtLeereZeile(List<string> lines, int ab)
    {
        for (var j = ab - 1; j >= 0; j--)
        {
            if (!string.IsNullOrWhiteSpace(lines[j]))
                return lines[j];
        }
        return null;
    }

    private static List<string> ExtractTabellenZeilen(List<string> lines)
    {
        var ergebnis = new List<string>();
        var innerhalbTabelle = false;
        var i = 0;
        while (i < lines.Count)
        {
            if (CombinedTableHeaderLine().IsMatch(lines[i]))
            {
                innerhalbTabelle = true;
                i++;
                continue;
            }

            if (TableHeaderPosLine().IsMatch(lines[i])
                && i + 1 < lines.Count
                && TableHeaderVertriebsmerkmaleLine().IsMatch(lines[i + 1]))
            {
                innerhalbTabelle = true;
                i += 2; // "Pos." und "Vertriebsmerkmale" überspringen, nicht als Inhalt aufnehmen
                continue;
            }

            if (innerhalbTabelle)
                ergebnis.Add(lines[i]);

            i++;
        }
        return ergebnis;
    }

    private static void ErgaenzeLayoutMerkmale(
        List<ErkanntesMerkmal> merkmale,
        List<ErkanntesMerkmal> sonderoptionen,
        IReadOnlyList<(ErkanntesMerkmal Merkmal, bool IstSonderoption)> alleLayoutTreffer)
    {
        var layoutTreffer = alleLayoutTreffer
            .GroupBy(x => x.Merkmal.Position, StringComparer.Ordinal)
            .Select(gruppe => gruppe.First())
            .ToList();

        var vorhandenePositionen = merkmale
            .Concat(sonderoptionen)
            .Select(m => m.Position)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var treffer in layoutTreffer)
        {
            var vorhandenesMerkmalIndex = merkmale.FindIndex(m =>
                string.Equals(m.Position, treffer.Merkmal.Position, StringComparison.Ordinal));
            var vorhandeneSonderoptionIndex = sonderoptionen.FindIndex(m =>
                string.Equals(m.Position, treffer.Merkmal.Position, StringComparison.Ordinal));

            if (vorhandenesMerkmalIndex >= 0 || vorhandeneSonderoptionIndex >= 0)
            {
                // Bei identischem Positions-/Nummernpaar darf die geometrisch begrenzte
                // Beschreibung den verschmutzten globalen Text ersetzen. Nummer und Kategorie
                // bleiben dabei unveraendert. Ein abweichendes Paar wird weiterhin ignoriert.
                var vorhandenes = vorhandenesMerkmalIndex >= 0
                    ? merkmale[vorhandenesMerkmalIndex]
                    : sonderoptionen[vorhandeneSonderoptionIndex];
                if (string.Equals(
                        vorhandenes.Merkmalsnummer,
                        treffer.Merkmal.Merkmalsnummer,
                        StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(treffer.Merkmal.Beschreibung))
                {
                    var bereinigt = vorhandenes with { Beschreibung = treffer.Merkmal.Beschreibung };
                    if (vorhandenesMerkmalIndex >= 0)
                        merkmale[vorhandenesMerkmalIndex] = bereinigt;
                    else
                        sonderoptionen[vorhandeneSonderoptionIndex] = bereinigt;
                }
                continue;
            }

            // Der globale Lesetext bleibt die fuehrende Quelle. Tabellenzellen ergaenzen nur
            // Positionen, die dort wirklich fehlen. So kann eine abweichende Tabellenzellfolge
            // einen bereits korrekt erkannten Treffer (z. B. 40/30) weder ueberschreiben noch in
            // die Sonderoptionen verschieben.
            if (vorhandenePositionen.Add(treffer.Merkmal.Position))
                (treffer.IstSonderoption ? sonderoptionen : merkmale).Add(treffer.Merkmal);
        }

        merkmale.Sort((links, rechts) => VergleichePositionsnummern(links.Position, rechts.Position));
        sonderoptionen.Sort((links, rechts) => VergleichePositionsnummern(links.Position, rechts.Position));
    }

    private static int VergleichePositionsnummern(string links, string rechts)
    {
        var linksTeile = Regex.Matches(links, @"\d+|\D+").Select(m => m.Value).ToList();
        var rechtsTeile = Regex.Matches(rechts, @"\d+|\D+").Select(m => m.Value).ToList();
        for (var i = 0; i < Math.Min(linksTeile.Count, rechtsTeile.Count); i++)
        {
            var linksIstZahl = int.TryParse(linksTeile[i], out var linksZahl);
            var rechtsIstZahl = int.TryParse(rechtsTeile[i], out var rechtsZahl);
            var vergleich = linksIstZahl && rechtsIstZahl
                ? linksZahl.CompareTo(rechtsZahl)
                : string.Compare(linksTeile[i], rechtsTeile[i], StringComparison.Ordinal);
            if (vergleich != 0)
                return vergleich;
        }

        return linksTeile.Count.CompareTo(rechtsTeile.Count);
    }

    private static (List<ErkanntesMerkmal> Merkmale, List<ErkanntesMerkmal> Sonderoptionen) ParseMerkmale(List<string> tabellenZeilen)
    {
        var merkmale = new List<ErkanntesMerkmal>();
        var sonderoptionen = new List<ErkanntesMerkmal>();
        // Ein "Sonderoption:"-Marker ist eine Überschrift, die GENAU die unmittelbar folgende
        // Position kennzeichnet — nicht alle weiteren. Steht der Marker vor der Merkmalsnummer der
        // aktuellen Position, gehört diese selbst dazu; steht er dahinter (oder allein zwischen zwei
        // Positionen), betrifft er erst die nächste Position. Dieses "one-shot"-Carry-over wird nach
        // genau einer Position wieder gelöscht, damit nachfolgende normale Merkmale nicht
        // fälschlich als Sonderoption einsortiert werden (echte Dokumente kennzeichnen jede
        // Sonderoption mit einem eigenen Marker; siehe die beiden Referenzdokumente Guillin/Malico).
        var naechsteIstSonderoption = false;
        var i = 0;
        while (i < tabellenZeilen.Count)
        {
            var positionsTreffer = PositionLine().Match(tabellenZeilen[i]);
            if (!positionsTreffer.Success)
            {
                if (SonderUeberschrift().IsMatch(tabellenZeilen[i]))
                    naechsteIstSonderoption = true; // freistehende Sonder-/Special-Überschrift → gilt für die nächste Position
                i++;
                continue;
            }

            var position = positionsTreffer.Groups[1].Value;
            var inlineMerkmalsnummer = positionsTreffer.Groups[2].Success ? positionsTreffer.Groups[2].Value : null;
            i++;

            var rohZeilen = new List<string>();
            while (i < tabellenZeilen.Count && !PositionLine().IsMatch(tabellenZeilen[i]))
            {
                rohZeilen.Add(tabellenZeilen[i]);
                i++;
            }

            var (markerVorNummer, markerNachNummer) =
                AnalysiereSonderoptionMarker(rohZeilen, inlineMerkmalsnummer is not null);

            // Carry-over vom vorherigen Marker verbrauchen; ein Marker VOR der Nummer macht auch
            // diese Position selbst zur Sonderoption ("Sonderoption: 020182 ...").
            var gehoertZuSonderoptionen = naechsteIstSonderoption || markerVorNummer;
            // Nur ein Marker NACH der Nummer (Nachsatz "...spannungsfrei. Sonderoption:" oder eigene
            // Zeile danach) gilt für die NÄCHSTE Position. Das Carry-over wird hier bewusst
            // zurückgesetzt (one-shot) statt klebrig zu bleiben.
            naechsteIstSonderoption = markerNachNummer;

            var blockZeilen = new List<string>();
            foreach (var roh in rohZeilen)
            {
                var zeile = SonderoptionMarker().IsMatch(roh)
                    ? SonderoptionMarker().Replace(roh, "").Trim()
                    : roh;
                if (!string.IsNullOrWhiteSpace(zeile))
                    blockZeilen.Add(zeile);
            }

            var merkmal = inlineMerkmalsnummer is not null
                ? new ErkanntesMerkmal(position, inlineMerkmalsnummer, string.Join(" ", blockZeilen))
                : ErmittleMerkmal(position, blockZeilen);

            if (merkmal is not null)
                (gehoertZuSonderoptionen ? sonderoptionen : merkmale).Add(merkmal);
        }
        return (merkmale, sonderoptionen);
    }

    /// <summary>
    /// Bestimmt, ob ein "Sonderoption:"-Marker im Block <b>vor</b> bzw. <b>nach</b> der ersten
    /// Merkmalsnummer steht. Ein Marker vor der Nummer macht die aktuelle Position selbst zur
    /// Sonderoption; ein Marker nach der Nummer gilt (als Überschrift) erst für die nächste
    /// Position. Steht die Nummer bereits auf der Positionszeile (<paramref name="nummerIstInline"/>),
    /// liegt jeder Marker im Block zwangsläufig dahinter.
    /// </summary>
    private static (bool VorNummer, bool NachNummer) AnalysiereSonderoptionMarker(
        List<string> rohZeilen, bool nummerIstInline)
    {
        if (nummerIstInline)
            return (false, rohZeilen.Any(z => SonderUeberschrift().IsMatch(z)));

        var vor = false;
        var nach = false;
        var nummerGefunden = false;
        foreach (var zeile in rohZeilen)
        {
            var markerTreffer = SonderUeberschrift().Match(zeile);

            if (nummerGefunden)
            {
                if (markerTreffer.Success)
                    nach = true;
                continue;
            }

            var ohneMarker = markerTreffer.Success ? SonderUeberschrift().Replace(zeile, "").Trim() : zeile;
            var (nummer, _) = ZerlegeMerkmalsnummer(ohneMarker);

            if (nummer is null)
            {
                if (markerTreffer.Success)
                    vor = true; // Marker gefunden, Nummer (noch) nicht — Marker steht davor.
                continue;
            }

            // Diese Zeile enthält die erste Merkmalsnummer.
            nummerGefunden = true;
            if (markerTreffer.Success)
            {
                if (markerTreffer.Index < zeile.IndexOf(nummer, StringComparison.Ordinal))
                    vor = true;
                else
                    nach = true;
            }
        }
        return (vor, nach);
    }

    /// <summary>
    /// Sucht im Block die erste Zeile mit einer erkennbaren Merkmalsnummer (siehe
    /// <see cref="ZerlegeMerkmalsnummer"/>). Zeilen davor und der Rest der Treffer-Zeile
    /// (ohne die Nummer) sowie alle Zeilen danach werden zur Beschreibung zusammengefügt.
    /// </summary>
    private static ErkanntesMerkmal? ErmittleMerkmal(string position, List<string> blockZeilen)
    {
        for (var i = 0; i < blockZeilen.Count; i++)
        {
            var (nummer, rest) = ZerlegeMerkmalsnummer(blockZeilen[i]);
            if (nummer is null)
                continue;

            var beschreibungZeilen = new List<string>(blockZeilen.Take(i));
            if (!string.IsNullOrWhiteSpace(rest))
                beschreibungZeilen.Add(rest);
            beschreibungZeilen.AddRange(blockZeilen.Skip(i + 1));

            return new ErkanntesMerkmal(position, nummer, string.Join(" ", beschreibungZeilen));
        }
        return null;
    }

    /// <summary>
    /// Die Merkmalsnummer steht entweder am Anfang einer Zeile (allein oder mit nachgestelltem
    /// Beschreibungstext, z. B. "9209425" oder "014936 Dynamische Prozessoptimierung...") oder
    /// am Ende einer Zeile mit vorangestelltem Text ("Mit folgender Konfiguration: 024933").
    /// </summary>
    private static (string? Nummer, string RestText) ZerlegeMerkmalsnummer(string zeile)
    {
        var leading = LeadingMerkmalsnummer().Match(zeile);
        if (leading.Success)
            return (leading.Groups[1].Value, leading.Groups[2].Success ? leading.Groups[2].Value : "");

        var trailing = TrailingMerkmalsnummer().Match(zeile);
        return trailing.Success
            ? (trailing.Groups[2].Value, trailing.Groups[1].Value)
            : (null, zeile);
    }
}
