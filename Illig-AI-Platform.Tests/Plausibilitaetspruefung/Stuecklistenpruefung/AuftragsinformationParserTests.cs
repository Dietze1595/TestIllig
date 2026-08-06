using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class AuftragsinformationParserTests
{
    // Wörtlicher (gekürzter) Auszug aus dem tatsächlichen AnalyzeResult.Content von Azure
    // Document Intelligence (prebuilt-layout) für Seite 1 des Referenzdokuments
    // 11055627_40_RDK80k_Guillin20252105_Auftragsinformation.pdf. Diese Fixture ersetzt eine
    // frühere, auf einer eigenen (falschen) PDF-Textextraktion basierende Annahme — Azure trennt
    // Label/Wert auf zwei Zeilen und "Pos."/"Vertriebsmerkmale" ebenfalls, und liefert keine
    // Trennlinien als Text.
    private const string Seite1 = """
        Termin: 19.11.2025
        Kd.Nr. 708555
        Guillin Polska Sp. z o.o. ul. Przemysłowa 3 56-400 OLESNICA POLEN Kd.Nr. 708555 GUILLIN, Olesnica
        Auftragsinformation
        Sachbearbeiter/in:
        Herr Mark
        Seite:
        1 / 15
        Heilbronn:
        21. Mai 2025
        Konstruktion:
        Gökcen/Bindereif 05.06.2025
        _
        Auftragszentrum:
        _
        RDK 80k_Siemens_konf_ab_01.2013
        Auftragsnr .:
        11055627 / 40
        Netzplan Nr .:
        1042100
        Serialnr .:
        ( 732_0478 )
        Versandbedingungen: Versand mit:
        Spedition
        20
        AWK 800_konf_ab ( 665_0652 )
        30
        VHW 90/2b ( 690_0771 )
        50
        RS 75b_konf_ab 22.11.2012 ( 567_1716 )
        120
        Versandvorb. Spedition(LKW)
        130
        Freight charges
        140
        Gewährleistung
        150
        Kulanz
        Pos.
        Vertriebsmerkmale
        40
        9209425
        ILLIG Druckluft-Formungsautomat Type RDK 80k
        40/10
        Version 06
        40/20
        025011
        Achtung, Grundmaschine mit neuem HMI Bedienfeld!
        """;

    // Wörtlicher Anschluss (Seite-1→2-Übergang, kein Trennzeichen im echten Content) desselben
    // Dokuments, direkt an Seite1 anschließend.
    private const string Seite2Anschluss = """
        Guillin Polska Sp. z o.o. ul. Przemysłowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 2 / 15
        Pos.
        Vertriebsmerkmale
        40/30
        Mit folgender Konfiguration: 024933
        Formungsautomat für Rollenfolie für den Einsatz von kombinierten Form- und Bandstahlschnittwerkzeugen.
        """;

    [Fact]
    public void Parse_ExtractsAuftragsnummer()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite1);
        Assert.Equal("11055627 / 40", ergebnis.Auftragsnummer);
    }

    [Fact]
    public void Parse_ExtractsKundennummer()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite1);
        Assert.Equal("708555", ergebnis.Kundennummer);
    }

    [Fact]
    public void Parse_ExtractsMaschinentyp()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite1);
        Assert.Equal("RDK 80k_Siemens_konf_ab_01.2013", ergebnis.Maschinentyp);
    }

    [Fact]
    public void Parse_ErkenntAuftragsinformation_AnhandTitelzeile()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite1);
        Assert.True(ergebnis.IstAuftragsinformation);
    }

    // Fiktiver Lieferschein (kein Auszug aus einer echten Datei) — enthält bewusst eine
    // "Auftragsnr .:"-ähnliche Zeile, damit die alte, rein auf die Auftragsnummer gestützte
    // Prüfung getäuscht worden wäre. Ohne die Titelzeile "Auftragsinformation" muss die neue
    // Prüfung das Dokument trotzdem korrekt als Fremddokument erkennen.
    private const string FiktiverLieferschein = """
        Lieferschein
        Auftragsnr .:
        11055627 / 40
        Kd.Nr. 708555
        Lieferdatum: 12.06.2025
        """;

    [Fact]
    public void Parse_ErkenntFremddokument_OhneAuftragsinformationsTitel()
    {
        var ergebnis = AuftragsinformationParser.Parse(FiktiverLieferschein);
        Assert.False(ergebnis.IstAuftragsinformation);
    }

    // Wörtlicher Auszug (Seite 1, Kopfbereich) aus dem echten AnalyzeResult.Content für
    // AI_11055894_40_RDM75Kc_Malico_20260423.pdf. Bei Export-Aufträgen mit abweichendem
    // Warenempfänger (zweite "Kd.Nr."-Angabe) verschmilzt Azure diesen zweiten Kd.Nr.-Verweis,
    // den Ortsnamen und den Maschinentyp-Titel zu EINER Zeile — das bricht die alte "Zeile
    // direkt vor Auftragsnr.: ist immer nur der Maschinentyp"-Annahme. Ebenso verschmilzt der
    // erste Kd.Nr.-Verweis mit der kompletten Kundenadresse zu einer Zeile.
    private const string MalicoSeite1KopfMitZweitemKdNr = """
        Termin: 22.03.2027
        Kd.Nr. 717216 Malico General Trading FZCO PO Box no. 18257, Office 16605, Jafza Lob 16, Jebel Ali Free Zone DUBAI VEREINIGTE ARABISCHE EMIRATE
        Auftragsinformation
        Sachbearbeiter/in:
        Herr Butz
        Seite:
        1/ 8
        Heilbronn:
        23. April 2026
        Konstruktion:
        Weidenfelder - 24.04.2026
        Roßnagel - 24.04.2026
        _
        Auftragszentrum:
        _
        Kd.Nr. 717220 AL-SULAYMANIA, Karbala Governorate RDM 75Kc_konf_ab (877)
        Auftragsnr .:
        11055894 / 40
        Netzplan Nr .:
        1042751
        Serialnr .:
        ( 877_0202 )
        """;

    // Wörtlicher Auszug der Fußzeilen von Seite 2 und 3 desselben Dokuments — der Maschinentyp
    // erscheint dort sauber, identisch wiederholt (ohne die Kd.Nr.-Verunreinigung von Seite 1).
    private const string MalicoSeite2Und3Fuss = """
        Malico General Trading FZCO PO Box no. 18257, Office 16605, DUBAI
        Kd.Nr. 717220
        Auftragsnr .:
        11055894 / 40
        Datum:
        23. April 2026
        RDM 75Kc_konf_ab (877)
        Serialnr .:
        ( 877_0202 )
        Termin:
        22.03.2027
        Seite: 2/ 8
        Malico General Trading FZCO PO Box no. 18257, Office 16605, DUBAI
        Kd.Nr. 717220
        Auftragsnr .:
        11055894 / 40
        Datum:
        23. April 2026
        RDM 75Kc_konf_ab (877)
        Serialnr .:
        ( 877_0202 )
        Termin:
        22.03.2027
        Seite: 3/ 8
        """;

    // Wörtlicher Kopfbereich (Azure-Content-Stil) aus AI_11055783_60_RS91_Plaszom_20251202.pdf.
    // Die Kunden-Kurzreferenz "PLASZOM, Orleans - SC" steht hier auf einer eigenen Zeile direkt
    // unter einer zweiten (bloßen) "Kd.Nr."-Zeile — anders als bei Guillin, wo sie am Ende der
    // vollen Adresszeile hängt. Beide Formen müssen als Kundenname erkannt werden.
    private const string PlaszomSeite1Kopf = """
        Termin: 13.08.2026
        Kd.Nr. 712082
        Plaszom Zomer Industrial de Plásticos Ltda PO Box 06 ORLEANS - SC 88870-000 BRASILIEN
        Kd.Nr. 712082
        PLASZOM, Orleans - SC
        RS 91_konf
        Auftragsinformation
        Sachbearbeiter/in:
        Herr Carrillo
        Auftragsnr .:
        11055783 / 60
        Serialnr .:
        ( 838_0159 )
        """;

    [Fact]
    public void Parse_ExtrahiertKundennummer_TrotzVerschmolzenerAdresseInDerSelbenZeile()
    {
        var ergebnis = AuftragsinformationParser.Parse(MalicoSeite1KopfMitZweitemKdNr);
        Assert.Equal("717216", ergebnis.Kundennummer);
    }

    [Fact]
    public void Parse_ExtrahiertKundennameAlsKurzreferenz_WennInlineHinterAdresse()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite1);
        Assert.Equal("GUILLIN, Olesnica", ergebnis.Kundenname);
    }

    [Fact]
    public void Parse_ExtrahiertKundenadresse_GetrenntVomFirmennamen_OhneAngehaengteKdNr()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite1);
        // Adresse getrennt vom Namen: der führende Firmenname ("Guillin Polska Sp. z o.o.")
        // gehört NICHT in die Adresse.
        Assert.Equal("ul. Przemysłowa 3 56-400 OLESNICA POLEN", ergebnis.Kundenadresse);
    }

    [Fact]
    public void Parse_ExtrahiertKundenname_WennKurzreferenzAufEigenerFolgezeileSteht()
    {
        var ergebnis = AuftragsinformationParser.Parse(PlaszomSeite1Kopf);
        Assert.Equal("712082", ergebnis.Kundennummer);
        Assert.Equal("PLASZOM, Orleans - SC", ergebnis.Kundenname);
        Assert.Equal("PO Box 06 ORLEANS - SC 88870-000 BRASILIEN", ergebnis.Kundenadresse);
    }

    [Fact]
    public void Parse_ImExportfall_NimmtHauptkundenNichtWarenempfaenger()
    {
        var ergebnis = AuftragsinformationParser.Parse(MalicoSeite1KopfMitZweitemKdNr);
        Assert.Equal("717216", ergebnis.Kundennummer);
        // Die einzige Kurzreferenz "AL-SULAYMANIA, Karbala Governorate" gehört zum Warenempfänger
        // (Kd.Nr. 717220) und darf NICHT als Kundenname übernommen werden. Ohne Kurzreferenz zum
        // Hauptkunden fällt der Name auf den führenden Firmenteil zurück.
        Assert.DoesNotContain("SULAYMANIA", ergebnis.Kundenname ?? "");
        Assert.Equal("Malico General Trading FZCO", ergebnis.Kundenname);
        // Adresse getrennt vom Firmennamen gespeichert.
        Assert.StartsWith("PO Box", ergebnis.Kundenadresse);
        Assert.DoesNotContain("Malico", ergebnis.Kundenadresse!);
        Assert.Contains("VEREINIGTE ARABISCHE EMIRATE", ergebnis.Kundenadresse);
    }

    // Wörtlicher Kopfbereich (Azure-Content-Stil) aus AI_11055639_30_RDK80k_Veripack_20250606.pdf.
    // Adressbeginn per spanischer Straßen-Kennung "C/".
    private const string VeripackSeite1Kopf = """
        Termin: 12.12.2025
        Kd.Nr. 708296
        Veripack Embalajes, S.L. C/ Mogoda 26-64 Pol. Ind. Can Salvatella 08210 BARBERÀ DEL VALLÈS SPANIEN
        Kd.Nr. 708296 VERIPACK, Barberà del Vallès
        Auftragsinformation
        Auftragsnr .:
        11055639 / 30
        """;

    // Wörtlicher Kopfbereich aus AI_11055751_30_RDKP72k_Alphaform_20250930.pdf. Adressbeginn per
    // französischer Postfach-Kennung "Boîte"; die Kurzreferenz steht erst nach dem Titel.
    private const string AlphaformSeite1Kopf = """
        Termin: 26.06.2026
        Kd.Nr. 250154
        ALPHAFORM S.A.S. Boîte post. 23 26240 BEAUSEMBLANT FRANKREICH
        Auftragsinformation
        Sachbearbeiter/in:
        Herr Albrecht
        Kd.Nr. 250154 ALPHAFORM, Beausemblant
        Auftragsnr .:
        11055751 / 30
        """;

    [Fact]
    public void Parse_ExtrahiertKundennameUndAdresse_MitSpanischerStrassenkennung()
    {
        var ergebnis = AuftragsinformationParser.Parse(VeripackSeite1Kopf);
        Assert.Equal("708296", ergebnis.Kundennummer);
        Assert.Equal("VERIPACK, Barberà del Vallès", ergebnis.Kundenname);
        Assert.Equal(
            "C/ Mogoda 26-64 Pol. Ind. Can Salvatella 08210 BARBERÀ DEL VALLÈS SPANIEN",
            ergebnis.Kundenadresse);
    }

    [Fact]
    public void Parse_ExtrahiertKundennameUndAdresse_MitFranzoesischerPostfachkennung()
    {
        var ergebnis = AuftragsinformationParser.Parse(AlphaformSeite1Kopf);
        Assert.Equal("250154", ergebnis.Kundennummer);
        Assert.Equal("ALPHAFORM, Beausemblant", ergebnis.Kundenname);
        Assert.Equal("Boîte post. 23 26240 BEAUSEMBLANT FRANKREICH", ergebnis.Kundenadresse);
    }

    [Fact]
    public void Parse_ExtrahiertMaschinentyp_TrotzZweitemKdNrDirektVorAuftragsnummerAufSeite1()
    {
        var kombiniert = MalicoSeite1KopfMitZweitemKdNr + "\n" + MalicoSeite2Und3Fuss;
        var ergebnis = AuftragsinformationParser.Parse(kombiniert);
        Assert.Equal("RDM 75Kc_konf_ab (877)", ergebnis.Maschinentyp);
    }

    [Fact]
    public void Parse_ExtractsMerkmalMitNummerAufEigenerZeile()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite1);

        var merkmal = Assert.Single(ergebnis.Merkmale, m => m.Position == "40");
        Assert.Equal("9209425", merkmal.Merkmalsnummer);
        Assert.Contains("ILLIG Druckluft-Formungsautomat", merkmal.Beschreibung);
    }

    [Fact]
    public void Parse_UebersprintPositionOhneNumerischeMerkmalsnummer()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite1);

        Assert.DoesNotContain(ergebnis.Merkmale, m => m.Position == "40/10");
    }

    [Fact]
    public void Parse_ExtrahiertMerkmalsnummerAufFolgezeile()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite1);

        var merkmal = Assert.Single(ergebnis.Merkmale, m => m.Position == "40/20");
        Assert.Equal("025011", merkmal.Merkmalsnummer);
        Assert.Contains("Achtung, Grundmaschine mit neuem HMI Bedienfeld!", merkmal.Beschreibung);
    }

    [Fact]
    public void Parse_ExtrahiertMerkmalsnummerAusZeileMitVorangestelltemText()
    {
        var kombiniert = Seite1 + "\n" + Seite2Anschluss;
        var ergebnis = AuftragsinformationParser.Parse(kombiniert);

        var merkmal = Assert.Single(ergebnis.Merkmale, m => m.Position == "40/30");
        Assert.Equal("024933", merkmal.Merkmalsnummer);
        Assert.Contains("Mit folgender Konfiguration:", merkmal.Beschreibung);
        Assert.Contains("Formungsautomat für Rollenfolie", merkmal.Beschreibung);
    }

    [Fact]
    public void Parse_SetztTabelleUeberSeitengrenzeHinwegFort()
    {
        var kombiniert = Seite1 + "\n" + Seite2Anschluss;
        var ergebnis = AuftragsinformationParser.Parse(kombiniert);

        Assert.Contains(ergebnis.Merkmale, m => m.Position == "40" && m.Merkmalsnummer == "9209425");
        Assert.Contains(ergebnis.Merkmale, m => m.Position == "40/30" && m.Merkmalsnummer == "024933");
    }

    [Fact]
    public void Parse_ExtractsDatum()
    {
        var kombiniert = Seite1 + "\n" + Seite2Anschluss;
        var ergebnis = AuftragsinformationParser.Parse(kombiniert);
        Assert.Equal(new DateOnly(2025, 5, 21), ergebnis.Datum);
    }

    [Fact]
    public void Parse_LiefertNullDatum_WennNichtGefunden()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite1);
        Assert.Null(ergebnis.Datum);
    }

    [Fact]
    public void Parse_LiefertNullDatum_WennFormatNichtErkannt()
    {
        var ergebnis = AuftragsinformationParser.Parse("Datum: 21.05.2025");
        Assert.Null(ergebnis.Datum);
    }

    [Fact]
    public void Parse_LiefertLeereMerkmalsliste_WennKeinTabellenkopfVorhanden()
    {
        var ergebnis = AuftragsinformationParser.Parse("Irgendein Text ohne Tabelle.");
        Assert.Empty(ergebnis.Merkmale);
    }

    [Fact]
    public void Parse_LiefertLeereAuftragsnummer_WennNichtGefunden()
    {
        var ergebnis = AuftragsinformationParser.Parse("Irgendein Text ohne Tabelle.");
        Assert.Equal("", ergebnis.Auftragsnummer);
    }

    [Fact]
    public void Parse_ErkenntWiederholtenTabellenkopf_OhneAbschlussmarkierungDazwischen()
    {
        const string text = """
            Auftragsnr .:
            11055627 / 40
            Pos.
            Vertriebsmerkmale
            40
            9209425
            Beschreibung A
            Pos.
            Vertriebsmerkmale
            40/10
            111222
            Beschreibung B
            """;

        var ergebnis = AuftragsinformationParser.Parse(text);

        var erstesMerkmal = Assert.Single(ergebnis.Merkmale, m => m.Position == "40");
        Assert.Equal("9209425", erstesMerkmal.Merkmalsnummer);

        var zweitesMerkmal = Assert.Single(ergebnis.Merkmale, m => m.Position == "40/10");
        Assert.Equal("111222", zweitesMerkmal.Merkmalsnummer);
    }

    // Wörtlicher Auszug aus Seite 9 des Referenzdokuments — verifiziert den Übergang von
    // normalen Merkmalen zu Sonderoptionen. "Sonderoption:" steht hier zweimal allein auf einer
    // Zeile, taucht bei 40/420 aber auch am Ende der Beschreibungszeile auf ("...spannungsfrei.
    // Sonderoption:") — beide Formen müssen den Merkmalsnummer-Fund nicht stören.
    private const string Seite9Sonderoptionsbeginn = """
        Pos.
        Vertriebsmerkmale
        Maschinenfarbe-Varianten:
        40/410
        020000 Maschinenfarbe: RAL 7035 mit RAL 5013
        Sonderoption:
        Sonderoption:
        40/420
        023771 Die RS wird über die RDK Maschine mit 400V Versorgt. Dafür wird im Formmaschinenschaltschrank eine Klemmeliste zu Verfügung gestellt. Wenn der Hauptschalter an der RDK aus ist, dann ist die Nebenmaschinen spannungsfrei. Sonderoption:
        40/430
        018093 Sonder Kühlwasser-Anschlüsse 4-fach An der Formstation ist an der Verteilerleiste ein zusätzlicher Werkzeug-Kühlwasser-Anschluss vorgesehen.
        """;

    [Fact]
    public void Parse_TrenntSonderoptionenVonNormalenMerkmalen()
    {
        var ergebnis = AuftragsinformationParser.Parse(Seite9Sonderoptionsbeginn);

        var normalesMerkmal = Assert.Single(ergebnis.Merkmale, m => m.Position == "40/410");
        Assert.Equal("020000", normalesMerkmal.Merkmalsnummer);
        Assert.DoesNotContain("Sonderoption", normalesMerkmal.Beschreibung);

        Assert.DoesNotContain(ergebnis.Merkmale, m => m.Position == "40/420" || m.Position == "40/430");

        var ersteSonderoption = Assert.Single(ergebnis.Sonderoptionen, m => m.Position == "40/420");
        Assert.Equal("023771", ersteSonderoption.Merkmalsnummer);
        Assert.DoesNotContain("Sonderoption", ersteSonderoption.Beschreibung);

        var zweiteSonderoption = Assert.Single(ergebnis.Sonderoptionen, m => m.Position == "40/430");
        Assert.Equal("018093", zweiteSonderoption.Merkmalsnummer);
    }

    [Fact]
    public void Parse_ErkenntSonderoptionMarkerVorMerkmalsnummerAufGleicherZeile()
    {
        // Wörtlicher Auszug Seite 10: "Sonderoption:" steht direkt vor der Merkmalsnummer auf
        // derselben Zeile, nicht wie sonst auf einer eigenen Zeile davor.
        const string seite10 = """
            Pos.
            Vertriebsmerkmale
            40/440
            Sonderoption: 020182 Sonderablauf für die servomotorische Transportspreizung Normalerweise fährt die Spreizung zurück, wenn die Maschine gestoppt wird.
            """;

        var ergebnis = AuftragsinformationParser.Parse(seite10);

        var sonderoption = Assert.Single(ergebnis.Sonderoptionen, m => m.Position == "40/440");
        Assert.Equal("020182", sonderoption.Merkmalsnummer);
        Assert.Contains("Sonderablauf für die servomotorische Transportspreizung", sonderoption.Beschreibung);
        Assert.DoesNotContain("Sonderoption", sonderoption.Beschreibung);
    }

    // Nachbau des Malico-Falls (AI_11055894...): Jede Sonderoption hat ihren EIGENEN
    // "Sonderoption:"-Marker (40/330, 40/340, 40/350). Nach dem letzten folgen wieder normale
    // Merkmale (40/360 "…-Varianten:", 40/370 …). Der Marker darf also NICHT klebrig sein und alle
    // folgenden Positionen mitziehen — sonst landen 40/370 ff. fälschlich in den Sonderoptionen.
    private const string MalicoSonderoptionenDannWiederNormal = """
        Pos.
        Vertriebsmerkmale
        Maschinenfarbe-Varianten:
        40/320
        020000
        Maschinenfarbe: RAL 7035 mit RAL 5013
        40/330
        Sonderoption:
        026034
        Vakuumeinrichtung wartungsarm mit 2 Vakuumkreisen
        40/340
        Sonderoption:
        022170
        Heizungstunnel zur verbesserten Beheizung des Folienbands.
        40/350
        Sonderoption:
        026033
        Verkettungspaket für RDML 75
        40/360
        Bedienerführung-Varianten:
        40/370
        9046049
        Bedienerführung in Englisch
        """;

    [Fact]
    public void Parse_MarkerGiltNurFuerFolgendePosition_NichtKlebrigFuerAlleWeiteren()
    {
        var ergebnis = AuftragsinformationParser.Parse(MalicoSonderoptionenDannWiederNormal);

        // Genau 40/330–40/350 sind Sonderoptionen (je eigener Marker).
        Assert.Equal("026034", Assert.Single(ergebnis.Sonderoptionen, m => m.Position == "40/330").Merkmalsnummer);
        Assert.Equal("022170", Assert.Single(ergebnis.Sonderoptionen, m => m.Position == "40/340").Merkmalsnummer);
        Assert.Equal("026033", Assert.Single(ergebnis.Sonderoptionen, m => m.Position == "40/350").Merkmalsnummer);

        // Davor UND danach normale Merkmale — insbesondere 40/370 darf nicht mitgezogen werden.
        Assert.Equal("020000", Assert.Single(ergebnis.Merkmale, m => m.Position == "40/320").Merkmalsnummer);
        Assert.Equal("9046049", Assert.Single(ergebnis.Merkmale, m => m.Position == "40/370").Merkmalsnummer);

        Assert.DoesNotContain(ergebnis.Sonderoptionen, m => m.Position == "40/320" || m.Position == "40/370");
        Assert.DoesNotContain(ergebnis.Merkmale, m => m.Position is "40/330" or "40/340" or "40/350");
    }

    [Fact]
    public void Parse_ExtrahiertMerkmalsnummerAusZeileMitNachgestelltemText()
    {
        // Wörtlicher Auszug Seite 4: die Merkmalsnummer steht am Anfang einer langen
        // Beschreibungszeile statt allein auf einer eigenen Zeile.
        const string seite4 = """
            Pos.
            Vertriebsmerkmale
            40/40
            Prozessoptimierung-Varianten:
            014936 Dynamische Prozessoptimierung für Taktzahlen bis zu 55 Takten/min im Form- und Form-/Stanzbetrieb.
            """;

        var ergebnis = AuftragsinformationParser.Parse(seite4);

        var merkmal = Assert.Single(ergebnis.Merkmale, m => m.Position == "40/40");
        Assert.Equal("014936", merkmal.Merkmalsnummer);
        Assert.Contains("Dynamische Prozessoptimierung", merkmal.Beschreibung);
    }

    [Fact]
    public void Parse_ExtrahiertPositionUndMerkmalsnummerAufGleicherZeile()
    {
        // Wörtlicher Auszug Seite 6: Position und Merkmalsnummer stehen zusammen auf einer
        // Zeile ("40/240 021746"), anders als bei den meisten anderen Positionen.
        const string seite6 = """
            Pos.
            Vertriebsmerkmale
            Stapelsystem-Varianten:
            40/240 021746
            Stapelstation mit Zähleinrichtung und Zweiachsen Handlingssystem servomotorisch angetrieben.
            """;

        var ergebnis = AuftragsinformationParser.Parse(seite6);

        var merkmal = Assert.Single(ergebnis.Merkmale, m => m.Position == "40/240");
        Assert.Equal("021746", merkmal.Merkmalsnummer);
        Assert.Contains("Stapelstation mit Zähleinrichtung", merkmal.Beschreibung);
    }

    [Fact]
    public void Parse_ErgaenztFehlendeMerkmaleAusSeitenweisenLayoutZeilen()
    {
        const string content = """
            Auftragsinformation
            Pos.
            Vertriebsmerkmale
            40/70
            010770 Schmierung fuer Transportketten automatisch
            40/100
            018562 Vorstreckstempel servomotorisch
            40/130
            020119 Temperatur-Regelung Werkzeug
            40/150
            020578 Vakuumeinrichtung Einbau vorbereitet
            """;
        const string layoutContent = """
            Pos. Vertriebsmerkmale_______________________________________________________________________
            40/70 010770
            Schmierung fuer Transportketten automatisch
            Heizungsregelung-Varianten:
            40/80 017364
            Unterheizung mit Laengsreihenregelung
            40/90 010745
            Aktive Strahlerfunktionskontrolle
            Pos. Vertriebsmerkmale_______________________________________________________________________
            40/100 018562
            Vorstreckstempel servomotorisch
            40/130 020119
            Temperatur-Regelung Werkzeug
            40/140 012516
            Anschluss fuer Temperaturfuehler im Werkzeugblock
            40/150 020578
            Vakuumeinrichtung Einbau vorbereitet
            """;

        var ergebnis = AuftragsinformationParser.Parse(content, layoutContent);

        Assert.Contains(ergebnis.Merkmale,
            m => m.Position == "40/80" && m.Merkmalsnummer == "017364");
        Assert.Contains(ergebnis.Merkmale,
            m => m.Position == "40/90" && m.Merkmalsnummer == "010745");
        Assert.Contains(ergebnis.Merkmale,
            m => m.Position == "40/140" && m.Merkmalsnummer == "012516");
        Assert.Single(ergebnis.Merkmale,
            m => m.Position == "40/70" && m.Merkmalsnummer == "010770");
        Assert.Single(ergebnis.Merkmale,
            m => m.Position == "40/100" && m.Merkmalsnummer == "018562");
    }

    [Fact]
    public void Parse_PriorisiertZeilenweiseTabellenzellenVorAbweichenderLesereihenfolge()
    {
        const string content = """
            Pos.
            Vertriebsmerkmale
            40/70
            010770 Schmierung fuer Transportketten
            """;
        const string seitenLayoutContent = """
            Pos. Vertriebsmerkmale
            40/80
            Heizungsregelung-Varianten:
            017364 Unterheizung mit Laengsreihenregelung
            40/90
            010745 Aktive Strahlerfunktionskontrolle
            """;
        const string tabellenLayoutContent = """
            Pos.
            Vertriebsmerkmale
            40/80
            017364
            Unterheizung mit Laengsreihenregelung
            40/90
            010745
            Aktive Strahlerfunktionskontrolle
            40/140
            012516
            Anschluss fuer Temperaturfuehler im Werkzeugblock
            """;

        var ergebnis = AuftragsinformationParser.Parse(
            content, seitenLayoutContent, tabellenLayoutContent);

        Assert.Equal("017364", Assert.Single(ergebnis.Merkmale, m => m.Position == "40/80").Merkmalsnummer);
        Assert.Equal("010745", Assert.Single(ergebnis.Merkmale, m => m.Position == "40/90").Merkmalsnummer);
        Assert.Equal("012516", Assert.Single(ergebnis.Merkmale, m => m.Position == "40/140").Merkmalsnummer);
    }

    [Fact]
    public void Parse_TabellenzellenUeberschreibenKeinBereitsErkanntesMerkmal()
    {
        const string content = """
            Pos.
            Vertriebsmerkmale
            40/20
            025011 Folienrollenaufnahme
            40/30
            024929 Vorheizung
            40/40
            020621 Folieneinlauf
            """;
        const string tabellenLayoutContent = """
            Pos.
            Vertriebsmerkmale
            Sonderoption:
            40/30
            999999 Falsch zugeordneter Tabellenwert
            40/80
            017364 Unterheizung mit Laengsreihenregelung
            """;

        var basisErgebnis = AuftragsinformationParser.Parse(content);
        Assert.Equal("024929", Assert.Single(
            basisErgebnis.Merkmale, m => m.Position == "40/30").Merkmalsnummer);

        var ergebnis = AuftragsinformationParser.Parse(
            content, tabellenLayoutContent: tabellenLayoutContent);

        var position40_30 = Assert.Single(ergebnis.Merkmale, m => m.Position == "40/30");
        Assert.Equal("024929", position40_30.Merkmalsnummer);
        Assert.DoesNotContain(ergebnis.Sonderoptionen, m => m.Position == "40/30");
        Assert.Contains(ergebnis.Merkmale.Concat(ergebnis.Sonderoptionen),
            m => m.Position == "40/80" && m.Merkmalsnummer == "017364");
    }

    [Fact]
    public void Parse_GleichesLayoutMerkmalErsetztNurVerschmutzteBeschreibung()
    {
        const string content = """
            Pos.
            Vertriebsmerkmale
            40/70
            010770 Schmierung automatisch Heizungsregelung-Varianten: 40/80 40/90 017364 Unterheizung Kundendaten Auftragsnr.: 11055894 / 40
            """;
        const string positionsLayoutContent = """
            Pos.
            Vertriebsmerkmale
            40/70
            010770 Schmierung automatisch
            """;

        var ergebnis = AuftragsinformationParser.Parse(
            content, tabellenLayoutContent: positionsLayoutContent);

        var position70 = Assert.Single(ergebnis.Merkmale);
        Assert.Equal("40/70", position70.Position);
        Assert.Equal("010770", position70.Merkmalsnummer);
        Assert.Equal("Schmierung automatisch", position70.Beschreibung);
        Assert.Empty(ergebnis.Sonderoptionen);
    }

    [Fact]
    public void Parse_TrenntLayoutQuellenDamitFooterNichtAnLetztePositionGeraet()
    {
        const string content = """
            Pos.
            Vertriebsmerkmale
            Sonderoption:
            120/10
            015726 Grundeinweisung 2,0 Tage Sachbearbeiter/in: Herr Butz Seite: 1/8 Auftragsnr.: 11055894 / 40
            """;
        const string positionsLayoutContent = """
            Pos.
            Vertriebsmerkmale
            120/10
            015726
            Grundeinweisung 2,0 Tage
            """;
        const string tabellenLayoutContent = """
            Sachbearbeiter/in: Herr Butz
            Seite: 1/8
            Auftragsnr.: 11055894 / 40
            Pos.
            Vertriebsmerkmale
            40/20
            025011 Grundmaschine
            """;

        var ergebnis = AuftragsinformationParser.Parse(
            content,
            tabellenLayoutContent: tabellenLayoutContent,
            positionsLayoutContent: positionsLayoutContent);

        var position120_10 = Assert.Single(
            ergebnis.Sonderoptionen, m => m.Position == "120/10");
        Assert.Equal("015726", position120_10.Merkmalsnummer);
        Assert.Equal("Grundeinweisung 2,0 Tage", position120_10.Beschreibung);
    }

    [Fact]
    public void Parse_UebersprintPositionOhneMerkmalsnummer_TrotzKurzerZahlImText()
    {
        // Wörtlicher Auszug Seite 15: "8,0 Tage" enthält keine 5-8-stellige Zahl und darf nicht
        // fälschlich als neue Position ("8") oder Merkmalsnummer interpretiert werden.
        const string seite15Auszug = """
            Pos.
            Vertriebsmerkmale
            100/10-A
            8,0 Tage
            110
            9195771
            Commissioning at customer
            """;

        var ergebnis = AuftragsinformationParser.Parse(seite15Auszug);

        Assert.DoesNotContain(ergebnis.Merkmale, m => m.Position == "100/10-A");
        var merkmal = Assert.Single(ergebnis.Merkmale, m => m.Position == "110");
        Assert.Equal("9195771", merkmal.Merkmalsnummer);
        Assert.Contains("Commissioning at customer", merkmal.Beschreibung);
    }

    // Wörtlicher, vollständiger Content aller 15 Seiten des Referenzdokuments
    // 11055627_40_RDK80k_Guillin20252105_Auftragsinformation.pdf, wie vom Nutzer aus dem echten
    // AnalyzeResult.Content geliefert (Task 12 — End-to-End-Verifikation, insbesondere für den
    // bisher ungeprüften Sonderoptionen-Bereich auf Seite 9-15).
    private const string VollstaendigesDokument = """
        Termin: 19.11.2025
        Kd.Nr. 708555
        Guillin Polska Sp. z o.o. ul. Przemysłowa 3 56-400 OLESNICA POLEN Kd.Nr. 708555 GUILLIN, Olesnica
        Auftragsinformation
        Sachbearbeiter/in:
        Herr Mark
        Seite:
        1 / 15
        Heilbronn:
        21. Mai 2025
        Konstruktion:
        Gökcen/Bindereif 05.06.2025
        _
        Auftragszentrum:
        _
        RDK 80k_Siemens_konf_ab_01.2013
        Auftragsnr .:
        11055627 / 40
        Netzplan Nr .:
        1042100
        Serialnr .:
        ( 732_0478 )
        Versandbedingungen: Versand mit:
        Spedition
        20
        AWK 800_konf_ab ( 665_0652 )
        30
        VHW 90/2b ( 690_0771 )
        50
        RS 75b_konf_ab 22.11.2012 ( 567_1716 )
        120
        Versandvorb. Spedition(LKW)
        130
        Freight charges
        140
        Gewährleistung
        150
        Kulanz
        Pos.
        Vertriebsmerkmale
        40
        9209425
        ILLIG Druckluft-Formungsautomat Type RDK 80k
        40/10
        Version 06
        40/20
        025011
        Achtung, Grundmaschine mit neuem HMI Bedienfeld!
        Guillin Polska Sp. z o.o. ul. Przemysłowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 2 / 15
        Pos.
        Vertriebsmerkmale
        40/30
        Mit folgender Konfiguration: 024933
        Formungsautomat für Rollenfolie für den Einsatz von kombinierten Form- und Bandstahlschnittwerkzeugen.
        Formfläche max.
        756 x 565 mm
        Stanzfläche max.
        743 x 552 mm
        Werkzeugbreite zwischen Folientransport max. 780 mm
        Werkzeuglänge in Vorschubrichtung max.
        595 mm
        Formteilhöhe über der Folienebene max.
        120 mm
        Formteilhöhe unter der Folienebene max.
        120 mm
        Gesamtformteilhöhe max.
        120 mm
        Grundmaschine RDK 80k mit folgender Ausstattung:
        - Folieneinlauf für vorgewärmte Folien mit Verkettung und Schnittstelle für Folienrollenaufnahme RO und/oder Walzenvorheizung VHW ohne Feldbus*
        - Klebestellenerkennung manuell über Drucktaster
        - Folientransport servomotorisch (Genauigkeit +/- 0,2 mm), Kettenspannsystem pneumatisch, Kettenstützleisten gehärtet, Spreizeinrichtung servomotorisch und Folientransport wassergekühlt, wahlweise an separates Temperaturniveau anschließbar*,
        - Oberheizung und Unterheizung mit Längsreihenregelung, keramische HTSs Infrarot-Heizungsstrahler, jeweils vier Kleinstrahlerquerreihen einlaufseitig abschaltbar zur Längenanpassung der Heizfläche, Heizungen 1.750 mm lang, bei Maschinenstart an das Werkzeug heranfahrend und Foliendurchhangskontrolle*
        - Druckluftformstation, Ober- und Untertischantrieb servomotorisch, ausgelegt für Bandstahl-Form-/Stanzwerkzeuge und Formwerkzeuge
        - separate Werkzeugkühlung an der Formstation
        - Grenzwertüberwachung mit Schließkraftanzeige
        - Ober- und Unterspannrahmenantrieb pneumatisch mittels Kaskade
        - Druckluft-/Vakuumanschluss für Ober- und Unterwerkzeug verschraubungslos
        - minimierte Zeiten für Formdruckauf- bzw. - abbau mittels hochdynamischer Ventile
        - Formluft-Vorlagedruck von Bedienseite manuell einstellbar
        - Entformluftsteuerung stufenlos
        - Vakuumeinrichtung; Vakuumpumpe wartungsarm
        - Ansteuerung für A/B-Stapelung oder Losteilsteuerung oder Entformhilfe im Formwerkzeug
        - Verkleidung auf Folienauslaufseite (notwendig bei separat stehender Formmaschine), Folienband-Hochhalter (4 Stück) verstellbar am Folienauslauf (entfällt bei Auswahl einer Stapelstation)*
        - Kühlwasserabsperrventil automatisch
        - Regelung der Maschinenkühltemperatur zur Vermeidung von Kondenswasserbildung
        - Ausblaseinrichtung manuell für Werkzeugkühlwasser*
        Guillin Polska Sp. z o.o. ul. Przemysłowa 3 56-400 OLESNICA
        Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 3 / 15
        Pos.
        Vertriebsmerkmale
        - Zentralschmierung automatisch
        - Schwingmetall-Maschinenfüße
        - Takt- und Produktionsstundenzähler
        - Schnellumrüstpaket bestehend aus: Folientransport auf Werkzeugbreite automatisch einstellend, Werkzeugeinbau über T-Nuten, zentrale Spannung des Formwerkzeugs von Bedienseite, digitale Einstellung während des Maschinenlaufs für Folientransportschritt, Folientransportbeschleunigung, Folientransportgeschwindigkeit, Spreizweg, Spannrahmenhub, Spannrahmengeschwindigkeit, Tischhub, Tischgeschwindigkeit, Form-Luftdruck und Entformluft, Werkzeugeinbauhöhenverstellung motorisch am Obertisch;
        Grundeinstellung automatisch; dynamische Prozessoptimierung; Prozessdatenvisualisierung; Einstelldatenspeicherung intern für 9999 Einstelldatensätze
        - Zentrale Visualisierung von Diagnosefunktionen
        - Steuerung Siemens S7 mit SOFT-SPS auf Industrie-PC; Motion Control System für die Koordination der Maschinenfunktionen
        - Touch-Bedienfeld mit ILLIG Easy Touch im schwenkbaren Bedienstand
        - 21" Bedienfeld im schwenkbaren Bedienstand
        - ILLIG EeasyTouch Bedienoberfläche
        - Druckerschnittstelle (USB)
        - rückspeisende Antriebstechnik
        - Energiesparmodus
        - Hardware für ILLIG NetService
        - Maschinenfrontverkleidung mit Schiebetüren
        - Bedienstand an der Maschine verfahrbar
        - Schaltschrank ausgelegt für Umgebungstemperaturen des Schaltschranks bis 40 ℃*
        - LED-Leuchten für Maschinen-Innenraum
        - Kühlmittelführende Teile werden mit einem Korrosionsschutzmittel behandelt. Die in der Betriebsanleitung empfohlene Wasserqualität muss durch den Betreiber sichergestellt werden. Wenn die kühlmittelführenden Teile ausschließlich aus den Werkstoffen Edelstahl und Kunststoff bestehen, ist die Behandlung nicht erforderlich
        - Drehstromversorgung mit belastbarem Neutralleiter, Stromversorgung: 3/PEN (N/PE), 50 Hz, 400 V, TN*
        - Maschinenfarbe: RAL 7035 mit RAL 5013*
        - Bedienerführung in Deutsch oder Englisch*
        - Dokumentation in Deutsch oder Englisch*
        - Schaltpläne in Deutsch oder Englisch*
        Hinweis: Bei Verlängerung der Maschine um eine Lochstanze oder ein Zwischengestell kann der Form-/Stanzantrieb nur bedingt eingesetzt werden.
        * Die gekennzeichnete Ausführung kann bei alternativen Ausstattungsvarianten nachstehend wiederholt als "inkl."-Position aufgeführt sein.
        Guillin Polska Sp. z o.o. ul. Przemyslowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 4 / 15
        Pos.
        Vertriebsmerkmale
        40/40
        Prozessoptimierung-Varianten:
        014936 Dynamische Prozessoptimierung für Taktzahlen bis zu 55 Takten/min im Form- und Form-/Stanzbetrieb; Bedienerführung mit rechnergestützter Parameterauswahl zur einfachen Optimierung der Taktzahl; zum Erreichen der erhöhten Taktzahl werden wassergekühlte Antriebe der Druckluftformstation und Bandstahlstanze, Endlagedämpfung am Untertisch der Druckluftformstation eingebaut. Folgende Ausstattungen sind zusätzlich erforderlich:
        - Folieneinlauf für vorgewärmte Folien für
        Folienrollenaufnahmen ROK , AWK und/oder Walzenvorheizung VHW mit
        Feldbus.
        Folieneinlauf-/Schnittstelle-Varianten:
        40/50
        011460
        Folieneinlauf für vorgewärmte Folien mit Verkettung und Schnittstelle für
        Folienrollenaufnahme ROK, Abwickeleinheit AWK und/oder Walzenvorheizung mit Feldbus.
        40/60
        014590 Folienband Querverstellung am Folieneinlauf. Bei Verarbeitung von vorbedruckter Folie empfohlen.
        40/70
        7002863 Folientransportkettenschmierung automatisch. Erstbefüllung mit
        lebensmitteltverträglichem Schmierstoff. Heizungsregelung-Varianten:
        40/80
        014919 Oberheizung und Unterheizung mit Längsreihenregelung; jeweils vier Kleinstrahlerquerreihen einlaufseitig abschaltbar zur Längenanpassung der Heizfläche, zusätzlich eine Kleinstrahlerquerreihe werkzeugseitig einzeln regelbar
        Heizungsabdeckungen-Auswahloptionen:
        40/90
        011354 Heizungsabdeckungen wassergekühlt 125 mm breit mit Parkposition
        40/100
        019852 Luftunterstützung der Foliendurchhangskontrolle
        40/110
        017710 Spreizeinrichtung servomotorisch, zusätzlich vor Formstation, Folientransport in der Formstation parallel geführt.
        40/120
        019853 Oberbrücke und Obertisch spielfrei an der der Form-/Stanzstation
        Guillin Polska Sp. z o.o. ul. Przemyslowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 5 / 15
        Pos.
        Vertriebsmerkmale
        Formluft-Vorlagedruck Einstellung-Varianten:
        40/130
        020373 Formluft-Vorlagedruck von Bedienseite manuell einstellbar
        Spannrahmenantrieb-Varianten:
        40/140
        000741 Oberspannrahmen-,
        Vorstreckstempelantrieb servomotorisch. Werkzeugkühl./temp .- Varianten:
        40/150
        014915 Separate Werkzeugkühlung an der
        Formstation für zweiten Kühlkreislauf (zwei unterschiedliche Temperaturniveaus möglich).
        [Hinweis: für CPET-Verarbeitung
        empfohlen]
        Ausblaseinrichtungs-Varianten:
        40/160
        017308 Ausblaseinrichtung automatisch für zwei Werkzeugkühlwasserkreisläufe
        40/170
        014917 Anzeige für Durchflußmenge, Vorlauf- und Rücklauftemperatur für zwei Werkzeugkühlkreisläufe. Abhängigkeit von anderen Optionen beachten!
        Vorrichtung für Werkzeugwechsel-Varianten Formstation:
        40/180
        014940 Vorrichtung für Werkzeugwechsel an der Formstation.
        40/190
        017311 Bandstahlstanze, Ober- und Untertischantrieb servomotorisch (Stanzkraft 600 KN);
        Grenzwertüberwachung mit Schließkraftanzeige; Folientransport automatisch auf Werkzeugbreite einstellend; Stanzposition entsprechend Transportschritt servomotorisch einstellend; Werkzeugklemmung pneumatisch;
        Werkzeugeinbauhöhenverstellung motorisch am Ober- und Untertisch;
        Stationszuschaltung und -abschaltung;
        Stanzprogramme; Verstelleinrichtung für Bandstahlschnitt (Quer- und Winkellage) während des Maschinenlaufs, am Ober- und Untertisch eingebaut.
        40/200
        019863 Oberbrücke und Obertisch spielfrei an der Bandstahlstanze
        40/210
        000722 Bandstahlschnittheizung am Obertisch (Heiz-, Kühl- und Isolierplatte).
        40/220
        000723 Bandstahlschnittheizung am Untertisch (Heiz-, Kühl- und Isolierplatte).
        Guillin Polska Sp. z o.o. ul. Przemyslowa 3 56-400 OLESNICA
        Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 6 / 15
        Pos.
        Vertriebsmerkmale
        Vorrichtung für Werkzeugwechsel-Varianten Bandstahlstanze:
        40/230
        014949 Vorrichtung für Werkzeugwechsel an der Bandstahlstanze.
        Stapelsystem-Varianten:
        40/240 021746
        Stapelstation mit Zähleinrichtung und Zweiachsen Handlingssystem servomotorisch angetrieben;
        - Antrieb für Klemm- und Ausbrechbewegung servomotorisch
        - Stapelbildung durch Handlingssystem: eine Saugerplatte übernimmt die ausgebrochene Teile, die durch das Zweiachsen Handlingssystem auf das Austransportband abgelegt und aufgestapelt werden
        - Wechselstapelung für A/B und A/B/C Anordnung bei Stapelbildung auf dem Austransportband möglich
        - Alternativ Stapelbildung im Stapelschacht: vorhandene Stapelformatteile einsetzbar
        - Hohe Positioniergenauigkeit beim Aufnehmen und Ineinanderstapeln der Ziehteile durch ruckfreie Achsenantriebe
        - Automatische Verrechnung aller relevanten Achsenbahnen- und Geschwindigkeiten in der zentralen Maschinensteuerung
        - einfaches Einstellen der Ausbrecher- und Saugerplattenhöhen (Formatteile) im Teach-in Verfahren am zentralen Bedienpanel.
        - Folientransport auf Werkzeugbreite automatisch einstellend; Spreizeinrichtung servomotorisch
        - Stapelposition entsprechend Transportschritt servomotorisch einstellend
        - Stationszuschaltung und -abschaltung
        - Stapelprogramme
        [Hinweis: Verkleidung auf Folienauslaufseite (notwendig bei separat stehender Formmaschine) und Folienband-Hochhalter (4 Stück) verstellbar am Folienauslauf entfallen].
        Guillin Polska Sp. z o.o. ul. Przemyslowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 7 / 15
        Pos.
        Vertriebsmerkmale
        40/250
        Stapelaustransport-Varianten:
        019868 Abstapel- und Austransportband, servomotorisch angetrieben, Abstapelband, auf das die Teile durch das Handlingssystem gestapelt werden; Austransportband, zur Übernahme der
        Teilestapel vom vorgesetzten Abstapelband, als Pufferband nutzbar,
        Gesamtlänge 4150 mm, Abstapelband 900 mm waagerecht und
        Austransportband 3270 mm mit 6,5° Neigung, zur Reduzierung der Abnahmehöhe; Geschwindigkeit zum Auseinanderziehen der Stapel einstellbar, Höhenverstellung mechanisch, mechanisch höhenverstellbare Stützfüße mit Laufrollen; über Drucktaster ist eine
        Leerfahrfunktion auch während eines Stapelvorgangs möglich (Entkoppelung vom Maschinentakt)
        [Hinweis: bei Verkettung mit Stanzgittermahleinrichtung ist der Stanzgittermühleneinschub von Bedienseite erforderlich].
        40/260
        014955 Wechselstapelung in Durchlaufrichtung servomotorisch in Verbindung mit
        Stapelung im Stapelschacht und Zweiachsen Handlingsystem im Ausschiebebetrieb.
        40/270
        7002874 Ansteuerung für pneumatische Wechsel-Stapelung in der Stapelstation (erforderlich bei Wechselstapelung in der Stapelstation)
        40/280
        017313 Vakuumeinrichtung an der Stapelstation zur Fixierung der Formteile auf dem Ausbrecher. Zwei Vakuumkreisläufe für Wechselstapelung.
        40/290
        019871 Ionisiereinrichtung an der Stapelstation
        40/300
        014882 ThermoLineControl bietet für alle über Feldbus verbundenen Einzelmaschinen (getrennt zu bestellen) folgenden Nutzen am zentralen Leitstand der Linie:
        - Rezepturenverwaltung und Einstelldatenspeicherung
        - Darstellung der Prozessparameter (Soll-/Istwerte) der Maschinen einer Linie - Meldewesen der Maschinen einer Linie - Bedienerführung beim Werkzeugwechsel der Maschinen einer Linie
        Guillin Polska Sp. z o.o. ul. Przemysłowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 8 / 15
        Pos.
        Vertriebsmerkmale
        40/310
        014903
        Kompensation: Verbesserung der Reproduzierbarkeit der Heizergebnisse insbesondere des Temperaturprofils des Halbzeuges über die Halbzeugdicke. Berücksichtigt werden Gestellerwärmung der Maschine und
        Halbzeugeingangstemperatur. Je nach
        Einfluss werden die Strahlertemperaturen automatisch angepasst.
        40/320
        020440 Aktivierung ILLIG NetService
        Mit der Aktivierung von ILLIG NetService wird die internetgestützte Ferndiagnose und Wartung von Maschinen, deren
        Komponenten mit Ethernetschnittstellen (Steuerung, Visualisierung, Antriebe, Messtechnik, usw.) ausgestattet sind, während des gesamten Zeitraums der
        Haftung/Gewährleistung ermöglicht. ILLIG NetService kann weltweit eingesetzt werden, kundenseitige Voraussetzung ist
        lediglich ein Internetzugang.
        Schaltschrankkühlung-Varianten:
        40/330
        016075 Schaltschrank ausgelegt für
        Umgebungstemperaturen des Schaltschranks bis 50 ℃.
        Elektrische Schnittstelle Temperiergerät-Auswahlvarianten:
        40/340
        017040 Schnittstelle über Feldbus für ein
        Temperiergerät ITG 95 inkl. Feldbuskabel,
        Steuerung /
        Softwareanbindung.(Elektrischer
        Anschluss kundenseitig).
        Mechanische Schnittstelle Temperiergerät-Auswahlvarianten:
        40/350
        017041 Schnittstelle für ein Temperiergerät ITG 95, Verbindungschläuche und -material. (Elektrischer Anschluss kundenseitig)
        40/360
        024990 Steckdosen Temperiergerät - Auswahloptionen:
        40/370
        019889 Maschinen-Steckdose, zum Anschluss eines Temperiergerätes Schnittstelle Stanzgitter-Handling-Auswahlvarianten:
        40/380
        019874 Schnittstelle für
        ILLIG-Stanzgittermahleinrichtung mit Feldbus nach Illig-Standard. ILLIG-Stanzgittermahleinrichtung erforderlich.
        40/390
        019875 Stanzgitteraufwickeleinrichtung mit pneumatischer Rollenausschiebeeinrichtung, Wickelkontrolle und Rollendurchmesserüberwachung.
        Guillin Polska Sp. z o.o. ul. Przemyslowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 9 / 15
        Pos.
        Vertriebsmerkmale
        Stromversorgung-Varianten:
        40/400
        9020016 Drehstromversorgung mit belastbarem Neutralleiter
        Stromversorgung: 3/PEN, (N/PE), 50 Hz, 400 V, TN 3 Außenleiter (L1/L2/L3) 1 Neutralleiter belastbar (N) 1 Schutzleiter (PE) Neutralleiter (N) und Schutzleiter (PE) können zu einem PEN-Leiter zusammengefasst sein. Nennspannung 400 V Frequenz 50 Hz Netzform TN-System Maschinenfarbe-Varianten:
        40/410
        020000 Maschinenfarbe: RAL 7035 mit RAL 5013
        Sonderoption:
        Sonderoption:
        40/420
        023771 Die RS wird über die RDK Maschine mit 400V Versorgt. Dafür wird im Formmaschinenschaltschrank eine Klemmeliste zu Verfügung gestellt. Wenn der Hauptschalter an der RDK aus ist, dann ist die Nebenmaschinen spannungsfrei. Sonderoption:
        40/430
        018093 Sonder Kühlwasser-Anschlüsse 4-fach An der Formstation ist an der Verteilerleiste ein zusätzlicher Werkzeug-Kühlwasser-Anschluss vorgesehen. Somit stehen je 4 Anschlüsse für die Kühlwasserzu- und Rückleitung zur Verfügung. Die Anschlüsse sind mit selbstabdichtenden Schnellverschluss-Kupplungen R1/2" ausgestattet.
        Beachte! In Verbindung mit Option 014915 ist das Merkmal 2x erforderlich! Stückzahl: 2 ST
        Guillin Polska Sp. z o.o. ul. Przemysłowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 10 / 15
        Pos.
        Vertriebsmerkmale
        40/440
        Sonderoption: 020182 Sonderablauf für die servomotorische Transportspreizung Normalerweise fährt die Spreizung zurück, wenn die Maschine gestoppt wird. Mit diesem Sonderablauf kann die Maschine gestoppt werden, ohne dass die Spreizung zurückfährt. Das bedeutet, die Maschine bleibt dann in Grundstellung stehen, und der Transport bleibt gespreizt. Im Automatikbetrieb kann dieser Sonderablauf am Touch-Bedienfeld aktiviert werden. Damit ist er eine Alternative zum Stop-Taster am Bedienfeld.
        Wenn die Maschine über diesen Sonderablauf gestoppt wurde, kann die Spreizeinrichtung immer noch über die Handfunktion zusammengefahren werden. Wenn die Maschine über diesen Sonderablauf gestoppt wurde, und ein Transportvorschub ausgelöst wird, dann fährt zuerst die Spreizung zurück. ACHTUNG Nicht Zurückstellen der Spreizposition kann durch die abkühlende Folie zu Schäden an der Maschine führen!
        Guillin Polska Sp. z o.o. ul. Przemyslowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 11 / 15
        Pos.
        Vertriebsmerkmale
        40/450
        Sonderoption: 015900 Tisch - Absteckeinrichtung für den Obertisch der Formstation Bei geöffneten Front-Verkleidung, kann der Obertisch der Formstation durch einen Absteckbolzen in seiner Halteposition zusätzlich mechanisch fixiert werden. Der Absteckbolzen befindet sich am Formaggregat in einer Docking Station (Aufbewahrungsvorrichtung), welche elektrisch abgefragt wird. Der Bediener kann durch Stecken des Bolzens in eine am Obertisch angebrachte Lochleiste den Obertisch zusätzlich mechanisch fixieren.
        Vor einem Neustart der Maschine muss der Absteckbolzen wieder aus der Lochleiste entfernt und in die Docking-Station gesteckt werden. Durch eine elektrische Abfrage des Absteckbolzen in der Docking-Station wird das Abziehen des Absteckbolzens aus der Lochleiste überwacht, d.h. ohne dass sich der Bolzen in der Docking-Station befindet, kann die Maschine nicht gestartet werden.
        HINWEIS:
        Nach den derzeit gültigen Sicherheitsvorschriften ist nur diese Absteckeinrichtung zur Tischabsicherung nicht zulässig, da die Sicherheitsverantwortung ausschließlich beim Bediener liegt. Deshalb müssen die vorgesehene/n Bremse/n im Antriebsstrang beim Einschalten der Maschine weiterhin automatisch überprüft werden.
        Guillin Polska Sp. z o.o. ul. Przemysłowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 12 / 15
        Pos.
        Vertriebsmerkmale
        40/460
        Sonderoption: 025983 Sondertischhub 150mm für Untertisch der Formstation RDK80k
        Durch diese Anpassung können höhere Ziehteile bis 130mm am Untertisch gefahren werden. Nur verwendbar für folgende Formprogramme: 1, 2, 13. (ohne die Funktion "Unterer Spannrahmen")
        Alle übrigen Form-Programme haben weiterhin einen max. Tischhub von 140 mm.
        Die Werkzeugwechselpositionen bleiben unverändert. Spannrahmenhub unten 140mm bleibt unverändert.
        Für das Fahren mit 150mm Hub sind die Grundplatten des Unter-Werkzeuges im Bereich der Spannrahmenstangen auszusparen (Kollisionsgefahr!).
        Werkzeughöhen gemäß Einbaublatt 220mm/370mm bleiben unverändert.
        Hinweise zur Formstation:
        -Aufgrund des vergrößerten Tischhubes muss auf der Bedienseite die Werkzeuggrundplatte im Bereich der Spannrahmenstange min. 15 mm tief ausgespart werden. Gegenüber Bedienseite muss die Spannrahmenstange vollständig freigestellt sein.
        -Die Referenzierung des Obertisches erfolgt unabhängig vom Form-Programm bei 150 mm Tischhub. Daher müssen Werkzeuge, die im Bereich der bedienseitigen Spannrahmenstange nicht ausgespart sind, ausgebaut werden.
        -Bei möglicher Durchhangsproblematik können produktabhängig Folien-Hochhalter am Ein- und Auslauf der Formstation optional ergänzt werden.
        Für die Umsetzung sind Sonderkniehebel notwendig. Buchsen und Bolzen können aus Serienlösung verwendet werden. Sonderhebel mit Bohrabstand 130mm statt 120mm, sowie 190mm statt 200mm werden verwendet. Es wird keine extra Doku/Einbaublatt für diese Funktion erstellt.
        Softwareseitige Anpassung ist inklusive. (Nur für die Formstation.)
        Hinweise zu Stapelung:
        Bei Verformung >120mm nach unten muß werkzeugtechnisch ohne die unterer Klemmbrille gefahren werden.
        Hinweise für weiteren Stationen:
        -Für Folgestationen sind ggf. weitere Sondermerkmale zusätzlich nötig. (z.b. Hochhalter für Stanze, Lochstanze.)
        Vorraussetzungen:
        -PL-Option "Servomotorischer Vorstrecker am Obertisch" erforderlich.
        -PL-Option "Spreizung vor Formstation" erforderlich.
        -Merkmal nicht zulässig in Verbindung mit Optionen "RDKL" oder "RDK80Sk".
        Guillin Polska Sp. z o.o. ul. Przemyslowa 3 56-400 OLESNICA
        Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 13 / 15
        Pos.
        Vertriebsmerkmale
        40/470
        Sonderoption: 011011
        Tisch - Absteckeinrichtung für den Obertisch der Bandstahlstanze Bei geöffneter Front-Verkleidung, kann der Obertisch der Bandstahlstanze durch einen Absteckbolzen in seiner Halteposition zusätzlich mechanisch fixiert werden. Der Absteckbolzen befindet sich am Stanzaggregat in einer Docking Station (Aufbewahrungsvorrichtung), welche elektrisch abgefragt wird.
        Vor einem Neustart der Maschine muss der Absteckbolzen wieder aus der Lochleiste entfernt und in die Docking-Station gesteckt werden.
        HINWEIS:
        Nach den derzeit gültigen Sicherheitsvorschriften ist nur diese Absteckeinrichtung zur Tischabsicherung nicht zulässig, da die Sicherheitsverantwortung ausschließlich beim Bediener liegt. Deshalb müssen die vorgesehene/n Bremse/n im Antriebsstrang beim Einschalten der Maschine weiterhin automatisch überprüft werden.
        40/480
        Sonderoption: 025984 Sonderausstattungen an Bandstahlstanze RDKP 72k /RDK 80k
        1) Sondertischhub
        Tischhub am Obertisch vergrößert auf 150 mm. Tischhub am Untertisch vergrößert auf 150 mm Verlängerte Sonderpleuel für Ober- und Untertisch werden verwendet.
        2) Zusätzliche Hinweise:
        Preislisten-Option 017311 #Bandstahlstanze# ist zusätzlich erforderlich Preislisten-Option 014936 #dynamische Prozess-Optimierung# ist zusätzlich erforderlich
        Guillin Polska Sp. z o.o. ul. Przemyslowa 3 56-400 OLESNICA
        Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        RDK 80k_Siemens_konf_ab_01.2013
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 14 / 15
        Pos.
        Vertriebsmerkmale
        40/490
        Sonderoption: 020679 Formteilstapel reihenweise ausschieben
        Am Handlingssystem wird eine zusätzliche Betriebsart vorgesehen, um die Formteilstapel reihenweise aus dem Stapelschacht herausschieben zu können. Für jeden Formteilstapel kann ein eigenes, ggf. konturnahes Ausschiebeblech vorgesehen werden, damit die Formteilstapel beim Ausschieben nicht zusammengedrückt werden. Einsatzgebiete dieser Zusatzfunktion sind:
        -nachfolgende Verkettungen (z.B. Beutelpacker), bei denen die Formteilstapel auf Abstand stehen müssen
        -kleine Formteile, die zum Umkippen neigen -schlecht zentrierende Formteile, die sich beim Ausschieben gegeneinander verschieben können
        Funktionsbeschreibung:
        -bei erreichter Stückzahl werden die Formteilstapel horizontal auf das Austransportband geschoben, jeder Formteilstapel wird separat abgestützt.
        -die vertikale Handlingsachse (Z-Achse) fährt anschließend nach oben, damit die Ausschiebebleche horizontal über die Formteilstapel hinweg zur Stapelstation zurückfahren können.
        -die Z-Achse fährt nach unten in den oberen Stapelschacht -die im unteren Stapelschacht gepufferten Formteile werden bei erreichter Stückzahl in den oberen Stapelschacht übergeben.
        -die einzelnen Verfahrwege und Positionen der Handlingsachsen können über das Bedienfeld geteacht werden.
        Sonstige Hinweise und Anforderungen:
        -2-geteilter Stapelschacht erforderlich, damit der Ausschiebevorgang sehr weich und ruckfrei ausgeführt werden kann.
        Weitere Hinweise und Einschränkungen des 2-geteilten Stapelschachtes sind zu berücksichtigen.
        -die zu erreichende, max. Stapelhöhe ist abhängig vom Werkzeugaufbau und der Ausschiebeebene. Bei Formteilhöhen < 60 mm können Stapelhöhen von 400 mm erzeugt werden.
        -das max. zulässige Gewicht des Ausschiebewerkzeuges beträgt 20 kg.
        40/500
        Sonderoption: 024872
        Kundenspezifische Sonderoptionen
        1)Die beiden Handhebelventile an der Unterbrücke der Formstation werden weiter nach außen versetzt, da der Kunde mehr Platz für seinen Werkzeug-Wechselwagen benötigt. 2)Zusätzlicher Kabel (Länge ca. 1,5 m) mit Steckersystem für Sensor am Stapelwerkzeug für pneumatisches A-B-Stapeln für die pneumatische Wechselstapelung
        3)Zusätzliche Klebeschilder in polnischer Sprache für den Schmierstoff-Behälter an der Formmaschine und für die Anpressung an der Stanzgittermühle
        Guillin Polska Sp. z o.o. ul. Przemysłowa 3 56-400 OLESNICA Kd.Nr. 708555
        Auftragsnr .:
        11055627 / 40
        Datum:
        21. Mai 2025
        Commissioning at customer
        Serialnr .:
        ( 732_0478 )
        Termin:
        19.11.2025
        Seite: 15 / 15
        Pos.
        Vertriebsmerkmale
        Bedienerführung-Sondersprache:
        40/520
        000645 Bedienerführung in Polnisch Dokumentation-Sondersprache:
        40/540
        000662 Dokumentation in Polnisch Anzahl: 1 ST
        Schaltplan-Varianten:
        40/550
        9046052 Schaltpläne in Englisch Versandvorbereitung:
        40/560
        009900 Versandvorbereitung für Spedition/LKW.
        100
        9195770 Installation at customer
        100/10
        015736 Die Montage wird durch ILLIG im Rahmen der vereinbarten Zeit durchgeführt. Details sind in den Geschäftsbedingungen auch unter ILLIG easy aufgeführt. Der Zeitrahmen der Montage beträgt:
        100/10-A
        8,0 Tage
        110
        9195771
        Commissioning at customer
        110/10
        015740 Die Inbetriebnahme wird durch Illig im Rahmen der vereinbarten Zeit durchgeführt. Details sind in den Geschäftsbedingungen auch unter ILLIG ready aufgeführt. Der Zeitrahmen der Inbetriebnahme beträgt: 2,0 Tage
        """;

    [Fact]
    public void Parse_VollstaendigesDokument_ExtrahiertKundennameUndAdresse()
    {
        var ergebnis = AuftragsinformationParser.Parse(VollstaendigesDokument);

        Assert.Equal("GUILLIN, Olesnica", ergebnis.Kundenname);
        Assert.Equal("ul. Przemysłowa 3 56-400 OLESNICA POLEN", ergebnis.Kundenadresse);
    }

    [Fact]
    public void Parse_VerarbeitetVollstaendigesDokument_AlleNormalenMerkmale()
    {
        var ergebnis = AuftragsinformationParser.Parse(VollstaendigesDokument);

        Assert.Equal("11055627 / 40", ergebnis.Auftragsnummer);
        Assert.Equal("708555", ergebnis.Kundennummer);
        Assert.Equal(new DateOnly(2025, 5, 21), ergebnis.Datum);
        Assert.Equal("RDK 80k_Siemens_konf_ab_01.2013", ergebnis.Maschinentyp);

        var erwarteteMerkmale = new (string Position, string Merkmalsnummer)[]
        {
            ("40", "9209425"),
            ("40/20", "025011"),
            ("40/30", "024933"),
            ("40/40", "014936"),
            ("40/50", "011460"),
            ("40/60", "014590"),
            ("40/70", "7002863"),
            ("40/80", "014919"),
            ("40/90", "011354"),
            ("40/100", "019852"),
            ("40/110", "017710"),
            ("40/120", "019853"),
            ("40/130", "020373"),
            ("40/140", "000741"),
            ("40/150", "014915"),
            ("40/160", "017308"),
            ("40/170", "014917"),
            ("40/180", "014940"),
            ("40/190", "017311"),
            ("40/200", "019863"),
            ("40/210", "000722"),
            ("40/220", "000723"),
            ("40/230", "014949"),
            ("40/240", "021746"),
            ("40/250", "019868"),
            ("40/260", "014955"),
            ("40/270", "7002874"),
            ("40/280", "017313"),
            ("40/290", "019871"),
            ("40/300", "014882"),
            ("40/310", "014903"),
            ("40/320", "020440"),
            ("40/330", "016075"),
            ("40/340", "017040"),
            ("40/350", "017041"),
            ("40/360", "024990"),
            ("40/370", "019889"),
            ("40/380", "019874"),
            ("40/390", "019875"),
            ("40/400", "9020016"),
            ("40/410", "020000"),
            // Ab hier folgt nach dem letzten markierten Sonderoptionen-Block (40/500) wieder
            // Normales: Sondersprachen, Schaltplan, Versand, Montage/Inbetriebnahme. Diese haben
            // KEINEN eigenen "Sonderoption:"-Marker und gehören daher zu den normalen Merkmalen.
            ("40/520", "000645"),
            ("40/540", "000662"),
            ("40/550", "9046052"),
            ("40/560", "009900"),
            ("100", "9195770"),
            ("100/10", "015736"),
            ("110", "9195771"),
            ("110/10", "015740"),
        };

        foreach (var (position, merkmalsnummer) in erwarteteMerkmale)
        {
            var merkmal = Assert.Single(ergebnis.Merkmale, m => m.Position == position);
            Assert.Equal(merkmalsnummer, merkmal.Merkmalsnummer);
        }

        // Positionen ohne numerische Merkmalsnummer werden übersprungen.
        Assert.DoesNotContain(ergebnis.Merkmale, m => m.Position == "40/10");
        Assert.DoesNotContain(ergebnis.Merkmale, m => m.Position == "100/10-A");
        Assert.Equal(erwarteteMerkmale.Length, ergebnis.Merkmale.Count);
    }

    [Fact]
    public void Parse_VerarbeitetVollstaendigesDokument_AlleSonderoptionen()
    {
        var ergebnis = AuftragsinformationParser.Parse(VollstaendigesDokument);

        // Jede Sonderoption hat einen eigenen "Sonderoption:"-Marker (one-shot, nicht klebrig):
        // 40/420 wird durch den freistehenden Marker nach 40/410 markiert, 40/430 durch den
        // Nachsatz-Marker von 40/420, 40/440–40/500 jeweils durch den Marker direkt vor ihrer
        // Nummer. Danach (40/520 ff.) folgen wieder normale Merkmale ohne Marker — diese gehören
        // NICHT hierher.
        var erwarteteSonderoptionen = new (string Position, string Merkmalsnummer)[]
        {
            ("40/420", "023771"),
            ("40/430", "018093"),
            ("40/440", "020182"),
            ("40/450", "015900"),
            ("40/460", "025983"),
            ("40/470", "011011"),
            ("40/480", "025984"),
            ("40/490", "020679"),
            ("40/500", "024872"),
        };

        foreach (var (position, merkmalsnummer) in erwarteteSonderoptionen)
        {
            var sonderoption = Assert.Single(ergebnis.Sonderoptionen, m => m.Position == position);
            Assert.Equal(merkmalsnummer, sonderoption.Merkmalsnummer);
        }

        Assert.Equal(erwarteteSonderoptionen.Length, ergebnis.Sonderoptionen.Count);

        // Kein Sonderoptionen-Merkmal darf den Marker selbst noch in der Beschreibung tragen
        // (der Wortbestandteil "Sonderoptionen" in echtem Beschreibungstext ist erlaubt).
        Assert.All(ergebnis.Sonderoptionen, m => Assert.DoesNotContain("Sonderoption:", m.Beschreibung));
    }
}
