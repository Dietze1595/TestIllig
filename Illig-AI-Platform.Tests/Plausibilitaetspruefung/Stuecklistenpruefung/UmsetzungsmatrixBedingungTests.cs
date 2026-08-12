using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class UmsetzungsmatrixBedingungTests
{
    [Fact]
    public void Erfuellt_EinzelneMerkmalsnummer_WennVorhanden()
    {
        var bedingung = UmsetzungsmatrixBedingung.Parse("020440");
        Assert.True(bedingung.Erfuellt(["020440"]));
        Assert.False(bedingung.Erfuellt(["999999"]));
    }

    [Fact]
    public void Erfuellt_KompakteOderVerknuepfung_WennMindestensEinesVorhanden()
    {
        // wörtlich aus Zelle N8 der echten Umsetzungsmatrix
        var bedingung = UmsetzungsmatrixBedingung.Parse("020113 / 020114");

        Assert.True(bedingung.Erfuellt(["020113"]));
        Assert.True(bedingung.Erfuellt(["020114"]));
        Assert.False(bedingung.Erfuellt(["999999"]));
    }

    [Fact]
    public void Erfuellt_AusgeschriebeneOderVerknuepfung()
    {
        // wörtlich aus Zelle BD18 der echten Umsetzungsmatrix
        var bedingung = UmsetzungsmatrixBedingung.Parse(
            "017367 oder 011847 oder 017369 oder 017370 oder 010751");

        Assert.True(bedingung.Erfuellt(["017370"]));
        Assert.False(bedingung.Erfuellt(["999999"]));
    }

    [Fact]
    public void Erfuellt_GeklammerteUndVerknuepfung()
    {
        // wörtlich aus Zelle BD289 der echten Umsetzungsmatrix
        var bedingung = UmsetzungsmatrixBedingung.Parse("(015109 oder 015110) und 011357");

        Assert.True(bedingung.Erfuellt(["015109", "011357"]));
        Assert.True(bedingung.Erfuellt(["015110", "011357"]));
        // "und"-Teil fehlt -> nicht erfüllt, obwohl der "oder"-Teil passt
        Assert.False(bedingung.Erfuellt(["015109"]));
        // "oder"-Teil fehlt -> nicht erfüllt, obwohl der "und"-Teil passt
        Assert.False(bedingung.Erfuellt(["011357"]));
    }

    [Fact]
    public void Erfuellt_Negation()
    {
        var bedingung = UmsetzungsmatrixBedingung.Parse("N 017370");

        Assert.True(bedingung.Erfuellt(["999999"]));
        Assert.False(bedingung.Erfuellt(["017370"]));
    }

    [Fact]
    public void Erfuellt_NegationAusgeschrieben()
    {
        var bedingung = UmsetzungsmatrixBedingung.Parse("nicht 017370");

        Assert.True(bedingung.Erfuellt(["999999"]));
        Assert.False(bedingung.Erfuellt(["017370"]));
    }

    [Fact]
    public void Erfuellt_KompakteNegationOhneLeerzeichen()
    {
        // wörtlich aus Zelle AI222 der echten Umsetzungsmatrix ("N020121" ohne Leerzeichen)
        var bedingung = UmsetzungsmatrixBedingung.Parse("N020121");

        Assert.True(bedingung.Erfuellt(["999999"]));
        Assert.False(bedingung.Erfuellt(["020121"]));
    }

    [Fact]
    public void Erfuellt_KompakteNegationInUndKombination()
    {
        // So verknüpft der Parser zwei Bedingungsspalten: (Z222) U (AI222).
        var bedingung = UmsetzungsmatrixBedingung.Parse("(017366 / 017367 / 011847) U (N020121)");

        Assert.True(bedingung.Erfuellt(["017366"]));
        // Auftrag hat 020121 -> Ausschluss greift, obwohl der Formluft-Teil passt
        Assert.False(bedingung.Erfuellt(["017366", "020121"]));
    }

    [Fact]
    public void Erfuellt_KompakteUndVerknuepfung()
    {
        var bedingung = UmsetzungsmatrixBedingung.Parse("020113 U 011357");

        Assert.True(bedingung.Erfuellt(["020113", "011357"]));
        Assert.False(bedingung.Erfuellt(["020113"]));
    }

    [Fact]
    public void Parse_WirftBeiLeeremAusdruck()
    {
        Assert.Throws<ArgumentException>(() => UmsetzungsmatrixBedingung.Parse(""));
        Assert.Throws<ArgumentException>(() => UmsetzungsmatrixBedingung.Parse("   "));
    }
}
