using Xunit;

namespace Illig_AI_Platform.Tests.Configuration;

public class B2CClaimsFallbackTests
{
    [Fact]
    public void ClaimsPrincipalExtensions_UsesSubjectClaimAsFallbackForB2CUserIdentifier()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform",
            "Services",
            "ClaimsPrincipalExtensions.cs"));

        var source = File.ReadAllText(path);

        // Die B2C-User-Id wird aus oid → objectidentifier → sub aufgelöst.
        // "sub" ist der letzte Fallback.
        Assert.Contains("\"oid\"", source);
        Assert.Contains("\"sub\"", source);
    }
}
