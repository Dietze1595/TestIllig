using System.Net;
using System.Net.Http.Json;
using Illig_AI_Platform.Client;
using Illig_AI_Platform.Client.Models;
using Illig_AI_Platform.Client.Services;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class UserProfileStateTests
{
    private class FakeInnerHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }

    private static UserProfileState CreateState(HttpResponseMessage response)
    {
        var apiErrorHandler = new ApiErrorHandler { InnerHandler = new FakeInnerHandler(response) };
        var httpClient = new HttpClient(apiErrorHandler) { BaseAddress = new Uri("http://localhost/") };
        return new UserProfileState(httpClient);
    }

    [Fact]
    public async Task SaveDisplayNameAsync_ReturnsFalse_WhenServerReturnsForbidden()
    {
        var state = CreateState(new HttpResponseMessage(HttpStatusCode.Forbidden));

        var result = await state.SaveDisplayNameAsync("Neuer Name");

        Assert.False(result);
    }

    [Fact]
    public async Task SaveDisplayNameAsync_ReturnsFalse_WhenServerReturnsInternalServerError()
    {
        var state = CreateState(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await state.SaveDisplayNameAsync("Neuer Name");

        Assert.False(result);
    }

    [Fact]
    public async Task SaveDisplayNameAsync_ReturnsTrue_WhenServerReturnsSuccess()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        var state = CreateState(response);

        var result = await state.SaveDisplayNameAsync("Neuer Name");

        Assert.True(result);
    }

    [Fact]
    public async Task HasAnyRoleAsync_GewaehrtDevDieGleichenRechteWieAdmin()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new UserProfileDto
            {
                Roles = [AppClientRoles.Dev]
            })
        };
        var state = CreateState(response);

        var result = await state.HasAnyRoleAsync(AppClientRoles.PlausibilityCheck);

        Assert.True(result);
        Assert.True(state.HatAdminBerechtigungen);
        Assert.Contains(AppClientRoles.Dev, state.Profile!.Roles);
        Assert.DoesNotContain(AppClientRoles.Admin, state.Profile.Roles);
    }
}
