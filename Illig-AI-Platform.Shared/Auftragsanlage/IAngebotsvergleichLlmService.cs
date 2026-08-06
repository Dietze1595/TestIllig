namespace Illig_AI_Platform.Shared.Auftragsanlage;

public interface IAngebotsvergleichLlmService
{
    Task<AngebotsVergleichLlmErgebnis> VergleicheAsync(
        string angebotVolltext, string bestaetigungVolltext, CancellationToken cancellationToken = default);
}
