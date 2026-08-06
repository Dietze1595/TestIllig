using System.Security.Claims;
using Illig_AI_Platform.Services;
using Illig_AI_Platform.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class AdminAuthorizationHandlerTests
{
    [Fact]
    public async Task Dev_ErfuelltJedeRollenanforderungWieAdmin()
    {
        var requirement = new RolesAuthorizationRequirement([AppRoles.PlausibilityCheck]);
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, AppRoles.Dev)],
            authenticationType: "Test",
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(identity),
            resource: null);

        await new AdminAuthorizationHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }
}
