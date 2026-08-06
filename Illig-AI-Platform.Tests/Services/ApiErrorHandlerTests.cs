using System.Net;
using Illig_AI_Platform.Client.Services;
using Xunit;

namespace Illig_AI_Platform.Tests.Services;

public class ApiErrorHandlerTests
{
    private class FakeInnerHandler(HttpResponseMessage? response, Exception? exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (exception is not null) throw exception;
            return Task.FromResult(response!);
        }
    }

    private static HttpClient CreateClient(HttpResponseMessage? response = null, Exception? exception = null)
    {
        var handler = new ApiErrorHandler { InnerHandler = new FakeInnerHandler(response, exception) };
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
    }

    [Fact]
    public async Task SendAsync_ThrowsApiRequestException_OnUnauthorized()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => client.GetAsync("test"));

        Assert.Equal(401, ex.StatusCode);
        Assert.Contains("Sitzung", ex.Message);
    }

    [Fact]
    public async Task SendAsync_ThrowsApiRequestException_OnForbidden()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.Forbidden));

        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => client.GetAsync("test"));

        Assert.Equal(403, ex.StatusCode);
        Assert.Contains("Berechtigung", ex.Message);
    }

    [Fact]
    public async Task SendAsync_ThrowsApiRequestException_OnServerError()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => client.GetAsync("test"));

        Assert.Equal(500, ex.StatusCode);
        Assert.Contains("Server", ex.Message);
    }

    [Fact]
    public async Task SendAsync_ThrowsApiRequestException_OnOtherClientError()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => client.GetAsync("test"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("Anfrage", ex.Message);
    }

    [Fact]
    public async Task SendAsync_ThrowsApiRequestException_OnNetworkFailure()
    {
        var client = CreateClient(exception: new HttpRequestException("Connection refused"));

        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => client.GetAsync("test"));

        Assert.Null(ex.StatusCode);
        Assert.Contains("Verbindung", ex.Message);
    }

    [Fact]
    public async Task SendAsync_ThrowsApiRequestException_OnTimeout()
    {
        var client = CreateClient(exception: new TaskCanceledException("The request timed out."));

        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => client.GetAsync("test"));

        Assert.Null(ex.StatusCode);
        Assert.Contains("Zeitüberschreitung", ex.Message);
    }

    [Fact]
    public async Task SendAsync_PropagatesCancellation_WhenCallerCancelled()
    {
        using var cts = new CancellationTokenSource();
        var client = CreateClient(exception: new TaskCanceledException("Cancelled by caller."));
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => client.GetAsync("test", cts.Token));
    }

    [Fact]
    public async Task SendAsync_PassesThrough_OnNotFound()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.NotFound));

        var response = await client.GetAsync("test");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_PassesThroughDocumentTypeHint_ForInnendienstUpload()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity));

        var response = await client.PostAsync(
            "api/auftragsanlage/innendienst/bestaetigung",
            new StringContent("test"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_PassesThroughDocumentTypeHint_ForVertriebAnalysis()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity));

        var response = await client.PostAsync(
            "api/v1/auftragsanlage/vertrieb/angebot/analyse",
            new StringContent("test"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_ThrowsApiRequestException_OnUnprocessableEntity_ForOtherEndpoints()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity));

        var ex = await Assert.ThrowsAsync<ApiRequestException>(
            () => client.PostAsync("api/anderer-endpunkt", new StringContent("test")));

        Assert.Equal(422, ex.StatusCode);
        Assert.Contains("Anfrage", ex.Message);
    }

    [Fact]
    public async Task SendAsync_PassesThrough_OnSuccess()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.OK));

        var response = await client.GetAsync("test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
