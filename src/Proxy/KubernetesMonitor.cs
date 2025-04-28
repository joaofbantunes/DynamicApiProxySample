using System.Diagnostics.CodeAnalysis;
using k8s;
using k8s.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Yarp.ReverseProxy.Configuration;

namespace Proxy;

public class KubernetesMonitor(InMemoryConfigProvider proxConfigProvider, ILogger<KubernetesMonitor> logger)
    : BackgroundService
{
    private readonly IDeserializer _deserializer = new StaticDeserializerBuilder(new YamlStaticContext())
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private readonly ProxyConfig _config = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorAsync(stoppingToken);
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // let it shut down gracefully
            }
        }
    }

    private async Task MonitorAsync(CancellationToken ct)
    {
        var config = KubernetesClientConfiguration.InClusterConfig();
        var client = new Kubernetes(config);

        // should use ListNamespacedServiceWithHttpMessagesAsync if wanted to get things from specific namespaces
        // but right now watching the whole cluster 
        var response = client.CoreV1.ListServiceForAllNamespacesWithHttpMessagesAsync(
            watch: true,
            cancellationToken: ct);

        await foreach (var (type, service) in response.WatchAsync<V1Service, V1ServiceList>(cancellationToken: ct))
        {
            var serviceName = new ServiceName(service.Metadata.Name, service.Metadata.Namespace());
            if (type == WatchEventType.Deleted)
            {
                if (_config.Remove(serviceName))
                {
                    logger.LogInformation("Service {Service} deleted, removing config", serviceName);
                }

                continue;
            }

            if (!TryGetConfig(service, out var appConfig))
            {
                if (_config.Remove(serviceName))
                {
                    logger.LogInformation("Service {Service} configuration removed, removing config", serviceName);
                }
                else
                {
                    logger.LogDebug("No config found for service {Service}", service.Metadata.Name);
                }

                continue;
            }

            var serviceRoutesConfig = _deserializer.Deserialize<ApiConfig>(appConfig);

            if (serviceRoutesConfig.Routes.Count == 0)
            {
                logger.LogWarning("Service {Service} has no routes defined", serviceName);
                continue;
            }

            _config.AddOrUpdate(
                serviceName,
                new Api
                {
                    ServiceName = serviceName,
                    // this uri is very hammered, need to investigate how to do it properly
                    Endpoint = new Uri(
                        $"http://{serviceName.Name}.{serviceName.Namespace}:{service.Spec.Ports.First().Port}"),
                    Routes = serviceRoutesConfig.Routes.ToDictionary(
                        kvp => new ApiRouteId(kvp.Key),
                        kvp => new ApiRoute
                        {
                            Path = kvp.Value.Path,
                            RewritePath = kvp.Value.RewritePath,
                        })
                });

            UpdateYarpConfig();

            logger.LogInformation("Service {Service} configuration updated", serviceName);
        }
    }

    private static bool TryGetConfig(V1Service service, [MaybeNullWhen(false)] out string appConfig)
    {
        appConfig = null;

        return service.Metadata?.Annotations is not null
               && service.Metadata.Annotations.TryGetValue("dynamicapiproxysample/config", out appConfig);
    }

    private void UpdateYarpConfig()
    {
        proxConfigProvider.Update(
            _config.ListApis().SelectMany(api =>
            {
                var serviceName = api.ServiceName;
                return api.Routes.Select(r => new RouteConfig
                {
                    RouteId = $"{api.ServiceName}_{r.Key}",
                    ClusterId = serviceName.ToString(),
                    Match = new RouteMatch
                    {
                        Path = r.Value.Path
                    },
                    Transforms =
                    [
                        new Dictionary<string, string>
                        {
                            ["PathPattern"] = r.Value.RewritePath
                        }
                    ]
                });
            }).ToArray(),
            _config.ListApis().Select(api =>
            {
                var serviceName = api.ServiceName.ToString();
                return new ClusterConfig
                {
                    ClusterId = serviceName,
                    Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                    {
                        [serviceName] = new() { Address = api.Endpoint.OriginalString }
                    }
                };
            }).ToArray());
    }
}