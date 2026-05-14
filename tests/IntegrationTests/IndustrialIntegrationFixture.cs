extern alias sql;
extern alias rest;

using Grpc.Net.Client;
using Industrial.Sqldata.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace IntegrationTests;

[CollectionDefinition("Industrial integration")]
public sealed class IndustrialIntegrationCollection : ICollectionFixture<IndustrialIntegrationFixture>
{
}

/// <summary>PostgreSQL, Redis, RabbitMQ, SqlData gRPC host, and RestApi (with live consumer) for pipeline tests.</summary>
public sealed class IndustrialIntegrationFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private RabbitMqContainer? _rabbit;

    public WebApplicationFactory<sql::Program> SqlFactory { get; private set; } = null!;
    public WebApplicationFactory<rest::Program> RestFactory { get; private set; } = null!;

    public string RedisConnectionString { get; private set; } = null!;
    public string RabbitHost { get; private set; } = null!;
    public int RabbitPort { get; private set; }

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder().Build();
        _redis = new RedisBuilder().Build();
        _rabbit = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

        await _postgres.StartAsync();
        await _redis.StartAsync();
        await _rabbit.StartAsync();

        RedisConnectionString = _redis.GetConnectionString() + ",abortConnect=false";
        RabbitHost = _rabbit.Hostname;
        RabbitPort = _rabbit.GetMappedPublicPort(5672);

        var pgConn = _postgres.GetConnectionString();

        SqlFactory = new WebApplicationFactory<sql::Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("ConnectionStrings:SqlData", pgConn);
        });

        using (SqlFactory.CreateClient())
        {
        }

        RestFactory = new WebApplicationFactory<rest::Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("ConnectionStrings:Redis", RedisConnectionString);
            b.UseSetting("RabbitMQ:HostName", RabbitHost);
            b.UseSetting("RabbitMQ:Port", RabbitPort.ToString());
            b.UseSetting("RabbitMQ:UserName", "guest");
            b.UseSetting("RabbitMQ:Password", "guest");
            b.UseSetting("RabbitMQ:VirtualHost", "/");
            b.UseSetting("SqlDataGrpc:Address", SqlFactory.Server.BaseAddress!.ToString());

            b.ConfigureTestServices(services =>
            {
                foreach (var d in services.Where(d =>
                             d.ServiceType == typeof(GrpcChannel) ||
                             d.ServiceType == typeof(SensorTelemetry.SensorTelemetryClient)).ToList())
                    services.Remove(d);

                services.AddSingleton(_ =>
                {
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    return GrpcChannel.ForAddress(
                        SqlFactory.Server.BaseAddress,
                        new GrpcChannelOptions { HttpHandler = SqlFactory.Server.CreateHandler() });
                });

                services.AddSingleton(sp =>
                    new SensorTelemetry.SensorTelemetryClient(sp.GetRequiredService<GrpcChannel>()));
            });
        });

        using (RestFactory.CreateClient())
        {
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    public async Task DisposeAsync()
    {
        if (RestFactory is not null)
            await RestFactory.DisposeAsync();

        if (SqlFactory is not null)
            await SqlFactory.DisposeAsync();

        if (_rabbit is not null)
            await _rabbit.DisposeAsync();

        if (_redis is not null)
            await _redis.DisposeAsync();

        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }
}
