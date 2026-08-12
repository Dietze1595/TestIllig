using Illig_AI_Platform.Shared.Auftragsanlage;
using Xunit;

namespace Illig_AI_Platform.Tests.Auftragsanlage;

public class AngebotsChecklisteTests
{
    [Theory]
    [InlineData("Musterstraße 1, 74074 Heilbronn", true)]
    [InlineData("Musterweg 1, 74074 Heilbronn", false)]
    [InlineData(null, false)]
    public void Berechnen_VergleichtKundenadresseMitLieferadresse(
        string? lieferadresse, bool erwartet)
    {
        var checkliste = AngebotsCheckliste.Berechnen(
            kundenname: "Muster GmbH",
            kundenadresse: "Musterstraße 1, 74074 Heilbronn",
            nummer: null,
            zahlungsbedingungen: null,
            incoterm: null,
            incotermOrt: null,
            versandbedingung: null,
            lieferadresse: lieferadresse);

        Assert.Equal(erwartet, checkliste.KundenUndLieferadresseIdentisch);
    }

    [Theory]
    [InlineData(
        "30% bei verbindlicher Beauftragung\n30% nach Konstruktion\n30% vor Auslieferung\n10% nach SAT",
        true)]
    [InlineData(
        "30 % bei verbindlicher Beauftragung\r\n30 % nach Konstruktion\r\n30 % vor Auslieferung\r\n10 % nach SAT",
        true)]
    [InlineData(
        "30% bei verbindlicher Beauftragung\n60% nach erfolgreichem FAT\n10% nach erfolgreichem SAT",
        false)]
    [InlineData(
        "120.000 EUR upon order confirmation\n120.800 EUR by the midpoint\n50% before delivery\n10% after commissioning",
        false)]
    [InlineData(
        "30% upon order confirmation\n30% by the midpoint\n40% before delivery",
        false)]
    [InlineData("50% upon order confirmation\n50% before delivery", true)]
    [InlineData(null, false)]
    public void Berechnen_ErkenntAusschliesslichStandardZahlungsplan(
        string? zahlungsplan,
        bool erwartet)
    {
        var checkliste = AngebotsCheckliste.Berechnen(
            kundenname: "Testkunde",
            kundenadresse: "Teststraße 1",
            nummer: "50200000",
            zahlungsbedingungen: "14d after receipt of invoice net",
            incoterm: "FCA",
            incotermOrt: null,
            versandbedingung: "Spedition",
            zahlungsbedingungCode: "A14",
            zahlungsplan: zahlungsplan,
            verkaeufer: "Test",
            liefertermin: "3 Monate",
            gueltigBis: DateTime.UtcNow.Date.AddDays(1));

        Assert.Equal(erwartet, checkliste.ZahlungsplanStandardErkannt);
    }

    [Theory]
    [InlineData("pick-up", true)]
    [InlineData("by seafreight", true)]
    [InlineData("air freight", true)]
    [InlineData("forwarder", true)]
    [InlineData("Economy (DPI)", true)]
    [InlineData("courier", false)]
    [InlineData(null, false)]
    public void Berechnen_ZeigtOriginalwertUndBewertetBekannteVersandkategorien(
        string? versandbedingung, bool erwartet)
    {
        var checkliste = AngebotsCheckliste.Berechnen(
            kundenname: null,
            kundenadresse: null,
            nummer: null,
            zahlungsbedingungen: null,
            incoterm: null,
            incotermOrt: null,
            versandbedingung: versandbedingung);

        Assert.Equal(erwartet, checkliste.VersandbedingungErkannt);
    }
}
