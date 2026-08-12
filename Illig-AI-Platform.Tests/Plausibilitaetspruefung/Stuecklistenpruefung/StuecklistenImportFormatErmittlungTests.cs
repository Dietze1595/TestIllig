using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class StuecklistenImportFormatErmittlungTests
{
    [Theory]
    [InlineData("RDM 73K_konf", StuecklistenImportFormat.Rdm73k)]
    [InlineData("rdm73k", StuecklistenImportFormat.Rdm73k)]
    [InlineData("RDM 75Kc_Siemens", StuecklistenImportFormat.Rdm75Kc)]
    [InlineData("RDM76Kb", StuecklistenImportFormat.Rdm76Kb)]
    [InlineData("RDK 80k_Siemens_konf_ab_01.2013", StuecklistenImportFormat.Rdk80k)]
    [InlineData("rdk80k", StuecklistenImportFormat.Rdk80k)]
    public void TryErmitteln_BekannterMaschinentyp_LiefertFormat(
        string maschinentyp,
        StuecklistenImportFormat erwartetesFormat)
    {
        var erkannt = StuecklistenImportFormatErmittlung.TryErmitteln(maschinentyp, out var format);

        Assert.True(erkannt);
        Assert.Equal(erwartetesFormat, format);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unbekannte Maschine")]
    public void TryErmitteln_UnbekannterMaschinentyp_LiefertFalse(string? maschinentyp)
    {
        Assert.False(StuecklistenImportFormatErmittlung.TryErmitteln(maschinentyp, out _));
    }
}
