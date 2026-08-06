using System.Security.Claims;

namespace Illig_AI_Platform.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal user)
        {
            var raw =
                user.FindFirst("oid")?.Value
                ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                ?? user.FindFirst("sub")?.Value;

            return Guid.TryParse(raw, out var id) ? id : null;
        }

        public static string GetEmail(this ClaimsPrincipal user) =>
            (user.FindFirst("emails")?.Value
             ?? user.FindFirst("email")?.Value
             ?? user.FindFirst("preferred_username")?.Value
             ?? user.FindFirst("upn")?.Value
             ?? "").Trim('[', ']', '"');
    }
}
