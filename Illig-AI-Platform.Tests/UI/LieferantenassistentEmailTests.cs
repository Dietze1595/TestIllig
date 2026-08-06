using Xunit;
using System.Runtime.CompilerServices;

namespace Illig_AI_Platform.Tests.UI;

public class LieferantenassistentEmailTests
{
    [Fact]
    public void NachfrageEmail_BittetUmBestaetigungDerVereinbartenLiefertermine()
    {
        var seitenPfad = Path.Combine(
            ClientPfad(),
            "Pages",
            "Lieferantenassistent",
            "Lieferantenassistent.razor");

        var seitenQuelltext = File.ReadAllText(seitenPfad);

        Assert.Contains("Sehr geehrte Damen und Herren,", seitenQuelltext);
        Assert.Contains(
            "bitte bestätigen Sie uns, ob die vereinbarten Liefertermine für die folgenden Produkte eingehalten werden können",
            seitenQuelltext);
        Assert.Contains("voraussichtlichen neuen Liefertermine", seitenQuelltext);
        Assert.Contains("Mit freundlichen Grüßen", seitenQuelltext);
        Assert.Contains("Bitte um Bestätigung der vereinbarten Liefertermine", seitenQuelltext);
        Assert.Contains("bestellnummern.Take(3)", seitenQuelltext);
        Assert.DoesNotContain(
            "Liefertermine – {_ausgewaehlterLieferantName}",
            seitenQuelltext);
    }

    [Fact]
    public void NachfrageDialog_ZeigtLieferantenkontextUndStrukturierteMailfelder()
    {
        var clientPfad = ClientPfad();

        var seitenQuelltext = File.ReadAllText(Path.Combine(
            clientPfad, "Pages", "Lieferantenassistent", "Lieferantenassistent.razor"));
        var seitenCss = File.ReadAllText(Path.Combine(
            clientPfad, "Pages", "Lieferantenassistent", "Lieferantenassistent.razor.css"));
        var modalQuelltext = File.ReadAllText(Path.Combine(clientPfad, "Components", "Modal.razor"));
        var modalCss = File.ReadAllText(Path.Combine(clientPfad, "Components", "Modal.razor.css"));

        Assert.Contains("la-mail-kontext", seitenQuelltext);
        Assert.Contains("Nachricht an", seitenQuelltext);
        Assert.Contains("la-mail-felder", seitenQuelltext);
        Assert.Contains("Sie können den E-Mail-Text vor dem Senden bearbeiten.", seitenQuelltext);
        Assert.DoesNotContain("Kann vor dem Öffnen bearbeitet werden", seitenQuelltext);
        Assert.Contains("OnClose=", seitenQuelltext);
        Assert.Contains(".la-mail-kontext", seitenCss);
        Assert.Contains("#00457b", seitenCss);
        Assert.Contains(".la-mail-feld__label", seitenCss);
        Assert.Contains("color: var(--color-text-muted)", seitenCss);
        Assert.Contains("aria-modal=\"true\"", modalQuelltext);
        Assert.Contains("la-dialog__schliessen", modalQuelltext);
        Assert.DoesNotContain("Lieferanten-Assist</span>", modalQuelltext);
        Assert.Contains("Title=\"Nachricht an Lieferanten senden\"", seitenQuelltext);
        Assert.Contains("backdrop-filter: blur(8px)", modalCss);
        Assert.Contains("border-top: 4px solid #00457b", modalCss);
        Assert.DoesNotContain("#9bc832", modalCss);
    }

    private static string ClientPfad([CallerFilePath] string testDateiPfad = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(testDateiPfad)!,
            "..",
            "..",
            "Illig-AI-Platform.Client"));
}
