using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class AngebotHistorieOwnerEditDesignTests
{
    [Fact]
    public void Vertrieb_GatetHistorienBearbeitungAufOwnerUndNichtFreigegeben()
    {
        var page = Page();

        // Owner-Vergleich gegen das eigene Profil und nur solange nicht freigegeben.
        Assert.Contains("UserProfileState", page);
        Assert.Contains("detail.UserProfileId", page);
        Assert.Contains("UserProfileState.Profile?.Id", page);
        Assert.Contains("!detail.Freigegeben", page);
    }

    [Fact]
    public void Vertrieb_SpeistDenEditierbarenLiveZweigAusDemHistorienDetail()
    {
        var page = Page();

        // Statt des schreibgeschützten Detail-Zweigs wird für den Owner der editierbare
        // Live-Zweig rekonstruiert (Analyse + gespeichertes Angebot mit der Angebots-Id).
        Assert.Contains("new AngebotAnalyseAntwort(", page);
        Assert.Contains("new AngebotSpeichernAntwort(", page);
    }

    private static string Page() =>
        File.ReadAllText(ClientFile("Pages", "Auftragsanlage", "AuftragsanlageVertrieb.razor"));

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
