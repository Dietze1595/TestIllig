using System.Reflection;
using Illig_AI_Platform.Shared.Auftragsanlage;
using Xunit;

namespace Illig_AI_Platform.Tests.Controllers;

public class AngebotsvergleichPruefkatalogTests
{
    [Theory]
    [InlineData("beliebigen ERP-Systemen")]
    [InlineData("Angebotsbezug")]
    [InlineData("Leistungsumfang")]
    [InlineData("Preis und Währung")]
    [InlineData("Zahlungsbedingungen")]
    [InlineData("Lieferbedingungen")]
    [InlineData("Abnahme und Gewährleistung")]
    [InlineData("Vertragsrisiken")]
    [InlineData("unterschriebenen Angebotskopie")]
    [InlineData("\"istBestelldokument\"")]
    [InlineData("kein Bestelldokument")]
    public void Systemanweisung_EnthaeltKaufmaennischenPruefbereich(string pruefbereich)
    {
        var feld = typeof(AzureAngebotsvergleichLlmService).GetField(
            "SystemAnweisung",
            BindingFlags.NonPublic | BindingFlags.Static);
        var anweisung = Assert.IsType<string>(feld?.GetRawConstantValue());

        Assert.Contains(pruefbereich, anweisung, StringComparison.Ordinal);
    }

    [Fact]
    public void Systemanweisung_VergleichtPositionenSemantisch()
    {
        var feld = typeof(AzureAngebotsvergleichLlmService).GetField(
            "SystemAnweisung",
            BindingFlags.NonPublic | BindingFlags.Static);
        var anweisung = Assert.IsType<string>(feld?.GetRawConstantValue());

        Assert.DoesNotContain("Vergleiche KEINE Einzelpositionen", anweisung, StringComparison.Ordinal);
        Assert.Contains("Hauptpositionen", anweisung, StringComparison.Ordinal);
        Assert.Contains("Mengen semantisch", anweisung, StringComparison.Ordinal);
        Assert.Contains("unterschiedliche Positionsnummern", anweisung, StringComparison.Ordinal);
        Assert.Contains("andere Angebotspositionen nicht automatisch als", anweisung, StringComparison.Ordinal);
    }

    [Fact]
    public void Systemanweisung_VerwechseltLieferantenadresseNichtMitLieferanschrift()
    {
        var feld = typeof(AzureAngebotsvergleichLlmService).GetField(
            "SystemAnweisung",
            BindingFlags.NonPublic | BindingFlags.Static);
        var anweisung = Assert.IsType<string>(feld?.GetRawConstantValue());

        Assert.Contains("keine Lieferanschrift", anweisung, StringComparison.Ordinal);
        Assert.Contains("erfinde keinen Empfänger aus Lieferantendaten", anweisung, StringComparison.Ordinal);
    }

    [Fact]
    public void Systemanweisung_TrenntUebereinstimmungenVonAbweichungen()
    {
        var feld = typeof(AzureAngebotsvergleichLlmService).GetField(
            "SystemAnweisung",
            BindingFlags.NonPublic | BindingFlags.Static);
        var anweisung = Assert.IsType<string>(feld?.GetRawConstantValue());

        Assert.Contains("\"pruefbereiche\"", anweisung, StringComparison.Ordinal);
        Assert.Contains("\"bereich\"", anweisung, StringComparison.Ordinal);
        Assert.Contains("\"status\"", anweisung, StringComparison.Ordinal);
        Assert.Contains("\"pruefschluessel\"", anweisung, StringComparison.Ordinal);
        Assert.Contains("keine Abweichung erkennbar", anweisung, StringComparison.Ordinal);
        Assert.Contains("Status \"uebereinstimmung\"", anweisung, StringComparison.Ordinal);
        Assert.Contains("unterschiedlichen Status", anweisung, StringComparison.Ordinal);
        Assert.Contains("Bankdatenänderungen", anweisung, StringComparison.Ordinal);
    }

    [Fact]
    public void Bereinigung_EntferntUebereinstimmungWennDerselbeBereichAbweicht()
    {
        AngebotsvergleichPruefpunkt[] pruefpunkte =
        [
            new("kunde_lieferanschrift", "uebereinstimmung",
                "Bestellende Gesellschaft und Lieferanschrift passen zum Angebot.", "lieferanschrift"),
            new("kunde_lieferanschrift", "abweichung",
                "Im Angebot steht 901 Vision Street, in der Bestellung 901 Vision Drive.", "lieferanschrift"),
            new("angebotsbezug", "uebereinstimmung", "Angebotsnummer 50196359 stimmt überein.",
                "angebotsnummer"),
        ];

        var ergebnis = AngebotsvergleichPruefpunktBereinigung.Bereinigen(pruefpunkte);

        Assert.Single(ergebnis.Uebereinstimmungen);
        Assert.StartsWith("Angebotsbezug:", ergebnis.Uebereinstimmungen[0]);
        Assert.Single(ergebnis.Abweichungen);
        Assert.Contains("Vision Street", ergebnis.Abweichungen[0]);
        Assert.DoesNotContain(ergebnis.Uebereinstimmungen,
            text => text.StartsWith("Kunde/Lieferanschrift:", StringComparison.Ordinal));
    }

    [Fact]
    public void Bereinigung_BehaeltUnabhaengigeUebereinstimmungImSelbenBereich()
    {
        AngebotsvergleichPruefpunkt[] pruefpunkte =
        [
            new("zahlungsbedingungen", "uebereinstimmung",
                "Proforma-Zahlung ist in beiden Dokumenten genannt.", "proforma_zahlung"),
            new("zahlungsbedingungen", "abweichung",
                "Angebot nennt 10 Tage netto, Bestellung nur Credit.", "zahlungsziel"),
        ];

        var ergebnis = AngebotsvergleichPruefpunktBereinigung.Bereinigen(pruefpunkte);

        Assert.Single(ergebnis.Uebereinstimmungen);
        Assert.Contains("Proforma", ergebnis.Uebereinstimmungen[0]);
        Assert.Single(ergebnis.Abweichungen);
        Assert.Contains("10 Tage", ergebnis.Abweichungen[0]);
    }

    [Fact]
    public void Bereinigung_FasstMehrereBefundeJeBereichUndStatusZusammen()
    {
        AngebotsvergleichPruefpunkt[] pruefpunkte =
        [
            new("preis_waehrung", "uebereinstimmung",
                "Der Nettogesamtpreis beträgt in beiden Dokumenten 3.999,00 EUR.", "gesamtpreis"),
            new("preis_waehrung", "uebereinstimmung",
                "Preis/Währung: Die Währung ist in beiden Dokumenten EUR.", "waehrung"),
            new("lieferbedingungen", "uebereinstimmung",
                "Der Incoterm ist in beiden Dokumenten FCA.", "incoterm"),
            new("lieferbedingungen", "uebereinstimmung",
                "Die Versandart ist in beiden Dokumenten Luftfracht.", "versandart"),
        ];

        var ergebnis = AngebotsvergleichPruefpunktBereinigung.Bereinigen(pruefpunkte);

        Assert.Equal(2, ergebnis.Uebereinstimmungen.Count);
        Assert.Equal(
            "Preis/Währung: Der Nettogesamtpreis beträgt in beiden Dokumenten 3.999,00 EUR. " +
            "Die Währung ist in beiden Dokumenten EUR.",
            ergebnis.Uebereinstimmungen[0]);
        Assert.Equal(
            "Lieferbedingungen: Der Incoterm ist in beiden Dokumenten FCA. " +
            "Die Versandart ist in beiden Dokumenten Luftfracht.",
            ergebnis.Uebereinstimmungen[1]);
    }
}
