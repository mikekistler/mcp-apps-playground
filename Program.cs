using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

// Map tool names to UI resource URIs for _meta.ui injection
var toolUiMap = new Dictionary<string, string>
{
    ["hello_world"] = "ui://mcp-apps-playground/greeting",
    ["list_sort"] = "ui://mcp-apps-playground/list-sort",
    ["flame_graph"] = "ui://mcp-apps-playground/flame-graph",
    ["feature_flags"] = "ui://mcp-apps-playground/feature-flags",
    ["database_query"] = "ui://mcp-apps-playground/database-query",
    ["weather_forecast"] = "ui://mcp-apps-playground/weather-forecast",
};

if (args.Contains("--stdio"))
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });
    RegisterHttpClient(builder.Services);
    ConfigureMcp(builder.Services.AddMcpServer(ConfigureOptions).WithStdioServerTransport());
    await builder.Build().RunAsync();
}
else
{
    var builder = WebApplication.CreateBuilder(args);
    RegisterHttpClient(builder.Services);
    ConfigureMcp(builder.Services.AddMcpServer(ConfigureOptions).WithHttpTransport());
    var app = builder.Build();
    app.MapMcp();
    app.Run();
}

void ConfigureOptions(McpServerOptions options)
{
    options.ServerInfo = new Implementation { Name = "mcp-apps-playground", Version = "1.0.0" };
    options.Capabilities = new ServerCapabilities
    {
        Tools = new ToolsCapability { ListChanged = true },
        Resources = new ResourcesCapability { Subscribe = true, ListChanged = true },
    };
}

void ConfigureMcp(IMcpServerBuilder mcpBuilder)
{
    mcpBuilder
        .WithTools<PlaygroundTools>()
        .WithResources<PlaygroundResources>()
        .WithRequestFilters(filters =>
        {
            filters.AddListToolsFilter(next => async (context, ct) =>
            {
                var result = await next(context, ct);
                foreach (var tool in result.Tools)
                {
                    if (toolUiMap.TryGetValue(tool.Name, out var resourceUri))
                    {
                        tool.Meta ??= new JsonObject();
                        tool.Meta["ui"] = new JsonObject
                        {
                            ["resourceUri"] = resourceUri,
                            ["visibility"] = new JsonArray("model", "app")
                        };
                    }
                }
                return result;
            });
        });
}

void RegisterHttpClient(IServiceCollection services)
{
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.weather.gov") };
    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("mcp-apps-playground", "1.0"));
    services.AddSingleton(httpClient);
}
