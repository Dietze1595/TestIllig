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
