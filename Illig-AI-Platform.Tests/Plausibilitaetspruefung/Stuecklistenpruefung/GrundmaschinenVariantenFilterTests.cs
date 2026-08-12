using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class GrundmaschinenVariantenFilterTests
{
    private static MaximalstuecklistenPosition Pos(string artikelnummer, string bezeichnung, string? bedingung = null) =>
        new()
        {
            Artikelnummer = artikelnummer,
            Bezeichnung = bezeichnung,
            Bedingung = bedingung
        };

    private static string[] Ausgeschlossen(IEnumerable<MaximalstuecklistenPosition> geschwister, string schluessel) =>
        GrundmaschinenVariantenFilter
            .AuszuschliessendeVarianten([.. geschwister], schluessel)
            .Select(p => p.Artikelnummer)
            .OrderBy(a => a)
            .ToArray();

    [Fact]
    public void SchliesstFremdeGrundmaschinenVarianteAus_WennBesserPassendesGeschwisterExistiert()
    {
        // Echter Fall aus der Rückmeldung: unter demselben Kabelsatz liegen zwei bedingungslose
        // Basis-Kabel; für eine 75Kc gilt das RDM75K-Kabel, nicht das RDM54-76K-Kabel.
        // Namen wörtlich aus der echten Maximalstückliste (nicht aus der Matrix — der Aufbau nutzt
        // die Baum-Bezeichnung): "RDM54Kc" vs. "RDM75K/Kc".
        var geschwister = new[]
        {
            Pos("9281770", "Kabel_Basis_RDM54Kc_Untertisch"),
            Pos("9308223", "Kabel_Basis_RDM75K/Kc_Untertisch")
        };

        Assert.Equal(["9281770"], Ausgeschlossen(geschwister, "RDM 75Kc"));
    }

    [Fact]
    public void LaesstUnterschiedlicheFunktionsgruppenInRuhe_TrotzGleicherGrundmaschineImNamen()
    {
        // Falle: beide bedingungslos, beide tragen "RDM54-76"/"RDM75" im Namen, sind aber KEINE
        // Varianten voneinander (Untertisch vs. innen, Kabelsatz vs. Kabelsatz_Basis). Der
        // "RDM54-76Kc_Untertisch"-Kabelsatz ist real verbaut und darf nicht gelöscht werden.
        var geschwister = new[]
        {
            Pos("9281756", "Kabelsatz_RDM54-76Kc_Untertisch"),
            Pos("9222832", "Kabelsatz_Basis_RDM75Kc/76K_innen")
        };

        Assert.Empty(Ausgeschlossen(geschwister, "RDM 75Kc"));
    }

    [Fact]
    public void SchliesstNichtsAus_WennKeinBesserPassendesGeschwisterExistiert()
    {
        // Nur die RDM54-76K-Variante vorhanden, kein konkurrierendes 75er-Geschwister -> behalten
        // (nicht raten). Namen mit Bereich schließen 75 sonst mit ein.
        var geschwister = new[]
        {
            Pos("9281770", "Kabel_Basis_RDM54-76K/Kc_Untertisch")
        };

        Assert.Empty(Ausgeschlossen(geschwister, "RDM 75Kc"));
    }

    [Fact]
    public void LaesstVariantenMitBedingungInRuhe()
    {
        // Nur bedingungslose Varianten sind betroffen. Eine merkmalsgesteuerte Variante bleibt der
        // bestehenden Bedingungslogik überlassen.
        var geschwister = new[]
        {
            Pos("9281770", "Kabel_Basis_RDM54-76K/Kc_Untertisch", bedingung: "010756"),
            Pos("9308223", "Kabel_Basis_RDM75K/Kc_Untertisch")
        };

        Assert.Empty(Ausgeschlossen(geschwister, "RDM 75Kc"));
    }

    [Fact]
    public void BehaeltVarianteDerenBereichDenMaschinentypEinschliesst()
    {
        // "RDM54-75Kc" nennt die 75 explizit -> passt und bleibt, auch neben einer reinen
        // RDM75K-Variante.
        var geschwister = new[]
        {
            Pos("9000001", "Teil_RDM54-75Kc_x"),
            Pos("9000002", "Teil_RDM75K_x")
        };

        Assert.Empty(Ausgeschlossen(geschwister, "RDM 75Kc"));
    }
}
