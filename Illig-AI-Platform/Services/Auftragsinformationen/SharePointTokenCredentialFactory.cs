using Azure.Core;
using Azure.Identity;

namespace Illig_AI_Platform.Services.Auftragsinformationen;

public static class SharePointTokenCredentialFactory
{
    private static readonly TokenRequestContext TokenExchangeContext =
        new(["api://AzureADTokenExchange/.default"]);

    public static TokenCredential Create(SharePointAuftragsinformationenOptions options)
    {
        var resourceTenantId = LeerZuNull(options.ResourceTenantId);
        var applicationClientId = LeerZuNull(options.ApplicationClientId);
        var managedIdentityClientId = LeerZuNull(options.ManagedIdentityClientId);

        var federationKonfiguriert = resourceTenantId is not null || applicationClientId is not null;
        if (federationKonfiguriert)
        {
            if (resourceTenantId is null || applicationClientId is null || managedIdentityClientId is null)
            {
                throw new InvalidOperationException(
                    "Fuer den tenantuebergreifenden SharePoint-Zugriff muessen " +
                    "ResourceTenantId, ApplicationClientId und ManagedIdentityClientId gesetzt sein.");
            }

            var managedIdentityCredential = new ManagedIdentityCredential(
                ManagedIdentityId.FromUserAssignedClientId(managedIdentityClientId));

            return new ClientAssertionCredential(
                resourceTenantId,
                applicationClientId,
                async cancellationToken =>
                    (await managedIdentityCredential.GetTokenAsync(
                        TokenExchangeContext,
                        cancellationToken).ConfigureAwait(false)).Token);
        }

        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = managedIdentityClientId,
        });
    }

    private static string? LeerZuNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
