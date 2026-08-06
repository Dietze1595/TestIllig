using Illig_AI_Platform.Shared.Data;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class AppRolesTests
{
    [Fact]
    public void Seed_EnthaeltEindeutigeDevRolle()
    {
        var dev = Assert.Single(AppRoles.Seed, role => role.Name == AppRoles.Dev);

        Assert.Equal(7, dev.Id);
        Assert.Equal(AppRoles.Seed.Count, AppRoles.Seed.Select(role => role.Id).Distinct().Count());
        Assert.Equal(AppRoles.Seed.Count, AppRoles.Seed.Select(role => role.Name).Distinct().Count());
    }
}
