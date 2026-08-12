namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public static class StuecklistenImportFormatErmittlung
{
    public static bool TryErmitteln(
        string? maschinentyp,
        out StuecklistenImportFormat format)
    {
        var normalisiert = string.Concat(
            (maschinentyp ?? "").Where(zeichen => !char.IsWhiteSpace(zeichen)));

        if (normalisiert.StartsWith("RDM73", StringComparison.OrdinalIgnoreCase))
            format = StuecklistenImportFormat.Rdm73k;
        else if (normalisiert.StartsWith("RDM75", StringComparison.OrdinalIgnoreCase))
            format = StuecklistenImportFormat.Rdm75Kc;
        else if (normalisiert.StartsWith("RDM76", StringComparison.OrdinalIgnoreCase))
            format = StuecklistenImportFormat.Rdm76Kb;
        else if (normalisiert.StartsWith("RDK80", StringComparison.OrdinalIgnoreCase))
            format = StuecklistenImportFormat.Rdk80k;
        else
        {
            format = default;
            return false;
        }

        return true;
    }
}
