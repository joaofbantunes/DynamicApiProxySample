using Proxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<KubernetesMonitor>();
builder.Services.AddReverseProxy().LoadFromMemory([], []);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapReverseProxy();

app.Run();

