using Illig_AI_Platform.Shared.Auftragsanlage;
using Xunit;

namespace Illig_AI_Platform.Tests.Controllers;

// Direkte Unit-Tests der Dokumentart-Erkennung. Wichtig: Der Prüftext ist NICHT das ideale
// zweizeilige Testformat, sondern spiegelt die reale Ausgabe von Document Intelligence
// (prebuilt-layout, ErsteSeiteText = zeilenweise verbundene Page-1-Lines) wider – Beschriftung
// und Wert stehen auf getrennten Zeilen, der Doppelpunkt fehlt teils an der Beschriftung, und
// der Briefkopf ist entspannt (die frühere Fassung mit erzwungenem Doppelpunkt wies genau diese
// echten Angebote ab).
public class AngebotsdokumentErkennungTests
{
    private static ExtrahierteAngebotsdaten Daten(string ersteSeiteText, string? nummer, string volltext = "") =>
        new(nummer, null, null, null, [], Volltext: volltext, ErsteSeiteText: ersteSeiteText);

    [Fact]
    public void IstAngebot_ErkenntIdealesTestformat()
    {
        var daten = Daten("Angebot\nAngebotsnr.: 50209936", "50209936");
        Assert.True(AngebotsdokumentErkennung.IstAngebot(daten));
    }

    [Fact]
    public void IstAngebot_ErkenntAngebot_WennDoppelpunktFehltUndWertInNaechsterZeile()
    {
        // So liefert das Layout-Modell den Kopf tatsächlich: Beschriftung ohne Doppelpunkt,
        // Wert eine Zeile darunter. Genau dieser Fall wurde von der alten Prüfung abgelehnt.
        var daten = Daten(
            "Angebot\nILLIG packaging solutions GmbH\nAngebotsnr.\n50209936\nGültig bis\n31. März 2026",
            "50209936");
        Assert.True(AngebotsdokumentErkennung.IstAngebot(daten));
    }

    [Fact]
    public void IstAngebot_ErkenntEnglischesQuotation_MitWertVorBeschriftung()
    {
        var daten = Daten(
            "Quotation\nILLIG packaging solutions GmbH\n50214045\nquotation no.\n30. April 2026",
            "50214045");
        Assert.True(AngebotsdokumentErkennung.IstAngebot(daten));
    }

    [Fact]
    public void IstAngebot_ErkenntAngebot_WennBeschriftungZerlegtAberNummerExtrahiert()
    {
        // Fällt die Beschriftung komplett aus, genügt die bereits extrahierte Angebotsnummer
        // zusammen mit der Titelzeile.
        var daten = Daten("Quotation\nILLIG packaging solutions GmbH\nMEZŐ STAHL Kft.", "50215475");
        Assert.True(AngebotsdokumentErkennung.IstAngebot(daten));
    }

    [Fact]
    public void IstAngebot_FaelltAufVolltextZurueck_WennErsteSeiteLeer()
    {
        var daten = Daten("", "50209936", volltext: "Angebot\nAngebotsnr.: 50209936\nGültig bis: 31. März 2026");
        Assert.True(AngebotsdokumentErkennung.IstAngebot(daten));
    }

    [Fact]
    public void IstAngebot_LehntBestellungAb_TrotzAngebotsreferenzUndNummer()
    {
        // "Bezug: Angebot 50209936" ist keine eigenständige Titelzeile → keine Angebotserkennung,
        // auch wenn eine 50er-Nummer extrahiert wurde.
        var daten = Daten("Bestellung\nBestellnummer: 45001234\nBezug: Angebot 50209936", "50209936");
        Assert.False(AngebotsdokumentErkennung.IstAngebot(daten));
    }

    [Fact]
    public void IstAngebot_LehntRechnungAb()
    {
        var daten = Daten("Rechnung\nRechnungsnummer: 90001234", nummer: null);
        Assert.False(AngebotsdokumentErkennung.IstAngebot(daten));
    }

    [Fact]
    public void IstAngebot_LehntBloszeAngebotserwaehnungImFliesstextAb()
    {
        var daten = Daten("Wir beziehen uns auf Ihr Angebot 50209936.", "50209936");
        Assert.False(AngebotsdokumentErkennung.IstAngebot(daten));
    }

    [Fact]
    public void IstAngebot_LehntLeeresDokumentAb()
    {
        var daten = Daten("", nummer: null);
        Assert.False(AngebotsdokumentErkennung.IstAngebot(daten));
    }
}
