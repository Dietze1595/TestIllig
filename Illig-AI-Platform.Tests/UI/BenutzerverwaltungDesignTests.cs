using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class BenutzerverwaltungDesignTests
{
    [Fact]
    public void Benutzerverwaltung_SperrtNovazoonNutzerFuerExterneAdmins()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Admin", "Benutzerverwaltung.razor"));

        // Die geschützte Domain und die Schutzprüfung müssen im Client verankert sein (UX-Spiegel des
        // serverseitigen Guards in AdminUsersController).
        Assert.Contains("novazoon.de", page);
        Assert.Contains("IstGeschuetzt", page);
        // Bei geschützten Zeilen ist das Speichern gesperrt und ein Hinweis sichtbar.
        Assert.Contains("Von Novazoon geschützt", page);
    }

    private static string ClientFile(params string[] parts)
    {
        var all = new[] { TestSourceDirectory(), "..", "..", "Illig-AI-Platform.Client" }
            .Concat(parts).ToArray();
        return Path.GetFullPath(Path.Combine(all));
    }

    private static string TestSourceDirectory([CallerFilePath] string sourceFile = "") =>
        Path.GetDirectoryName(sourceFile)!;
}
