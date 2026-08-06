using System.Security.Cryptography;
using System.Text;

namespace Illig_AI_Platform.Shared.Services;

/// <summary>
/// Vergleicht API-Keys in konstanter Zeit, um Timing-Angriffe zu vermeiden.
/// </summary>
public static class ApiKeyValidator
{
    public static bool IsValid(string? providedKey, string? configuredKey)
    {
        if (string.IsNullOrEmpty(providedKey) || string.IsNullOrEmpty(configuredKey))
            return false;

        var provided = Encoding.UTF8.GetBytes(providedKey);
        var configured = Encoding.UTF8.GetBytes(configuredKey);

        if (provided.Length != configured.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(provided, configured);
    }
}
