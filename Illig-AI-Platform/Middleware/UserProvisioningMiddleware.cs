using Illig_AI_Platform.Services;
using Illig_AI_Platform.Shared.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Illig_AI_Platform.Middleware
{
    public class UserProvisioningMiddleware(RequestDelegate next, ILogger<UserProvisioningMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context, UserProfileService profiles, IMemoryCache cache)
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated == true && user.GetUserId() is Guid id)
            {
                if (!cache.TryGetValue(CacheKey(id), out _))
                {
                    try
                    {
                        var email = user.GetEmail();
                        var displayName = user.FindFirst("name")?.Value ?? "";
                        var vorname = user.FindFirst("given_name")?.Value ?? "";
                        var nachname = user.FindFirst("family_name")?.Value ?? "";

                        await profiles.EnsureProfileCreatedAsync(id, email, displayName, nachname, vorname);

                        cache.Set(CacheKey(id), true, TimeSpan.FromHours(2));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to provision user profile for {UserId}.", id);
                    }
                }
            }

            await next(context);
        }

        private static string CacheKey(Guid id) => $"user-provisioned:{id}";
    }
}
