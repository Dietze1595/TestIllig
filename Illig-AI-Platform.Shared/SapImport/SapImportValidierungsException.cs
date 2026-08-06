namespace Illig_AI_Platform.Shared.SapImport;

public sealed class SapImportValidierungsException(IReadOnlyList<string> fehler)
    : Exception(string.Join(" ", fehler))
{
    public IReadOnlyList<string> Fehler { get; } = fehler;
}
