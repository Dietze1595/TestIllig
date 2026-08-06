using Illig_AI_Platform.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Illig_AI_Platform.Services;

/// <summary>
/// Lässt Nutzer mit der Rolle <see cref="AppRoles.Admin"/> oder <see cref="AppRoles.Dev"/>
/// jede rollenbasierte Autorisierung passieren. Greift für jedes [Authorize(Roles=…)],
/// ohne dass die privilegierten Rollen bei jedem Endpunkt einzeln eingetragen werden müssen.
/// </summary>
public class AdminAuthorizationHandler : AuthorizationHandler<RolesAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, RolesAuthorizationRequirement requirement)
    {
        if (context.User.IsInRole(AppRoles.Admin) || context.User.IsInRole(AppRoles.Dev))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
