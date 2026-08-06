using System.Reflection;
using Illig_AI_Platform.Shared.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Illig_AI_Platform.Tests.Migrations;

public class MigrationRegressionTests
{
    [Fact]
    public void AddSharePointAuftragsinformationen_LegtKeineBestehendenFachtabellenNeuAn()
    {
        var migration = new AddSharePointAuftragsinformationen();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        var up = typeof(AddSharePointAuftragsinformationen).GetMethod(
            "Up", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(up);
        up.Invoke(migration, [builder]);

        var angelegteTabellen = builder.Operations
            .OfType<CreateTableOperation>()
            .Select(operation => operation.Name)
            .ToArray();
        Assert.Equal(
            ["SharePointSynchronisationsstaende", "AuftragsdokumentMerkmale"],
            angelegteTabellen);

        var sql = string.Join("\n", builder.Operations
            .OfType<SqlOperation>()
            .Select(operation => operation.Sql));
        Assert.Contains("CREATE TABLE IF NOT EXISTS `Auftragsdokumente`", sql);
        Assert.DoesNotContain("Dispositionspositionen", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE TABLE `Angebote`", sql, StringComparison.OrdinalIgnoreCase);
    }
}
