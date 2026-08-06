using Illig_AI_Platform.Shared.Lieferantenassistent;
using Xunit;

namespace Illig_AI_Platform.Tests.Lieferantenassistent;

public class DispositionslisteCsvParserTests
{
    private const string Csv =
        "Lieferant/Lieferwerk;Anzahl der Positionen;Einkäufergruppe;Einkaufsbeleg;Position;Belegdatum;Lieferdatum;Auftragsbestätigung;Material;Kurztext;Werk;Bestellmenge;Bestellmengeneinheit;Nettopreis;Währung;Preiseinheit;Einteilungsmenge;Gelieferte Menge;noch zu liefern (Menge);Menge in Lager-ME\n" +
        ";1.886;;;;;;;;;;;;;;;;;;\n" +
        "1051       R+W Antriebselemente GmbH;1;;;;;;;;;;;;;;;;;;\n" +
        "1051       R+W Antriebselemente GmbH;1;021;45647126;10;10.07.2026;17.08.2026;;9152207;Kupplung_BKL_30_14_24;0001;2;ST;71,64;EUR;1;2;0;2;2\n" +
        "1088       Harmonic Drive AG;1;;;;;;;;;;;;;;;;;;\n" +
        "1088       Harmonic Drive AG;1;021;45644415;10;02.04.2026;31.07.2026;;9238159;Servomotor_CHA-32A-100-H-M128P-B-SP;0001;2;ST;5.972,00;EUR;1;2;0;2;2\n";

    [Fact]
    public void Parse_UeberspringtKopfUndZwischensummenzeilen_LiefertNurDetailzeilen()
    {
        var ergebnis = DispositionslisteCsvParser.Parse(Csv);

        Assert.Equal(2, ergebnis.Count);
    }

    [Fact]
    public void Parse_ExtrahiertKreditorAusKombinierterLieferantenspalte()
    {
        var ergebnis = DispositionslisteCsvParser.Parse(Csv);

        Assert.Equal(1051, ergebnis[0].LieferantKreditor);
        Assert.Equal(1088, ergebnis[1].LieferantKreditor);
    }

    [Fact]
    public void Parse_ParstDeutschesZahlenformatUndDatum()
    {
        var ergebnis = DispositionslisteCsvParser.Parse(Csv);

        var erste = ergebnis[0];
        Assert.Equal("45647126", erste.Einkaufsbeleg);
        Assert.Equal("10", erste.Position);
        Assert.Equal(new DateOnly(2026, 7, 10), erste.Belegdatum);
        Assert.Equal(new DateOnly(2026, 8, 17), erste.Lieferdatum);
        Assert.Equal("9152207", erste.Material);
        Assert.Equal("Kupplung_BKL_30_14_24", erste.Kurztext);
        Assert.Equal(2m, erste.Bestellmenge);
        Assert.Equal(71.64m, erste.Nettopreis);
        Assert.Equal(2m, erste.NochZuLiefernMenge);

        var zweite = ergebnis[1];
        Assert.Equal(5972.00m, zweite.Nettopreis);
    }

    [Fact]
    public void Parse_UeberspringtZeileMitLeererLieferantenspalteUndGefuelltemEinkaufsbeleg()
    {
        var csv =
            "Lieferant/Lieferwerk;Anzahl der Positionen;Einkäufergruppe;Einkaufsbeleg;Position;Belegdatum;Lieferdatum;Auftragsbestätigung;Material;Kurztext;Werk;Bestellmenge;Bestellmengeneinheit;Nettopreis;Währung;Preiseinheit;Einteilungsmenge;Gelieferte Menge;noch zu liefern (Menge);Menge in Lager-ME\n" +
            "   ;1;021;45647126;10;10.07.2026;17.08.2026;;9152207;Kupplung_BKL_30_14_24;0001;2;ST;71,64;EUR;1;2;0;2;2\n";

        var ergebnis = DispositionslisteCsvParser.Parse(csv);

        Assert.Empty(ergebnis);
    }

    [Fact]
    public void Parse_WirftFormatException_WennLieferdatumFehlt()
    {
        var csv =
            "Lieferant/Lieferwerk;Anzahl der Positionen;Einkäufergruppe;Einkaufsbeleg;Position;Belegdatum;Lieferdatum;Auftragsbestätigung;Material;Kurztext;Werk;Bestellmenge;Bestellmengeneinheit;Nettopreis;Währung;Preiseinheit;Einteilungsmenge;Gelieferte Menge;noch zu liefern (Menge);Menge in Lager-ME\n" +
            "1051       R+W Antriebselemente GmbH;1;021;45647126;10;10.07.2026;;;9152207;Kupplung_BKL_30_14_24;0001;2;ST;71,64;EUR;1;2;0;2;2\n";

        Assert.Throws<FormatException>(() => DispositionslisteCsvParser.Parse(csv));
    }

    [Fact]
    public void Parse_BautUnterschiedlicheSchluesselFuerEinteilungenDerselbenPosition()
    {
        // Reale SAP-Daten: eine Bestellmenge wird auf zwei Lieferplan-Einteilungen (Teillieferungen)
        // mit unterschiedlichem Lieferdatum aufgeteilt — beide Zeilen teilen sich Einkaufsbeleg
        // und Position, dürfen sich beim additiven Import aber nicht gegenseitig überschreiben.
        var csv =
            "Lieferant/Lieferwerk;Anzahl der Positionen;Einkäufergruppe;Einkaufsbeleg;Position;Belegdatum;Lieferdatum;Auftragsbestätigung;Material;Kurztext;Werk;Bestellmenge;Bestellmengeneinheit;Nettopreis;Währung;Preiseinheit;Einteilungsmenge;Gelieferte Menge;noch zu liefern (Menge);Menge in Lager-ME\n" +
            "2906       Nägele Mechanik GmbH;1;950;45645541;20;06.05.2026;14.07.2026;;9228805;Verteiler_aussen_FT;0001;24;ST;320,95;EUR;1;12;0;12;24\n" +
            "2906       Nägele Mechanik GmbH;1;950;45645541;20;08.07.2026;20.07.2026;;9228805;Verteiler_aussen_FT;0001;24;ST;320,95;EUR;1;12;0;12;24\n";

        var ergebnis = DispositionslisteCsvParser.Parse(csv);

        Assert.Equal(2, ergebnis.Count);
        Assert.Equal(ergebnis[0].Einkaufsbeleg, ergebnis[1].Einkaufsbeleg);
        Assert.Equal(ergebnis[0].Position, ergebnis[1].Position);
        Assert.NotEqual(ergebnis[0].Schluessel, ergebnis[1].Schluessel);
        Assert.Equal("45645541/20/2026-07-14", ergebnis[0].Schluessel);
        Assert.Equal("45645541/20/2026-07-20", ergebnis[1].Schluessel);
    }
}
