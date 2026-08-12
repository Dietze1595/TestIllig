using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class StuecklistenDiffTests
{
    private static StuecklistenKnoten Knoten(string artikel, string bezeichnung, decimal menge, string einheit, params StuecklistenKnoten[] kinder) =>
        new(artikel, bezeichnung, menge, einheit, kinder);

    private static RohPosition Roh(string artikel, string bezeichnung, decimal menge, string einheit, params RohPosition[] kinder) =>
        new(artikel, bezeichnung, menge, einheit, [.. kinder]);

    [Fact]
    public void Vergleiche_MarkiertUebereinstimmung_WennAllesGleich()
    {
        var unser = Knoten("A1", "Teil A", 2, "ST");
        var sap = Roh("A1", "Teil A", 2, "ST");

        var ergebnis = StuecklistenDiff.Vergleiche(unser, sap);

        Assert.Equal(VergleichsStatus.Uebereinstimmung, ergebnis.Wurzel.Status);
        Assert.Null(ergebnis.Wurzel.Hinweis);
        Assert.Empty(ergebnis.Wurzel.Kinder);
        Assert.Empty(ergebnis.NurInSap);
    }

    [Fact]
    public void Vergleiche_MarkiertAbweichung_BeiUnterschiedlicherMenge()
    {
        var unser = Knoten("A1", "Teil A", 2, "ST");
        var sap = Roh("A1", "Teil A", 3, "ST");

        var ergebnis = StuecklistenDiff.Vergleiche(unser, sap);

        Assert.Equal(VergleichsStatus.Abweichung, ergebnis.Wurzel.Status);
        Assert.Contains("Menge", ergebnis.Wurzel.Hinweis);
    }

    [Fact]
    public void Vergleiche_MarkiertAbweichung_BeiUnterschiedlicherEinheit()
    {
        var unser = Knoten("A1", "Teil A", 2, "ST");
        var sap = Roh("A1", "Teil A", 2, "KG");

        var ergebnis = StuecklistenDiff.Vergleiche(unser, sap);

        Assert.Equal(VergleichsStatus.Abweichung, ergebnis.Wurzel.Status);
        Assert.Contains("Einheit", ergebnis.Wurzel.Hinweis);
    }

    [Fact]
    public void Vergleiche_MarkiertAbweichung_BeiUnterschiedlicherBezeichnung()
    {
        var unser = Knoten("A1", "Teil A", 2, "ST");
        var sap = Roh("A1", "Anderer Text", 2, "ST");

        var ergebnis = StuecklistenDiff.Vergleiche(unser, sap);

        Assert.Equal(VergleichsStatus.Abweichung, ergebnis.Wurzel.Status);
        Assert.Contains("Bezeichnung", ergebnis.Wurzel.Hinweis);
    }

    [Fact]
    public void Vergleiche_MarkiertNurBeiUns_InklusiveKinder_WennSapKindFehlt()
    {
        var unser = Knoten("A1", "Teil A", 1, "ST",
            Knoten("A1.1", "Kind", 1, "ST",
                Knoten("A1.1.1", "Enkel", 1, "ST")));
        var sap = Roh("A1", "Teil A", 1, "ST");

        var ergebnis = StuecklistenDiff.Vergleiche(unser, sap);

        Assert.Equal(VergleichsStatus.Uebereinstimmung, ergebnis.Wurzel.Status);
        var kind = Assert.Single(ergebnis.Wurzel.Kinder);
        Assert.Equal("A1.1", kind.Artikelnummer);
        Assert.Equal(VergleichsStatus.NurBeiUns, kind.Status);
        var enkel = Assert.Single(kind.Kinder);
        Assert.Equal("A1.1.1", enkel.Artikelnummer);
        Assert.Equal(VergleichsStatus.NurBeiUns, enkel.Status);
    }

    [Fact]
    public void Vergleiche_SammeltNurInSap_InklusiveKinder_WennUnserKnotenFehlt()
    {
        var unser = Knoten("A1", "Teil A", 1, "ST");
        var sap = Roh("A1", "Teil A", 1, "ST",
            Roh("A1.1", "Nur SAP", 1, "ST",
                Roh("A1.1.1", "Nur SAP Enkel", 1, "ST")));

        var ergebnis = StuecklistenDiff.Vergleiche(unser, sap);

        Assert.Equal(2, ergebnis.NurInSap.Count);
        Assert.All(ergebnis.NurInSap, p => Assert.Equal(VergleichsStatus.NurInSap, p.Status));
        Assert.Contains(ergebnis.NurInSap, p => p.Artikelnummer == "A1.1");
        Assert.Contains(ergebnis.NurInSap, p => p.Artikelnummer == "A1.1.1");
    }

    [Fact]
    public void Vergleiche_PaartErsetztesTeilNachBezeichnung_UndVergleichtUnterbaumWeiter()
    {
        // Gleicher Slot "Gehäuse", andere Artikelnummer (Maximalstückliste-Soll 9223375 vs SAP-Ist
        // 9317922). Das darunterliegende Schweissteil ist in beiden identisch und muss als
        // Übereinstimmung erkannt werden — nicht wegen des Eltern-Mismatchs pauschal als "fehlt".
        var unser = Knoten("M1", "Maschine", 1, "ST",
            Knoten("9223375", "Gehäuse_RDM75K/76K", 1, "ST",
                Knoten("9223288", "Gehäuse_Schweissteil_RDM75K_76K", 1, "ST")));
        var sap = Roh("M1", "Maschine", 1, "ST",
            Roh("9317922", "Gehäuse_RDM75Kc_RDML75b", 1, "ST",
                Roh("9223288", "Gehäuse_Schweissteil_RDM75K_76K", 1, "ST")));

        var ergebnis = StuecklistenDiff.Vergleiche(unser, sap);

        var gehaeuse = Assert.Single(ergebnis.Wurzel.Kinder);
        Assert.Equal("9223375", gehaeuse.Artikelnummer);
        Assert.Equal(VergleichsStatus.Abweichung, gehaeuse.Status);
        Assert.Contains("Anderes Teil", gehaeuse.Hinweis);
        Assert.Contains("9317922", gehaeuse.Hinweis);

        var schweissteil = Assert.Single(gehaeuse.Kinder);
        Assert.Equal("9223288", schweissteil.Artikelnummer);
        Assert.Equal(VergleichsStatus.Uebereinstimmung, schweissteil.Status);

        // Kein doppeltes "Nur in SAP" mehr für den getauschten Gehäuse-Zweig.
        Assert.Empty(ergebnis.NurInSap);
    }

    [Fact]
    public void Vergleiche_PaartNichtVerwandteTeileNicht_BleibtFehltUndNurInSap()
    {
        // Unterschiedliche Bezeichnungen (kein gemeinsames führendes Wort) → NICHT paaren,
        // sondern wie bisher getrennt als "fehlt" bzw. "nur in SAP" melden.
        var unser = Knoten("M1", "Maschine", 1, "ST",
            Knoten("X1", "Vakuumpumpe", 1, "ST"));
        var sap = Roh("M1", "Maschine", 1, "ST",
            Roh("Y1", "Steuerung", 1, "ST"));

        var ergebnis = StuecklistenDiff.Vergleiche(unser, sap);

        var kind = Assert.Single(ergebnis.Wurzel.Kinder);
        Assert.Equal("X1", kind.Artikelnummer);
        Assert.Equal(VergleichsStatus.NurBeiUns, kind.Status);

        var nurSap = Assert.Single(ergebnis.NurInSap);
        Assert.Equal("Y1", nurSap.Artikelnummer);
    }

    [Fact]
    public void Vergleiche_SummiertMenge_BeiDoppelterGeschwisterArtikelnummerInSap()
    {
        var unser = Knoten("A1", "Teil A", 1, "ST",
            Knoten("K1", "Kleinteil", 5, "ST"));
        var sap = Roh("A1", "Teil A", 1, "ST",
            Roh("K1", "Kleinteil", 2, "ST"),
            Roh("K1", "Kleinteil", 3, "ST"));

        var ergebnis = StuecklistenDiff.Vergleiche(unser, sap);

        var kindPosition = Assert.Single(ergebnis.Wurzel.Kinder);
        Assert.Equal("K1", kindPosition.Artikelnummer);
        Assert.Equal(VergleichsStatus.Uebereinstimmung, kindPosition.Status);
        Assert.Equal(5, kindPosition.SapMenge);
    }
}
