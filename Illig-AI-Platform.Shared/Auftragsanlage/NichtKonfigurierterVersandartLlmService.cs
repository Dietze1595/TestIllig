namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Ohne Azure-OpenAI-Konfiguration bleibt die deterministische Angebotsanalyse nutzbar.
/// </summary>
public class NichtKonfigurierterVersandartLlmService : IVersandartLlmService
{
    public Task<string?> ErmittleAsync(
        string angebotVolltext, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
