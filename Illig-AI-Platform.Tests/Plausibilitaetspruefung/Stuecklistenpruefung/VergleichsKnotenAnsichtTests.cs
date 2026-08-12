using Illig_AI_Platform.Client.Models.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class VergleichsKnotenAnsichtTests
{
    [Fact]
    public void EnthaeltAbweichung_ErkenntAbweichungInNachfahren()
    {
        var kind = Knoten(VergleichsStatus.Abweichung);
        var wurzel = Knoten(VergleichsStatus.Uebereinstimmung, kind);

        var ansicht = VergleichsKnotenAnsicht.Aufbauen(wurzel);

        Assert.True(ansicht.EnthaeltAbweichung);
    }

    [Fact]
    public void EnthaeltAbweichung_IstFalschBeiVollstaendigerUebereinstimmung()
    {
        var wurzel = Knoten(
            VergleichsStatus.Uebereinstimmung,
            Knoten(VergleichsStatus.Uebereinstimmung));

        var ansicht = VergleichsKnotenAnsicht.Aufbauen(wurzel);

        Assert.False(ansicht.EnthaeltAbweichung);
    }

    [Theory]
    [InlineData(VergleichsStatus.Abweichung)]
    [InlineData(VergleichsStatus.NurBeiUns)]
    public void EnthaeltAbweichung_BeruecksichtigtBeideProblemstatus(
        VergleichsStatus status)
    {
        var ansicht = VergleichsKnotenAnsicht.Aufbauen(Knoten(status));

        Assert.True(ansicht.EnthaeltAbweichung);
    }

    [Fact]
    public void Aufbauen_FehlenderKnoten_BleibtEingeklappt()
    {
        // Ein fehlender Knoten (NurBeiUns) trägt seinen Status auf dem ganzen Teilbaum — es
        // genügt, den obersten fehlenden Knoten zu zeigen, daher eingeklappt.
        var wurzel = Knoten(
            VergleichsStatus.NurBeiUns,
            Knoten(VergleichsStatus.NurBeiUns));

        var ansicht = VergleichsKnotenAnsicht.Aufbauen(wurzel);

        Assert.False(ansicht.Aufgeklappt);
    }

    [Fact]
    public void Aufbauen_ElternEinesFehlendenKnotens_KlapptAufUmIhnZuZeigen()
    {
        var wurzel = Knoten(
            VergleichsStatus.Uebereinstimmung,
            Knoten(VergleichsStatus.NurBeiUns, Knoten(VergleichsStatus.NurBeiUns)));

        var ansicht = VergleichsKnotenAnsicht.Aufbauen(wurzel);

        Assert.True(ansicht.Aufgeklappt);            // Eltern zeigen den fehlenden Knoten
        Assert.False(ansicht.Kinder[0].Aufgeklappt); // fehlender Knoten selbst eingeklappt
    }

    [Fact]
    public void Aufbauen_TieferliegendeAbweichung_WirdAufgeklappt()
    {
        var wurzel = Knoten(
            VergleichsStatus.Uebereinstimmung,
            Knoten(VergleichsStatus.Uebereinstimmung, Knoten(VergleichsStatus.Abweichung)));

        var ansicht = VergleichsKnotenAnsicht.Aufbauen(wurzel);

        Assert.True(ansicht.Aufgeklappt);
        Assert.True(ansicht.Kinder[0].Aufgeklappt);
    }

    [Fact]
    public void Aufbauen_VollstaendigeUebereinstimmung_BleibtEingeklappt()
    {
        var wurzel = Knoten(
            VergleichsStatus.Uebereinstimmung,
            Knoten(VergleichsStatus.Uebereinstimmung));

        var ansicht = VergleichsKnotenAnsicht.Aufbauen(wurzel);

        Assert.False(ansicht.Aufgeklappt);
    }

    [Fact]
    public void EnthaeltStatus_TrueBeiEigenemStatus()
    {
        var ansicht = VergleichsKnotenAnsicht.Aufbauen(Knoten(VergleichsStatus.Abweichung));

        Assert.True(ansicht.EnthaeltStatus(new HashSet<VergleichsStatus> { VergleichsStatus.Abweichung }));
    }

    [Fact]
    public void EnthaeltStatus_TrueBeiNachfahre()
    {
        var wurzel = Knoten(
            VergleichsStatus.Uebereinstimmung,
            Knoten(VergleichsStatus.NurBeiUns));
        var ansicht = VergleichsKnotenAnsicht.Aufbauen(wurzel);

        Assert.True(ansicht.EnthaeltStatus(new HashSet<VergleichsStatus> { VergleichsStatus.NurBeiUns }));
    }

    [Fact]
    public void EnthaeltStatus_FalseWennStatusNichtImTeilbaum()
    {
        var wurzel = Knoten(
            VergleichsStatus.Uebereinstimmung,
            Knoten(VergleichsStatus.Uebereinstimmung));
        var ansicht = VergleichsKnotenAnsicht.Aufbauen(wurzel);

        Assert.False(ansicht.EnthaeltStatus(new HashSet<VergleichsStatus> { VergleichsStatus.Abweichung }));
    }

    [Fact]
    public void EnthaeltStatus_FalseBeiLeererMenge()
    {
        var ansicht = VergleichsKnotenAnsicht.Aufbauen(Knoten(VergleichsStatus.Abweichung));

        Assert.False(ansicht.EnthaeltStatus(new HashSet<VergleichsStatus>()));
    }

    [Fact]
    public void EnthaeltStatus_TrueBeiEinemVonMehrerenStatus()
    {
        var wurzel = Knoten(
            VergleichsStatus.Uebereinstimmung,
            Knoten(VergleichsStatus.Abweichung));
        var ansicht = VergleichsKnotenAnsicht.Aufbauen(wurzel);

        Assert.True(ansicht.EnthaeltStatus(
            new HashSet<VergleichsStatus> { VergleichsStatus.Abweichung, VergleichsStatus.NurBeiUns }));
    }

    private static VergleichsKnoten Knoten(
        VergleichsStatus status,
        params VergleichsKnoten[] kinder) =>
        new(
            "100",
            "Position",
            "Position",
            1,
            "ST",
            1,
            "ST",
            status,
            null,
            kinder);

    private static VergleichsKnotenAnsicht Ansicht(VergleichsKnoten knoten, bool aufgeklappt, params VergleichsKnotenAnsicht[] kinder) =>
        new() { Knoten = knoten, Aufgeklappt = aufgeklappt, Kinder = kinder };

    [Fact]
    public void SichtbareZeilen_ListetWurzelUndAufgeklappteKinderAuf()
    {
        var kind = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), false);
        var wurzel = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), true, kind);

        var zeilen = VergleichsKnotenAnsicht.SichtbareZeilen(wurzel, 0, null, false).ToList();

        Assert.Equal(2, zeilen.Count);
        Assert.Same(wurzel, zeilen[0].Knoten);
        Assert.Equal(0, zeilen[0].Tiefe);
        Assert.Same(kind, zeilen[1].Knoten);
        Assert.Equal(1, zeilen[1].Tiefe);
    }

    [Fact]
    public void SichtbareZeilen_UeberspringtEingeklappteKinder()
    {
        var kind = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), false);
        var wurzel = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), false, kind);

        var zeilen = VergleichsKnotenAnsicht.SichtbareZeilen(wurzel, 0, null, false).ToList();

        Assert.Single(zeilen);
        Assert.Same(wurzel, zeilen[0].Knoten);
    }

    [Fact]
    public void SichtbareZeilen_FilterBlendetGanzenTeilbaumAus()
    {
        var kind = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), false);
        var wurzel = Ansicht(Knoten(VergleichsStatus.Abweichung), true, kind);
        bool Filter(VergleichsKnotenAnsicht k) => k.Knoten.Status != VergleichsStatus.Uebereinstimmung;

        var zeilen = VergleichsKnotenAnsicht.SichtbareZeilen(wurzel, 0, Filter, false).ToList();

        Assert.Single(zeilen);
        Assert.Same(wurzel, zeilen[0].Knoten);
    }

    [Fact]
    public void SichtbareZeilen_AlleSichtbarenKinderAnzeigenErzwingtAufklappen()
    {
        var kind = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), false);
        var wurzel = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), false, kind);

        var zeilen = VergleichsKnotenAnsicht.SichtbareZeilen(wurzel, 0, null, true).ToList();

        Assert.Equal(2, zeilen.Count);
        Assert.True(zeilen[0].IstAufgeklappt);
    }

    [Fact]
    public void SichtbareZeilen_SetztHatSichtbareKinderKorrekt()
    {
        var kindMitKind = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), false, Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), false));
        var blatt = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), false);
        var wurzel = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), true, kindMitKind, blatt);

        var zeilen = VergleichsKnotenAnsicht.SichtbareZeilen(wurzel, 0, null, false).ToList();

        Assert.True(zeilen.Single(z => z.Knoten == kindMitKind).HatSichtbareKinder);
        Assert.False(zeilen.Single(z => z.Knoten == blatt).HatSichtbareKinder);
    }

    [Fact]
    public void SichtbareZeilen_LiefertLeereListe_WennWurzelSelbstAmFilterScheitert()
    {
        var wurzel = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), true);
        bool Filter(VergleichsKnotenAnsicht k) => k.Knoten.Status != VergleichsStatus.Uebereinstimmung;

        var zeilen = VergleichsKnotenAnsicht.SichtbareZeilen(wurzel, 0, Filter, false).ToList();

        Assert.Empty(zeilen);
    }

    [Fact]
    public void SichtbareZeilen_HatSichtbareKinderIstFalsch_WennFilterAlleKinderAusblendet()
    {
        var kind = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), false);
        var wurzel = Ansicht(Knoten(VergleichsStatus.Abweichung), true, kind);
        bool Filter(VergleichsKnotenAnsicht k) => k.Knoten.Status != VergleichsStatus.Uebereinstimmung;

        var zeilen = VergleichsKnotenAnsicht.SichtbareZeilen(wurzel, 0, Filter, false).ToList();

        Assert.Single(zeilen);
        Assert.False(zeilen[0].HatSichtbareKinder);
    }

    [Fact]
    public void SichtbareZeilen_SchliesstGefilterteEnkelMitEin_WennderenElternDenFilterBesteht()
    {
        var enkelDerDenFilterBesteht = Ansicht(Knoten(VergleichsStatus.Abweichung), false);
        var kindOhneEigeneAbweichungAberMitPassendemEnkel = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), true, enkelDerDenFilterBesteht);
        var kindDasKomplettAusgeblendetWird = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), true,
            Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), true));
        var wurzel = Ansicht(Knoten(VergleichsStatus.Uebereinstimmung), true,
            kindOhneEigeneAbweichungAberMitPassendemEnkel, kindDasKomplettAusgeblendetWird);
        bool Filter(VergleichsKnotenAnsicht k) =>
            k.Knoten.Status != VergleichsStatus.Uebereinstimmung || k.Kinder.Any(kind => Filter(kind));

        var zeilen = VergleichsKnotenAnsicht.SichtbareZeilen(wurzel, 0, Filter, false).ToList();

        Assert.Contains(zeilen, z => z.Knoten == enkelDerDenFilterBesteht);
        Assert.DoesNotContain(zeilen, z => z.Knoten == kindDasKomplettAusgeblendetWird);
    }

    private static VergleichsKnoten KnotenMitText(
        string artNum,
        string bezeichnung,
        VergleichsStatus status = VergleichsStatus.Uebereinstimmung,
        params VergleichsKnoten[] kinder) =>
        new(
            artNum,
            bezeichnung,
            bezeichnung,
            1,
            "ST",
            1,
            "ST",
            status,
            null,
            kinder);

    [Fact]
    public void SichtbareZeilen_SearchPropagatesToDescendants()
    {
        // Root -> Baugruppe A -> Kind 1 (Uebereinstimmung)
        //                     -> Kind 2 (Abweichung)
        //      -> Baugruppe B
        var kind1 = KnotenMitText("101", "Kind 1");
        var kind2 = KnotenMitText("102", "Kind 2", VergleichsStatus.Abweichung);
        var baugruppeA = KnotenMitText("200", "Baugruppe A", VergleichsStatus.Uebereinstimmung, kind1, kind2);
        var baugruppeB = KnotenMitText("300", "Baugruppe B");
        var wurzel = KnotenMitText("000", "Root", VergleichsStatus.Uebereinstimmung, baugruppeA, baugruppeB);

        var ansicht = VergleichsKnotenAnsicht.Aufbauen(wurzel);

        // Search for "Baugruppe A"
        var zeilen = VergleichsKnotenAnsicht.SichtbareZeilen(ansicht, 0, null, true, "Baugruppe A").ToList();

        // Should include Root (ancestor), Baugruppe A (match), and its children Kind 1 and Kind 2 (descendants of match).
        // It should NOT include Baugruppe B.
        Assert.Contains(zeilen, z => z.Knoten.Knoten.Bezeichnung == "Root");
        Assert.Contains(zeilen, z => z.Knoten.Knoten.Bezeichnung == "Baugruppe A");
        Assert.Contains(zeilen, z => z.Knoten.Knoten.Bezeichnung == "Kind 1");
        Assert.Contains(zeilen, z => z.Knoten.Knoten.Bezeichnung == "Kind 2");
        Assert.DoesNotContain(zeilen, z => z.Knoten.Knoten.Bezeichnung == "Baugruppe B");
    }
}
