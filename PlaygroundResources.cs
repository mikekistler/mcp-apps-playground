using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

[McpServerResourceType]
public sealed class PlaygroundResources
{
    private static readonly string UiDir = Path.Combine(AppContext.BaseDirectory, "ui");

    [McpServerResource(UriTemplate = "ui://mcp-apps-playground/greeting", Name = "greeting-ui", MimeType = "text/html;profile=mcp-app")]
    [Description("Interactive greeting UI panel")]
    public static string GetGreetingUi() => File.ReadAllText(Path.Combine(UiDir, "greeting.html"));

    [McpServerResource(UriTemplate = "ui://mcp-apps-playground/list-sort", Name = "list-sort-ui", MimeType = "text/html;profile=mcp-app")]
    [Description("Interactive list sorting UI panel")]
    public static string GetListSortUi() => File.ReadAllText(Path.Combine(UiDir, "list-sort.html"));

    [McpServerResource(UriTemplate = "ui://mcp-apps-playground/flame-graph", Name = "flame-graph-ui", MimeType = "text/html;profile=mcp-app")]
    [Description("Interactive flame graph profiler visualization")]
    public static string GetFlameGraphUi() => File.ReadAllText(Path.Combine(UiDir, "flame-graph.html"));

    [McpServerResource(UriTemplate = "ui://mcp-apps-playground/feature-flags", Name = "feature-flags-ui", MimeType = "text/html;profile=mcp-app")]
    [Description("Feature flag selector with multi-select and environment support")]
    public static string GetFeatureFlagsUi() => File.ReadAllText(Path.Combine(UiDir, "feature-flags.html"));

    [McpServerResource(UriTemplate = "ui://mcp-apps-playground/database-query", Name = "database-query-ui", MimeType = "text/html;profile=mcp-app")]
    [Description("Interactive sales database query UI with filters and preview")]
    public static string GetDatabaseQueryUi() => File.ReadAllText(Path.Combine(UiDir, "database-query.html"));

    [McpServerResource(UriTemplate = "ui://mcp-apps-playground/weather-forecast", Name = "weather-forecast-ui", MimeType = "text/html;profile=mcp-app")]
    [Description("Interactive weather forecast UI with city picker")]
    public static string GetWeatherForecastUi() => File.ReadAllText(Path.Combine(UiDir, "weather-ui.html"));

    [McpServerResource(UriTemplate = "data://mcp-apps-playground/us-cities", Name = "us-cities", MimeType = "application/json")]
    [Description("List of supported US cities for weather forecasts")]
    public static string GetUsCities()
    {
        var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter<UsCity>() } };
        var cities = Enum.GetValues<UsCity>().Select(c => JsonSerializer.Serialize(c, options).Trim('"')).Order().ToList();
        return JsonSerializer.Serialize(cities);
    }

    [McpServerResource(UriTemplate = "mcp://mcp-apps-playground/docs/greeting", Name = "greeting-docs", MimeType = "text/markdown")]
    [Description("Documentation for the greeting tool")]
    public static string GetGreetingDocs() => """
        # Greeting Tool

        Use the `hello_world` tool to generate personalized greetings.

        ## Parameters
        - **name**: The name to greet
        - **showUI**: Whether to show interactive UI (default: true)

        ## Apps Extension (SEP-1865)
        When `showUI=true`, the tool returns `_meta.ui.resourceUri` pointing to
        `ui://mcp-apps-playground/greeting`. The host:

        1. Fetches the HTML template via `resources/read`
        2. Renders it in a sandboxed iframe
        3. Sends tool arguments via `ui/notifications/tool-input`
        4. Sends tool result via `ui/notifications/tool-result`

        The UI can send messages to chat via `ui/message` request.
        """;
}
