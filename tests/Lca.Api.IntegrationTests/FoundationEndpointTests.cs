using System.Net;
using System.Net.Http.Json;
using Lca.Api.Contracts;
using Lca.Api.Infrastructure;

namespace Lca.Api.IntegrationTests;

public sealed class FoundationEndpointTests(LcaApiFactory factory) : IClassFixture<LcaApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointsAreAvailable(string path)
    {
        using HttpResponseMessage response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StatusReturnsContractAndGeneratedCorrelationId()
    {
        using HttpResponseMessage response = await client.GetAsync("/api/v1/system/status");
        SystemStatusResponse? status = await response.Content.ReadFromJsonAsync<SystemStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(status);
        Assert.Equal("ok", status.Status);
        Assert.False(string.IsNullOrWhiteSpace(status.CorrelationId));
        Assert.Equal(status.CorrelationId, response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task ValidCorrelationIdIsEchoed()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/system/status");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "migration-test-123");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal("migration-test-123", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task UnknownRouteReturnsProblemDetails()
    {
        using HttpResponseMessage response = await client.GetAsync("/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TenantEndpointDeniesUnauthenticatedRequest()
    {
        using HttpResponseMessage response = await client.GetAsync("/api/v1/test/tenant");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TenantEndpointDeniesAuthenticatedUserWithoutTenant()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/test/tenant");
        request.Headers.Add("X-Test-User", "user-1");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TenantEndpointUsesTrustedAuthenticatedClaim()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/test/tenant");
        request.Headers.Add("X-Test-User", "user-1");
        request.Headers.Add("X-Test-Tenant", "tenant-a");

        using HttpResponseMessage response = await client.SendAsync(request);
        Dictionary<string, string>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("tenant-a", body?["tenantId"]);
    }
}
