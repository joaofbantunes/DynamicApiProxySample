namespace Proxy;

public sealed record ApiConfig
{
    public Dictionary<string, ApiRouteConfig> Routes { get; init; } = [];
}

public sealed record ApiRouteConfig
{
    public string Path { get; init; } = null!;
    public string RewritePath { get; init; } = null!;
}