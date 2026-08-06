using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class ProfileMenuHeaderTests
{
    [Fact]
    public void LoginDisplay_UsesHoverProfileMenu_AndLinksToProfileRoute()
    {
        var loginDisplayPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Layout",
            "LoginDisplay.razor"));

        var loginDisplayCssPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Layout",
            "LoginDisplay.razor.css"));

        var profilePagePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Pages",
            "Profile.razor"));

        var loginDisplaySource = File.ReadAllText(loginDisplayPath);
        var loginDisplayCssSource = File.ReadAllText(loginDisplayCssPath);
        var profilePageSource = File.ReadAllText(profilePagePath);

        Assert.Contains("profile-menu", loginDisplaySource);
        Assert.Contains("href=\"@AppRoutes.Profile\"", loginDisplaySource);
        Assert.Contains("Logout", loginDisplaySource);
        Assert.Contains(".profile-menu:hover .profile-menu__dropdown", loginDisplayCssSource);
        Assert.Contains("box-shadow: none;", loginDisplayCssSource);
        Assert.Contains("background: transparent;", loginDisplayCssSource);
        Assert.Contains(".profile-action--danger", loginDisplayCssSource);
        Assert.Contains("@page \"/profile\"", profilePageSource);
    }

    [Fact]
    public void LoginDisplay_ShowsSkeletonUntilProfileLoaded()
    {
        var loginDisplayPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Layout",
            "LoginDisplay.razor"));

        var loginDisplayCssPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Layout",
            "LoginDisplay.razor.css"));

        var loginDisplaySource = File.ReadAllText(loginDisplayPath);
        var loginDisplayCssSource = File.ReadAllText(loginDisplayCssPath);

        Assert.Contains("ProfileState.Loaded", loginDisplaySource);
        Assert.Contains("profile-menu__skeleton--name", loginDisplaySource);
        Assert.Contains("profile-menu__skeleton--email", loginDisplaySource);
        Assert.Contains("profile-menu__skeleton--avatar", loginDisplaySource);
        Assert.Contains(".profile-menu__skeleton", loginDisplayCssSource);
    }
}
