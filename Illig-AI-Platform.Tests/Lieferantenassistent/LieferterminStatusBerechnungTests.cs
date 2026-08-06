using Illig_AI_Platform.Shared.Lieferantenassistent;
using Xunit;

namespace Illig_AI_Platform.Tests.Lieferantenassistent;

public class LieferterminStatusBerechnungTests
{
    private static readonly DateOnly Heute = new(2026, 7, 17);

    [Fact]
    public void Berechnen_GibtUeberfaellig_WennLieferdatumInDerVergangenheit()
    {
        var status = LieferterminStatusBerechnung.Berechnen(Heute.AddDays(-1), Heute);

        Assert.Equal(LieferterminStatus.Ueberfaellig, status);
    }

    [Fact]
    public void Berechnen_GibtBaldFaellig_WennLieferdatumHeuteIst()
    {
        var status = LieferterminStatusBerechnung.Berechnen(Heute, Heute);

        Assert.Equal(LieferterminStatus.BaldFaellig, status);
    }

    [Fact]
    public void Berechnen_GibtBaldFaellig_WennLieferdatumGenau7TageEntferntIst()
    {
        var status = LieferterminStatusBerechnung.Berechnen(Heute.AddDays(7), Heute);

        Assert.Equal(LieferterminStatus.BaldFaellig, status);
    }

    [Fact]
    public void Berechnen_GibtImPlan_WennLieferdatumMehrAls7TageEntferntIst()
    {
        var status = LieferterminStatusBerechnung.Berechnen(Heute.AddDays(8), Heute);

        Assert.Equal(LieferterminStatus.ImPlan, status);
    }
}
