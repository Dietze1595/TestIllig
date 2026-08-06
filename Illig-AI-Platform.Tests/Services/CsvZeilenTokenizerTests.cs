using Illig_AI_Platform.Shared.Services;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class CsvZeilenTokenizerTests
{
    [Fact]
    public void Zeilen_SplittetUnquotierteFelderWieNaivesSplit()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("a;b;c\n1;2;3\n").ToList();

        Assert.Equal(2, zeilen.Count);
        Assert.Equal(["a", "b", "c"], zeilen[0]);
        Assert.Equal(["1", "2", "3"], zeilen[1]);
    }

    [Fact]
    public void Zeilen_ErlaubtTrennzeichenInnerhalbEinesQuotiertenFelds()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("\"Meier; Schulz GmbH\";Fellbach\n").ToList();

        Assert.Equal(["Meier; Schulz GmbH", "Fellbach"], zeilen[0]);
    }

    [Fact]
    public void Zeilen_ErlaubtZeilenumbruchInnerhalbEinesQuotiertenFelds()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("\"Zeile1\nZeile2\";Rest\nX;Y\n").ToList();

        Assert.Equal(2, zeilen.Count);
        Assert.Equal(["Zeile1\nZeile2", "Rest"], zeilen[0]);
        Assert.Equal(["X", "Y"], zeilen[1]);
    }

    [Fact]
    public void Zeilen_EntkommtEingebettetesAnfuehrungszeichenAlsDoppeltes()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("\"Sag \"\"Hallo\"\"\";Rest\n").ToList();

        Assert.Equal(["Sag \"Hallo\"", "Rest"], zeilen[0]);
    }

    [Fact]
    public void Zeilen_LiefertLetzteZeileOhneAbschliessendenZeilenumbruch()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("a;b").ToList();

        Assert.Single(zeilen);
        Assert.Equal(["a", "b"], zeilen[0]);
    }

    [Fact]
    public void Zeilen_IgnoriertLeerenInhalt()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("").ToList();

        Assert.Empty(zeilen);
    }

    [Fact]
    public void Zeilen_NormalisiertCrlfZeilenenden()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("a;b\r\n1;2\r\n").ToList();

        Assert.Equal(2, zeilen.Count);
        Assert.Equal(["a", "b"], zeilen[0]);
        Assert.Equal(["1", "2"], zeilen[1]);
    }

    [Fact]
    public void Zeilen_NormalisiertCrlfInnerhalbEinesQuotiertenFelds()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("\"Zeile1\r\nZeile2\";Rest\r\n").ToList();

        Assert.Equal(["Zeile1\nZeile2", "Rest"], zeilen[0]);
    }

    [Fact]
    public void Zeilen_BehaeltLeereFelderZwischenTrennzeichen()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("a;;c\n").ToList();

        Assert.Equal(["a", "", "c"], zeilen[0]);
    }

    [Fact]
    public void Zeilen_BehaeltAbschliessendesLeeresFeld()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("a;b;\n").ToList();

        Assert.Equal(["a", "b", ""], zeilen[0]);
    }

    [Fact]
    public void Zeilen_ErlaubtLeeresQuotiertesFeld()
    {
        var zeilen = CsvZeilenTokenizer.Zeilen("\"\";x\n").ToList();

        Assert.Equal(["", "x"], zeilen[0]);
    }
}
