using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class AngebotHistorieLadezustandDesignTests
{
    [Fact]
    public void HistoryEntry_ShowsProgressWhileDetailLoads()
    {
        var sidebar = File.ReadAllText(ClientFile(
            "Components", "Auftragsanlage", "AngebotVerlaufSidebar.razor"));
        var sidebarCss = File.ReadAllText(ClientFile(
            "Components", "Auftragsanlage", "AngebotVerlaufSidebar.razor.css"));

        Assert.Contains("@{ var wirdGeladen = _ladenderId == eintrag.Id; }", sidebar);
        Assert.Contains("@onclick=\"() => EintragOeffnenAsync(eintrag)\"", sidebar);
        Assert.Contains("disabled=\"@_ladenderId.HasValue\"", sidebar);
        Assert.Contains("aria-busy=\"@wirdGeladen\"", sidebar);
        Assert.Contains("su-verlauf__eintrag--laedt", sidebar);
        Assert.Contains("su-verlauf__ladebalken", sidebar);
        Assert.Contains("role=\"progressbar\"", sidebar);
        Assert.Contains("private int? _ladenderId", sidebar);
        Assert.Contains("_ladenderId = eintrag.Id", sidebar);
        Assert.Contains("await OnEintragOeffnen.InvokeAsync(eintrag)", sidebar);
        Assert.Contains("finally", sidebar);
        Assert.Contains("_ladenderId = null", sidebar);
        Assert.Contains("@keyframes su-verlauf-laden", sidebarCss);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", sidebarCss);
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
