using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Sondermerkmalsuche;

public class SondermerkmalMatcherTests
{
    private static StuecklisteKandidat Kandidat(
        string auftrag, string typ, int jahr, int monat, int tag, params string[] merkmale) =>
        new(auftrag, typ, new DateOnly(jahr, monat, tag), merkmale);

    [Fact]
    public void Rank_CountsOverlappingMerkmalsnummern()
    {
        var input = new[] { "M1", "M2", "M3" };
        var kandidaten = new[]
        {
            Kandidat("A100", "Typ-X", 2025, 1, 1, "M1", "M2", "M9"),
        };

        var result = SondermerkmalMatcher.Rank(input, kandidaten);

        var treffer = Assert.Single(result);
        Assert.Equal(2, treffer.TrefferAnzahl);
        Assert.Equal(3, treffer.Gesamtanzahl);
        Assert.Equal(new[] { "M1", "M2" }, treffer.UeberschneidendeMerkmalsnummern);
    }

    [Fact]
    public void Rank_ExcludesCandidatesWithoutOverlap()
    {
        var input = new[] { "M1", "M2" };
        var kandidaten = new[]
        {
            Kandidat("A1", "Typ-X", 2025, 1, 1, "M1"),
            Kandidat("A2", "Typ-Y", 2025, 1, 1, "M8", "M9"),
        };

        var result = SondermerkmalMatcher.Rank(input, kandidaten);

        Assert.Single(result);
        Assert.Equal("A1", result[0].Auftragsnummer);
    }

    [Fact]
    public void Rank_OrdersByTrefferAnzahlDescending()
    {
        var input = new[] { "M1", "M2", "M3" };
        var kandidaten = new[]
        {
            Kandidat("A1", "Typ-X", 2025, 1, 1, "M1"),
            Kandidat("A2", "Typ-Y", 2025, 1, 1, "M1", "M2", "M3"),
            Kandidat("A3", "Typ-Z", 2025, 1, 1, "M1", "M2"),
        };

        var result = SondermerkmalMatcher.Rank(input, kandidaten);

        Assert.Equal(new[] { "A2", "A3", "A1" }, result.Select(r => r.Auftragsnummer));
    }

    [Fact]
    public void Rank_TieBreaksByNewestAbschlussdatum()
    {
        var input = new[] { "M1", "M2" };
        var kandidaten = new[]
        {
            Kandidat("ALT", "Typ-X", 2023, 5, 1, "M1", "M2"),
            Kandidat("NEU", "Typ-Y", 2025, 5, 1, "M1", "M2"),
        };

        var result = SondermerkmalMatcher.Rank(input, kandidaten);

        Assert.Equal(new[] { "NEU", "ALT" }, result.Select(r => r.Auftragsnummer));
    }

    [Fact]
    public void Rank_LimitsToTopFive()
    {
        var input = new[] { "M1" };
        var kandidaten = Enumerable.Range(1, 8)
            .Select(i => Kandidat($"A{i}", "Typ-X", 2025, 1, i, "M1"))
            .ToArray();

        var result = SondermerkmalMatcher.Rank(input, kandidaten);

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void Rank_UebernimmtKundennameUndKundennummer()
    {
        var kandidaten = new[]
        {
            new StuecklisteKandidat("A100", "Typ-X", new DateOnly(2025, 1, 1), new[] { "M1" },
                Kundennummer: "708555", Kundenname: "GUILLIN, Olesnica"),
        };

        var treffer = Assert.Single(SondermerkmalMatcher.Rank(new[] { "M1" }, kandidaten));

        Assert.Equal("708555", treffer.Kundennummer);
        Assert.Equal("GUILLIN, Olesnica", treffer.Kundenname);
    }

    [Fact]
    public void Rank_ExcludesSourceOrder()
    {
        var input = new[] { "M1", "M2" };
        var kandidaten = new[]
        {
            Kandidat("QUELLE", "Typ-X", 2025, 1, 1, "M1", "M2"),
            Kandidat("ANDER", "Typ-Y", 2025, 1, 1, "M1"),
        };

        var result = SondermerkmalMatcher.Rank(input, kandidaten, ausschlussAuftragsnummer: "QUELLE");

        Assert.Single(result);
        Assert.Equal("ANDER", result[0].Auftragsnummer);
    }
}
