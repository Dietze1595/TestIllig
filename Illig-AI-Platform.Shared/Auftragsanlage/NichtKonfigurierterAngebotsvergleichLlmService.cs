namespace Illig_AI_Platform.Shared.Auftragsanlage;

public class NichtKonfigurierterAngebotsvergleichLlmService : IAngebotsvergleichLlmService
{
    public Task<AngebotsVergleichLlmErgebnis> VergleicheAsync(
        string angebotVolltext, string bestaetigungVolltext, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Azure OpenAI ist nicht konfiguriert (AzureOpenAI:Endpoint/ApiKey/DeploymentName fehlt).");
}
