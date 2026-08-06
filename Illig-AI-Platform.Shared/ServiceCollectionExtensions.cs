using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Lieferantenassistent;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.SapImport;
using Illig_AI_Platform.Shared.Services;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Illig_AI_Platform.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config["Db:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Db:ConnectionString ist nicht konfiguriert.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 0, 40))));

        services.AddScoped<UserProfileService>();
        services.AddScoped<SapDirektImportService>();
        services.AddScoped<KundenstammService>();

        services.AddScoped<ISondermerkmalService, DatenbankSondermerkmalService>();

        // Stücklistenprüfung: Ohne konfigurierten Azure-Endpunkt bleibt die App lauffähig,
        // nur der Endpunkt selbst liefert einen klaren Fehler (siehe NichtKonfigurierterDocumentAnalyseService).
        var documentIntelligenceEndpoint = config["AzureDocumentIntelligence:Endpoint"];
        var documentIntelligenceKey = config["AzureDocumentIntelligence:ApiKey"];
        if (!string.IsNullOrWhiteSpace(documentIntelligenceEndpoint) && !string.IsNullOrWhiteSpace(documentIntelligenceKey))
        {
            services.AddSingleton(new DocumentIntelligenceClient(
                new Uri(documentIntelligenceEndpoint),
                new AzureKeyCredential(documentIntelligenceKey)));
            services.AddScoped<IDocumentAnalyseService, AzureDocumentIntelligenceAnalyseService>();
            services.AddScoped<IAngebotsdokumentAnalyseService, AzureAngebotsdokumentAnalyseService>();
        }
        else
        {
            services.AddScoped<IDocumentAnalyseService, NichtKonfigurierterDocumentAnalyseService>();
            services.AddScoped<IAngebotsdokumentAnalyseService, NichtKonfigurierterAngebotsdokumentAnalyseService>();
        }

        // Auftragsanlage-Vergleich: Ohne konfigurierten Azure-OpenAI-Zugang bleibt die App
        // lauffähig, nur der Innendienst-Endpunkt liefert einen klaren Fehler
        // (siehe NichtKonfigurierterAngebotsvergleichLlmService).
        var openAiEndpoint = config["AzureOpenAI:Endpoint"];
        var openAiKey = config["AzureOpenAI:ApiKey"];
        var openAiDeployment = config["AzureOpenAI:DeploymentName"];
        if (!string.IsNullOrWhiteSpace(openAiEndpoint) && !string.IsNullOrWhiteSpace(openAiKey) &&
            !string.IsNullOrWhiteSpace(openAiDeployment))
        {
            var azureOpenAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), new AzureKeyCredential(openAiKey));
            services.AddSingleton(azureOpenAiClient.GetChatClient(openAiDeployment));
            services.AddScoped<IAngebotsvergleichLlmService, AzureAngebotsvergleichLlmService>();
            services.AddScoped<IVersandartLlmService, AzureVersandartLlmService>();
        }
        else
        {
            services.AddScoped<IAngebotsvergleichLlmService, NichtKonfigurierterAngebotsvergleichLlmService>();
            services.AddScoped<IVersandartLlmService, NichtKonfigurierterVersandartLlmService>();
        }

        // Stücklistenprüfung: Ohne konfigurierten Blob-Storage bleibt die App lauffähig,
        // nur der Endpunkt selbst liefert einen klaren Fehler (siehe NichtKonfigurierterBlobStorageService).
        var blobStorageConnectionString = config["AzureBlobStorage:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(blobStorageConnectionString))
        {
            var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(blobStorageConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("stuecklistenpruefung-dokumente");
            services.AddSingleton(containerClient);
            services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

            // Auftragsanlage (Angebot + Kundenbestellung): eigener Container statt des mit der
            // Stücklistenprüfung geteilten — daher keyed statt der Standard-Registrierung oben.
            var invoicesContainerClient = blobServiceClient.GetBlobContainerClient("invoices");
            services.AddKeyedScoped<IBlobStorageService>(
                "auftragsanlage", (_, _) => new AzureBlobStorageService(invoicesContainerClient));
        }
        else
        {
            services.AddScoped<IBlobStorageService, NichtKonfigurierterBlobStorageService>();
            services.AddKeyedScoped<IBlobStorageService, NichtKonfigurierterBlobStorageService>("auftragsanlage");
        }

        services.AddScoped<StuecklistenpruefungVerlaufService>();
        services.AddScoped<StuecklistenAufbauService>();
        services.AddScoped<StuecklistenImportService>();
        services.AddScoped<AngebotsService>();
        services.AddScoped<LieferantenassistentAbfrageService>();
        services.AddScoped<LieferantenassistentImportService>();

        return services;
    }
}
