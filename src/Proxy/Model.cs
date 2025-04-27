namespace Proxy;

public readonly record struct ServiceName(string Name, string Namespace)
{
    public override string ToString() => $"{Namespace}/{Name}";
}

public readonly record struct ApiRouteId(string Value)
{
    public override string ToString() => Value;
}

public sealed record ApiRoute
{
    public required string Path { get; init; } 
    public required string RewritePath { get; init; }
}

public sealed record Api
{
    public required ServiceName ServiceName { get; init; }
    public required Uri Endpoint { get; init; }
    public required IReadOnlyDictionary<ApiRouteId, ApiRoute> Routes { get; init; }
}

public sealed class ProxyConfig
{
    private readonly Dictionary<ServiceName, Api> _apis = new();

    public bool Remove(ServiceName serviceName) => _apis.Remove(serviceName);

    public void AddOrUpdate(ServiceName serviceName, Api api) => _apis[serviceName] = api;

    public IEnumerable<Api> ListApis() => _apis.Values;
}