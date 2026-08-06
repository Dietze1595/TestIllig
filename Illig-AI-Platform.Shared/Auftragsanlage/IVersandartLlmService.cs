namespace Illig_AI_Platform.Shared.Auftragsanlage;

public interface IVersandartLlmService
{
    Task<string?> ErmittleAsync(string angebotVolltext, CancellationToken cancellationToken = default);
}
