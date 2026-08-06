using System.Runtime.CompilerServices;
using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class AdminBenutzerverwaltungDesignTests
{
    [Fact]
    public void Seite_IstAdminGeschuetztUndListetNutzerMitRollenauswahl()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Admin", "Benutzerverwaltung.razor"));
        var css = File.ReadAllText(ClientFile("Pages", "Admin", "Benutzerverwaltung.razor.css"));

        Assert.Contains("@page \"/admin/benutzer\"", page);
        Assert.Contains("@using Illig_AI_Platform.Client.Components.Admin", page);
        // Client-seitiges Gating auf Admin (zusätzlich zum serverseitigen [Authorize(Roles=Admin)]).
        Assert.Contains("<RoleGuard Roles=\"@(new[] { AppClientRoles.Admin })\"", page);
        Assert.Contains("RollenMehrfachauswahl", page);
        Assert.Contains("Name oder E-Mail suchen", page);
        Assert.Contains(".benutzer-tabelle", css);
    }

    [Fact]
    public void Seite_SpeichertNurBeiAenderungUndSchuetztEigeneAdminRolle()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Admin", "Benutzerverwaltung.razor"));

        // Speichern-Button ist deaktiviert, solange nichts geändert wurde (expliziter Speichern-Fluss).
        Assert.Contains("Disabled=\"@(!zeile.Geaendert || zeile.Speichert)\"", page);
        // Selbstschutz: eigene Admin-Rolle ist gesperrt.
        Assert.Contains("GesperrteRollen", page);
    }

    [Fact]
    public void Multiselect_RendertCheckboxenUndBeachtetSperre()
    {
        var comp = File.ReadAllText(ClientFile("Components", "Admin", "RollenMehrfachauswahl.razor"));
        var css = File.ReadAllText(ClientFile("Components", "Admin", "RollenMehrfachauswahl.razor.css"));
        var js = File.ReadAllText(ClientFile("Components", "Admin", "RollenMehrfachauswahl.razor.js"));

        Assert.Contains("type=\"checkbox\"", comp);
        Assert.Contains("disabled=\"@gesperrt\"", comp);
        Assert.Contains("OnUmgeschaltet", comp);
        Assert.Contains("@GewaehlteLabels[0]", comp);
        Assert.Contains("+@(GewaehlteLabels.Count - 1)", comp);
        Assert.DoesNotContain("rollenauswahl__chips", comp);
        Assert.Contains("dropdownPositionieren", comp);
        Assert.Contains("position: fixed", css);
        Assert.Contains("document.addEventListener(\"scroll\"", js);
    }

    [Fact]
    public void ProfilMenu_ZeigtBenutzerverwaltungNurFuerAdmins()
    {
        var loginDisplay = File.ReadAllText(ClientFile("Layout", "LoginDisplay.razor"));

        Assert.Contains("HatBenutzerverwaltung", loginDisplay);
        Assert.Contains("HatAdminBerechtigungen", loginDisplay);
        Assert.Contains("href=\"@AppRoutes.AdminBenutzer\"", loginDisplay);
        Assert.Contains("Benutzerverwaltung", loginDisplay);
    }

    [Fact]
    public void Seite_KennzeichnetEntwicklerSichtbarMitDevBadge()
    {
        var page = File.ReadAllText(ClientFile("Pages", "Admin", "Benutzerverwaltung.razor"));
        var css = File.ReadAllText(ClientFile("Pages", "Admin", "Benutzerverwaltung.razor.css"));

        Assert.Contains("[\"DEV\"] = \"Entwickler (DEV)\"", page);
        Assert.Contains("benutzer-dev-badge", page);
        Assert.Contains("IstEntwickler", page);
        Assert.Contains(".benutzer-dev-badge", css);
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
