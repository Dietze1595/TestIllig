using Microsoft.Extensions.Options;

namespace Illig_AI_Platform.Services.Auftragsinformationen;

public sealed class SharePointAuftragsinformationenWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SharePointAuftragsinformationenOptions> options,
    ILogger<SharePointAuftragsinformationenWorker> logger) : BackgroundService
{
    private readonly SharePointAuftragsinformationenOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        // Beim App-Start zuerst der Webanwendung Zeit zum Hochfahren geben.
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var importer = scope.ServiceProvider.GetRequiredService<AuftragsinformationenImportService>();
                var ergebnis = await importer.SynchronisierenAsync(stoppingToken);
                logger.LogInformation(
                    "SharePoint-Auftragsinformationen synchronisiert: {Verarbeitet} verarbeitet, {Uebersprungen} uebersprungen, {Geloescht} geloescht, {Fehlgeschlagen} fehlgeschlagen.",
                    ergebnis.Verarbeitet, ergebnis.Uebersprungen, ergebnis.Geloescht, ergebnis.Fehlgeschlagen);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SharePoint-Auftragsinformationen-Synchronisation fehlgeschlagen.");
            }

            await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, _options.PollingMinutes)), stoppingToken);
        }
    }
}
