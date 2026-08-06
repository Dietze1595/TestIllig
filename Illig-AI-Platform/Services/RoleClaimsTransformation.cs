using System.Security.Claims;
using Illig_AI_Platform.Shared.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Illig_AI_Platform.Services
{
    public class RoleClaimsTransformation(AppDbContext db, IMemoryCache cache) : IClaimsTransformation
    {
        /// <summary>
        /// Cache-Schlüssel der pro Nutzer gecachten Rollen. Zentral, damit die Admin-Rollenverwaltung
        /// den Cache nach einer Änderung gezielt invalidieren kann, ohne den String zu duplizieren.
        /// </summary>
        public static string RoleCacheKey(Guid userId) => $"user-roles:{userId}";

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity identity
                || principal.GetUserId() is not Guid userId)
            {
                return principal;
            }

            if (identity.HasClaim(c => c.Type == "app-roles-loaded"))
                return principal;

            var roles = await cache.GetOrCreateAsync(RoleCacheKey(userId), async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await db.UserProfileRoles
                    .Where(upr => upr.UserProfileId == userId)
                    .Select(upr => upr.Role.Name)
                    .ToListAsync();
            }) ?? [];

            foreach (var role in roles)
                identity.AddClaim(new Claim(identity.RoleClaimType, role));

            identity.AddClaim(new Claim("app-roles-loaded", "true"));
            return principal;
        }
    }
}
