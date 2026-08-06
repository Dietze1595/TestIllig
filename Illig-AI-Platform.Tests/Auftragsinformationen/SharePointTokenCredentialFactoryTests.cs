using Azure.Identity;
using Illig_AI_Platform.Services.Auftragsinformationen;
using Xunit;

namespace Illig_AI_Platform.Tests.Auftragsinformationen;

public class SharePointTokenCredentialFactoryTests
{
    [Fact]
    public void Vollstaendige_CrossTenant_Konfiguration_verwendet_ClientAssertionCredential()
    {
        var credential = SharePointTokenCredentialFactory.Create(new SharePointAuftragsinformationenOptions
        {
            ResourceTenantId = "70c6a234-09a1-4970-bad6-9b11870e8a97",
            ApplicationClientId = "dcc60cbc-d56f-444c-9678-e5f2fd41a197",
            ManagedIdentityClientId = "64fe1cd6-1180-4750-81d1-4c372994ed40",
        });

        Assert.IsType<ClientAssertionCredential>(credential);
    }

    [Fact]
    public void Unvollstaendige_CrossTenant_Konfiguration_wird_abgelehnt()
    {
        var options = new SharePointAuftragsinformationenOptions
        {
            ResourceTenantId = "70c6a234-09a1-4970-bad6-9b11870e8a97",
            ApplicationClientId = "dcc60cbc-d56f-444c-9678-e5f2fd41a197",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => SharePointTokenCredentialFactory.Create(options));

        Assert.Contains("ManagedIdentityClientId", exception.Message);
    }

    [Fact]
    public void Ohne_CrossTenant_Konfiguration_bleibt_DefaultAzureCredential_verfuegbar()
    {
        var credential = SharePointTokenCredentialFactory.Create(new SharePointAuftragsinformationenOptions());

        Assert.IsType<DefaultAzureCredential>(credential);
    }
}
