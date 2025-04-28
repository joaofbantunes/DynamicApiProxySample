using YamlDotNet.Serialization;

namespace Proxy;

public sealed record ApiConfig
{
    public Dictionary<string, ApiRouteConfig> Routes { get; set; } = [];
}

public sealed record ApiRouteConfig
{
    public string Path { get; set; } = null!;
    public string RewritePath { get; set; } = null!;
}

[YamlStaticContext]
[YamlSerializable(typeof(ApiConfig))]
[YamlSerializable(typeof(ApiRouteConfig))]
public sealed partial class YamlStaticContext : StaticContext;