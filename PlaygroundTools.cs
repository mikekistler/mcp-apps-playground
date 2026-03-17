using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

[McpServerToolType]
public sealed class PlaygroundTools
{
    [McpServerTool(Name = "hello_world")]
    [Description("Display a Hello World greeting with optional interactive UI")]
    public static CallToolResult HelloWorld(
        [Description("Name to greet")] string name,
        [Description("Show interactive UI panel")] bool showUI = true)
    {
        var greeting = $"Hello, {name}!";
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = greeting }]
        };

        if (showUI)
        {
            result.StructuredContent = JsonSerializer.SerializeToElement(new
            {
                name,
                greeting,
                timestamp = DateTime.UtcNow.ToString("O")
            });
        }

        return result;
    }

    [McpServerTool(Name = "list_sort")]
    [Description("Display an interactive list sorting UI. User can drag to reorder items, save the sorted order, or ask the AI to sort the list.")]
    public static CallToolResult ListSort(
        [Description("List of items to sort")] List<ListSortItem> items,
        [Description("Optional title for the list")] string? title = "Sort List")
    {
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"Showing {items.Count} items for sorting." }],
            StructuredContent = JsonSerializer.SerializeToElement(new
            {
                items,
                title
            })
        };
    }

    [McpServerTool(Name = "flame_graph")]
    [Description("Display an interactive flame graph visualization for performance profiling. Shows call hierarchy with execution time. Click frames to zoom, analyze hot paths.")]
    public static CallToolResult FlameGraph(
        [Description("Title for the profile visualization")] string? title = "Performance Profile",
        [Description("Source filename or profile name")] string? filename = "CPU Profile",
        [Description("Profile data (uses simulated data if not provided)")] JsonElement? profile = null)
    {
        var analysis = GetSimulatedAnalysis();

        var findings = string.Join("\n", analysis.Findings.Select((f, i) => $"{i + 1}. {f}"));
        var hotPaths = string.Join("\n", analysis.HotPaths.Select(h => $"- `{h.Name}` in {h.File} ({h.Percent}%)"));
        var recommendations = string.Join("\n", analysis.Recommendations.Select(r => $"- {r}"));

        var analysisText = $"""
            ## Flame Graph: {title}

            **Summary:** {analysis.Summary}

            ### Key Findings
            {findings}

            ### Hot Paths (>10% self time)
            {hotPaths}

            ### Recommendations
            {recommendations}
            """;

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = analysisText }],
            StructuredContent = JsonSerializer.SerializeToElement(new
            {
                title,
                filename,
                profile = profile.HasValue ? profile.Value : (object?)null,
                summary = analysis.Summary,
                findings = analysis.Findings,
                hotPaths = analysis.HotPaths,
                recommendations = analysis.Recommendations
            })
        };
    }

    [McpServerTool(Name = "feature_flags")]
    [Description("Browse and select feature flags to generate SDK code. Shows flag status per environment (prod/staging/dev), rollout percentages, and tags. Multi-select flags to generate useFeatureFlag() hooks.")]
    public static CallToolResult FeatureFlags(
        [Description("Default environment to show (production, staging, development)")] string? environment = "production",
        [Description("Filter flags by name or tag")] string? filter = null,
        [Description("Custom flags to display (uses sample data if not provided)")] JsonElement? flags = null)
    {
        environment ??= "production";

        var flagData = flags.HasValue
            ? JsonSerializer.Deserialize<List<FlagData>>(flags.Value.GetRawText()) ?? GetSimulatedFlags()
            : GetSimulatedFlags();

        var filteredFlags = string.IsNullOrEmpty(filter)
            ? flagData
            : flagData.Where(f => f.Key.Contains(filter) || f.Tags.Any(t => t.Contains(filter))).ToList();

        var onCount = filteredFlags.Count(f => GetStatusForEnv(f, environment) == "on");
        var partialCount = filteredFlags.Count(f => GetStatusForEnv(f, environment) == "partial");
        var offCount = filteredFlags.Count(f => GetStatusForEnv(f, environment) == "off");

        var flagLines = string.Join("\n", filteredFlags.Select(f =>
        {
            var s = GetStatusForEnv(f, environment);
            return $"- `{f.Key}`: {f.Description} ({(s == "partial" ? $"{f.Rollout}%" : s)})";
        }));

        var text = $"""
            ## Feature Flags ({environment})

            **Total:** {filteredFlags.Count} flags | {onCount} on | {partialCount} partial | {offCount} off

            ### Available Flags
            {flagLines}

            *User can select flags in the UI to generate SDK code.*
            """;

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = text }],
            StructuredContent = JsonSerializer.SerializeToElement(new
            {
                environment,
                totalFlags = filteredFlags.Count,
                summary = new { on = onCount, partial = partialCount, off = offCount },
                flags = filteredFlags
            })
        };
    }

    [McpServerTool(Name = "database_query")]
    [Description("Query and filter sales database with interactive UI. Filter by date range, category, status, sales rep, and amount. Preview data in table format and export summaries.")]
    public static CallToolResult DatabaseQuery(
        [Description("Date range filter: 7d, 30d, 90d, or all (default: 30d)")] string? dateRange = "30d",
        [Description("Product category filter: Electronics, Clothing, Home, Sports, or Books")] string? category = null,
        [Description("Order status filter: completed, pending, shipped, or cancelled")] string? status = null,
        [Description("Filter by sales representative name")] string? salesRep = null,
        [Description("Minimum order amount filter")] double? minAmount = null)
    {
        dateRange ??= "30d";

        const int totalOrders = 150;
        const int totalRevenue = 89432;
        var avgOrder = totalRevenue / totalOrders;

        var appliedFilters = new[] { dateRange, category, status, salesRep, minAmount.HasValue ? $"${minAmount}+" : null }
            .Where(f => f != null).ToArray();

        return new CallToolResult
        {
            Content = [new TextContentBlock
            {
                Text = $"## Sales Database Query\n\n" +
                       $"**Filters applied:** {(appliedFilters.Length > 0 ? string.Join(" | ", appliedFilters) : "None")}\n\n" +
                       $"**Summary:** {totalOrders} orders | ${totalRevenue:N0} revenue | ${avgOrder} avg order\n\n" +
                       "*Use the interactive UI to explore data, apply filters, and export results.*"
            }],
            StructuredContent = JsonSerializer.SerializeToElement(new
            {
                filters = new
                {
                    dateRange,
                    category,
                    status,
                    salesRep,
                    minAmount
                },
                summary = new
                {
                    totalOrders,
                    totalRevenue,
                    avgOrder
                }
            })
        };
    }

    [McpServerTool(Name = "weather_forecast")]
    [Description("Get weather forecast for a US city. Returns detailed multi-period forecast from the National Weather Service.")]
    public static async Task<CallToolResult> WeatherForecast(
        HttpClient client,
        [Description("US city to get the forecast for")] UsCity cityState)
    {
        var (latitude, longitude) = UsCityData.GetCoordinates(cityState);
        var pointUrl = string.Create(CultureInfo.InvariantCulture, $"/points/{latitude},{longitude}");
        using var locationDocument = await client.ReadJsonDocumentAsync(pointUrl);
        var forecastUrl = locationDocument.RootElement.GetProperty("properties").GetProperty("forecast").GetString()
            ?? throw new McpException($"No forecast URL provided by {client.BaseAddress}points/{latitude},{longitude}");

        using var forecastDocument = await client.ReadJsonDocumentAsync(forecastUrl);
        var periods = forecastDocument.RootElement.GetProperty("properties").GetProperty("periods").EnumerateArray().ToList();

        var structuredPeriods = periods.Select(period => new
        {
            name = period.GetProperty("name").GetString(),
            temperature = period.GetProperty("temperature").GetInt32(),
            temperatureUnit = period.GetProperty("temperatureUnit").GetString(),
            windSpeed = period.GetProperty("windSpeed").GetString(),
            windDirection = period.GetProperty("windDirection").GetString(),
            shortForecast = period.GetProperty("shortForecast").GetString(),
            detailedForecast = period.GetProperty("detailedForecast").GetString(),
            isDaytime = period.GetProperty("isDaytime").GetBoolean()
        }).ToList();

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"Weather forecast for {cityState} displayed in UI." }],
            StructuredContent = JsonSerializer.SerializeToElement(new
            {
                cityState = cityState.ToString(),
                latitude,
                longitude,
                periods = structuredPeriods
            })
        };
    }

    // --- Private helpers ---

    private static string GetStatusForEnv(FlagData flag, string env) => env switch
    {
        "staging" => flag.Status.Staging,
        "development" => flag.Status.Development,
        _ => flag.Status.Production
    };

    internal static ProfileAnalysis GetSimulatedAnalysis() => new()
    {
        Summary = "Node.js API server profile showing 3.85s execution across 58 stack frames. Full request lifecycle including auth, business logic, webhooks, scheduled jobs, event loop, and GC.",
        Findings =
        [
            "Database operations dominate at 42% total time (prisma + pg queries across 4 call sites)",
            "Network I/O (fetch for webhooks + TLS) adds 320ms latency with crypto overhead",
            "GC pressure visible: 350ms in garbage collection (9% of total) - consider memory optimization",
            "Event loop processing shows 520ms overhead including microtask draining",
            "JWT verification is a hot synchronous operation at 180ms self time",
            "JSON parsing/stringifying appears in multiple paths totaling ~175ms"
        ],
        HotPaths =
        [
            new HotPath("fetch", "[native]", 6),
            new HotPath("jwt.verify", "node_modules/jsonwebtoken/verify.js:45", 5),
            new HotPath("prisma.query", "node_modules/@prisma/client/runtime.js:1234", 5),
            new HotPath("gc", "[native]", 5),
            new HotPath("prisma.deleteMany", "node_modules/@prisma/client/runtime.js:3456", 4),
            new HotPath("calculateTotals", "src/services/orders.ts:234", 4),
            new HotPath("JSON.parse", "[native]", 3)
        ],
        Recommendations =
        [
            "**High impact:** Batch database queries - 4 separate pg.query calls could potentially be combined",
            "**Medium impact:** Cache JWT verification results for repeat tokens within request window",
            "**Medium impact:** Move webhook delivery to async background queue to reduce request latency by 320ms",
            "**Medium impact:** Investigate GC pressure - 9% in garbage collection suggests allocation hotspots",
            "**Low impact:** Consider streaming JSON parsing for large payloads",
            "**Investigate:** Event loop is processing 520ms of callbacks - check for blocking operations",
            "**User code focus:** `calculateTotals` at 140ms self time is the hottest user function - profile for optimization opportunities"
        ]
    };

    internal static List<FlagData> GetSimulatedFlags() =>
    [
        new("checkout-v2", "New checkout flow with saved cards", ["experiment", "payments"], new("partial", "on", "on"), 25),
        new("dark-mode", "Enable dark mode toggle in settings", ["rollout"], new("on", "on", "on"), 100),
        new("ai-suggestions", "ML-powered product recommendations", ["experiment", "ml"], new("off", "on", "on"), 0),
        new("rate-limit-v2", "Adaptive rate limiting algorithm", ["ops"], new("partial", "on", "on"), 50),
        new("graphql-federation", "Enable federated GraphQL gateway", ["ops", "api"], new("off", "partial", "on"), 0),
        new("social-login", "OAuth with Google, GitHub, Apple", ["rollout", "auth"], new("on", "on", "on"), 100),
        new("realtime-collab", "WebSocket-based real-time editing", ["experiment"], new("off", "off", "on"), 0),
        new("cdn-next", "Next-gen CDN with edge compute", ["ops", "infra"], new("partial", "on", "on"), 10),
        new("password-strength", "Enhanced password requirements", ["rollout", "auth"], new("on", "on", "on"), 100),
        new("analytics-v3", "Event streaming analytics pipeline", ["experiment", "data"], new("off", "partial", "on"), 0),
        new("user-segments", "Dynamic user segmentation engine", ["experiment", "ml"], new("partial", "on", "on"), 15),
        new("webhook-retry", "Exponential backoff for webhooks", ["ops"], new("on", "on", "on"), 100),
    ];
}

// --- Records ---

public record ListSortItem(
    [property: Description("Unique identifier for the item")] string Id,
    [property: Description("Display label for the item")] string Label);

public record ProfileAnalysis
{
    public string Summary { get; init; } = "";
    public List<string> Findings { get; init; } = [];
    public List<HotPath> HotPaths { get; init; } = [];
    public List<string> Recommendations { get; init; } = [];
}

public record HotPath(string Name, string File, int Percent);

public record FlagStatus(string Production, string Staging, string Development);

public record FlagData(string Key, string Description, List<string> Tags, FlagStatus Status, int Rollout);
