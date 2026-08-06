using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class AuftragsanlageChecklistenReihenfolgeTests
{
    [Theory]
    [InlineData("AuftragsanlageVertrieb.razor")]
    [InlineData("AuftragsanlageInnendienst.razor")]
    public void Checkliste_BeginntMitAngebotsnummerUndKunde(string dateiname)
    {
        var page = File.ReadAllText(ClientFile("Pages", "Auftragsanlage", dateiname));
        var checklisteStart = page.IndexOf(
            "<ul class=\"aa-checkliste",
            StringComparison.Ordinal);
        var angebotsnummer = page.IndexOf(
            "Label=\"Angebotsnummer\"",
            checklisteStart,
            StringComparison.Ordinal);
        var kunde = page.IndexOf(
            "Label=\"Kunde mit Adresse\"",
            checklisteStart,
            StringComparison.Ordinal);
        var verkaeufer = page.IndexOf(
            "Label=\"Verkäuferinformation\"",
            checklisteStart,
            StringComparison.Ordinal);

        Assert.True(checklisteStart >= 0);
        Assert.True(angebotsnummer > checklisteStart);
        Assert.True(kunde > angebotsnummer);
        Assert.True(verkaeufer > kunde);
    }

    private static string ClientFile(params string[] parts)
    {
        var all = new[] { TestSourceDirectory(), "..", "..", "Illig-AI-Platform.Client" }
            .Concat(parts)
            .ToArray();
        return Path.GetFullPath(Path.Combine(all));
    }

    private static string TestSourceDirectory([CallerFilePath] string sourceFile = "") =>
        Path.GetDirectoryName(sourceFile)!;
}
