using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RestApiService.Tests;

public sealed class RestApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("Testing");
}

public sealed class RestApiHealthTests : IClassFixture<RestApiWebApplicationFactory>
{
    private readonly RestApiWebApplicationFactory _factory;

    public RestApiHealthTests(RestApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_ok_without_external_infra()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ok", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task System_status_returns_json_without_throwing()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/system/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("redis", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sqlDataGrpc", body, StringComparison.OrdinalIgnoreCase);
    }
}
