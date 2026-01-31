using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.ServiceDiscovery;

public class ConsulServiceRegistration : IHostedService
{
    private readonly IConsulClient _consulClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsulServiceRegistration> _logger;
    private string? _registrationId;

    public ConsulServiceRegistration(
        IConsulClient consulClient,
        IConfiguration configuration,
        ILogger<ConsulServiceRegistration> logger)
    {
        _consulClient = consulClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceName = _configuration["Service:Name"] ?? "unknown-service";
        var serviceId = $"{serviceName}-{Guid.NewGuid()}";
        var serviceHost = _configuration["Service:Host"] ?? "localhost";
        var servicePort = int.Parse(_configuration["Service:Port"] ?? "80");

        _registrationId = serviceId;

        var registration = new AgentServiceRegistration
        {
            ID = serviceId,
            Name = serviceName,
            Address = serviceHost,
            Port = servicePort,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{serviceHost}:{servicePort}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
            }
        };

        await _consulClient.Agent.ServiceDeregister(serviceId, cancellationToken);
        await _consulClient.Agent.ServiceRegister(registration, cancellationToken);

        _logger.LogInformation($"Service {serviceName} registered with Consul (ID: {serviceId})");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_registrationId))
        {
            await _consulClient.Agent.ServiceDeregister(_registrationId, cancellationToken);
            _logger.LogInformation($"Service deregistered from Consul (ID: {_registrationId})");
        }
    }
}

public static class ConsulServiceDiscoveryExtensions
{
    public static IServiceCollection AddConsulServiceDiscovery(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConsulClient>(sp =>
        {
            var consulHost = configuration["Consul:Host"] ?? "localhost";
            var consulPort = int.Parse(configuration["Consul:Port"] ?? "8500");

            return new ConsulClient(config =>
            {
                config.Address = new Uri($"http://{consulHost}:{consulPort}");
            });
        });

        services.AddHostedService<ConsulServiceRegistration>();
        return services;
    }
}
