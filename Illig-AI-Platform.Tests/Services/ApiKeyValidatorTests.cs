using Illig_AI_Platform.Shared.Services;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class ApiKeyValidatorTests
{
    [Fact]
    public void IsValid_ReturnsTrue_WhenKeysMatch()
    {
        Assert.True(ApiKeyValidator.IsValid("geheim-123", "geheim-123"));
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenKeysDiffer()
    {
        Assert.False(ApiKeyValidator.IsValid("geheim-123", "anders-456"));
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenProvidedKeyIsNullOrEmpty()
    {
        Assert.False(ApiKeyValidator.IsValid(null, "geheim-123"));
        Assert.False(ApiKeyValidator.IsValid("", "geheim-123"));
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenConfiguredKeyIsNullOrEmpty()
    {
        Assert.False(ApiKeyValidator.IsValid("geheim-123", null));
        Assert.False(ApiKeyValidator.IsValid("geheim-123", ""));
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenLengthsDiffer()
    {
        Assert.False(ApiKeyValidator.IsValid("kurz", "viel-laenger-als-kurz"));
    }
}
