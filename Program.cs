using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;
using System.Text.Json.Nodes;

const string LogFile = "/tmp/mcp-apps-playground.log";

void Log(string message)
{
    var line = $"[{DateTime.UtcNow:O}] {message}";
    File.AppendAllText(LogFile, line + "\n");
    Console.Error.WriteLine(line);
}

// UI resource definitions
var uiDir = Path.Combine(AppContext.BaseDirectory, "ui");
var uiResources = new Dictionary<string, (string Name, string Description, string HtmlFile)>
{
    ["ui://mcp-apps-playground/greeting"] = ("greeting-ui", "Interactive greeting UI panel", "greeting.html"),
    ["ui://mcp-apps-playground/list-sort"] = ("list-sort-ui", "Interactive list sorting UI panel", "list-sort.html"),
    ["ui://mcp-apps-playground/flame-graph"] = ("flame-graph-ui", "Interactive flame graph profiler visualization", "flame-graph.html"),
    ["ui://mcp-apps-playground/feature-flags"] = ("feature-flags-ui", "Feature flag selector with multi-select and environment support", "feature-flags.html"),
    ["ui://mcp-apps-playground/database-query"] = ("database-query-ui", "Interactive sales database query UI with filters and preview", "database-query.html"),
};

// Tool definitions
var tools = new List<Tool>
{
    CreateTool("hello_world",
        "Display a Hello World greeting with optional interactive UI",
        new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string", description = "Name to greet" },
                showUI = new { type = "boolean", description = "Show interactive UI panel" }
            },
            required = new[] { "name" }
        },
        "ui://mcp-apps-playground/greeting"),

    CreateTool("list_sort",
        "Display an interactive list sorting UI. User can drag to reorder items, save the sorted order, or ask the AI to sort the list.",
        new
        {
            type = "object",
            properties = new
            {
                items = new
                {
                    type = "array",
                    description = "List of items to sort",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "Unique identifier for the item" },
                            label = new { type = "string", description = "Display label for the item" }
                        },
                        required = new[] { "id", "label" }
                    }
                },
                title = new { type = "string", description = "Optional title for the list" }
            },
            required = new[] { "items" }
        },
        "ui://mcp-apps-playground/list-sort"),

    CreateTool("flame_graph",
        "Display an interactive flame graph visualization for performance profiling. Shows call hierarchy with execution time. Click frames to zoom, analyze hot paths.",
        new
        {
            type = "object",
            properties = new
            {
                title = new { type = "string", description = "Title for the profile visualization" },
                filename = new { type = "string", description = "Source filename or profile name" },
                profile = new { type = "object", description = "Profile data (uses simulated data if not provided)" }
            }
        },
        "ui://mcp-apps-playground/flame-graph"),

    CreateTool("feature_flags",
        "Browse and select feature flags to generate SDK code. Shows flag status per environment (prod/staging/dev), rollout percentages, and tags. Multi-select flags to generate useFeatureFlag() hooks.",
        new
        {
            type = "object",
            properties = new
            {
                environment = new { type = "string", @enum = new[] { "production", "staging", "development" }, description = "Default environment to show" },
                filter = new { type = "string", description = "Filter flags by name or tag" },
                flags = new
                {
                    type = "array",
                    description = "Custom flags to display (uses sample data if not provided)",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            key = new { type = "string" },
                            description = new { type = "string" },
                            tags = new { type = "array", items = new { type = "string" } },
                            status = new
                            {
                                type = "object",
                                properties = new
                                {
                                    production = new { type = "string", @enum = new[] { "on", "off", "partial" } },
                                    staging = new { type = "string", @enum = new[] { "on", "off", "partial" } },
                                    development = new { type = "string", @enum = new[] { "on", "off", "partial" } }
                                }
                            },
                            rollout = new { type = "number", minimum = 0, maximum = 100 }
                        }
                    }
                }
            }
        },
        "ui://mcp-apps-playground/feature-flags"),

    CreateTool("database_query",
        "Query and filter sales database with interactive UI. Filter by date range, category, status, sales rep, and amount. Preview data in table format and export summaries.",
        new
        {
            type = "object",
            properties = new
            {
                dateRange = new { type = "string", @enum = new[] { "7d", "30d", "90d", "all" }, description = "Date range filter (default: 30d)" },
                category = new { type = "string", @enum = new[] { "Electronics", "Clothing", "Home", "Sports", "Books" }, description = "Product category filter" },
                status = new { type = "string", @enum = new[] { "completed", "pending", "shipped", "cancelled" }, description = "Order status filter" },
                salesRep = new { type = "string", description = "Filter by sales representative name" },
                minAmount = new { type = "number", description = "Minimum order amount filter" }
            }
        },
        "ui://mcp-apps-playground/database-query"),
};

var options = new McpServerOptions
{
    ServerInfo = new Implementation { Name = "mcp-apps-playground", Version = "1.0.0" },
    Capabilities = new ServerCapabilities
    {
        Tools = new ToolsCapability { ListChanged = true },
        Resources = new ResourcesCapability { Subscribe = true, ListChanged = true },
    },
    Handlers = new McpServerHandlers
    {
        ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = tools }),

        CallToolHandler = (request, _) =>
        {
            var name = request.Params?.Name;
            var args = request.Params?.Arguments;
            Log($"Tool {name} called with args: {JsonSerializer.Serialize(args)}");

            return ValueTask.FromResult(name switch
            {
                "hello_world" => HandleHelloWorld(args!),
                "list_sort" => HandleListSort(args!),
                "flame_graph" => HandleFlameGraph(args!),
                "feature_flags" => HandleFeatureFlags(args!),
                "database_query" => HandleDatabaseQuery(args!),
                _ => throw new McpException($"Unknown tool: {name}")
            });
        },

        ListResourcesHandler = (_, _) =>
        {
            var resources = new List<Resource>();

            foreach (var (uri, (resName, description, htmlFile)) in uiResources)
            {
                resources.Add(new Resource
                {
                    Uri = uri,
                    Name = resName,
                    MimeType = "text/html;profile=mcp-app",
                    Description = description
                });
            }

            // Add markdown docs resource
            resources.Add(new Resource
            {
                Uri = "mcp://mcp-apps-playground/docs/greeting",
                Name = "greeting-docs",
                MimeType = "text/markdown",
                Description = "Documentation for the greeting tool"
            });

            return ValueTask.FromResult(new ListResourcesResult { Resources = resources });
        },

        ReadResourceHandler = (request, _) =>
        {
            var uri = request.Params?.Uri;
            Log($"resources/read called for: {uri}");

            if (uri != null && uiResources.TryGetValue(uri, out var uiResource))
            {
                var html = File.ReadAllText(Path.Combine(uiDir, uiResource.HtmlFile));
                Log($"Returning HTML template ({html.Length} bytes)");
                return ValueTask.FromResult(new ReadResourceResult
                {
                    Contents =
                    [
                        new TextResourceContents
                        {
                            Uri = uri,
                            MimeType = "text/html;profile=mcp-app",
                            Text = html
                        }
                    ]
                });
            }

            if (uri == "mcp://mcp-apps-playground/docs/greeting")
            {
                return ValueTask.FromResult(new ReadResourceResult
                {
                    Contents =
                    [
                        new TextResourceContents
                        {
                            Uri = uri,
                            MimeType = "text/markdown",
                            Text = """
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
                                """
                        }
                    ]
                });
            }

            throw new McpException($"Resource not found: {uri}");
        }
    }
};

var transport = new StdioServerTransport("mcp-apps-playground");
await using var server = McpServer.Create(transport, options);

Log("MCP Apps Playground server running (stdio)");
Log("Apps Extension: UI resources available at ui://mcp-apps-playground/");

await server.RunAsync();

// --- Tool handlers ---

CallToolResult HandleHelloWorld(IDictionary<string, JsonElement>? args)
{
    var name = GetStringArg(args, "name", "World");
    var showUI = GetBoolArg(args, "showUI", true);
    Log($"Tool hello_world: name=\"{name}\", showUI={showUI}");

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
        Log($"Returning structuredContent for hello_world");
    }

    return result;
}

CallToolResult HandleListSort(IDictionary<string, JsonElement>? args)
{
    var items = GetJsonArg(args, "items");
    var title = GetStringArg(args, "title", "Sort List");

    var itemCount = items.ValueKind == JsonValueKind.Array ? items.GetArrayLength() : 0;
    Log($"Tool list_sort: {itemCount} items, title=\"{title}\"");

    return new CallToolResult
    {
        Content = [new TextContentBlock { Text = $"Showing {itemCount} items for sorting." }],
        StructuredContent = JsonSerializer.SerializeToElement(new
        {
            items,
            title
        })
    };
}

CallToolResult HandleFlameGraph(IDictionary<string, JsonElement>? args)
{
    var title = GetStringArg(args, "title", "Performance Profile");
    var filename = GetStringArg(args, "filename", "CPU Profile");
    var profile = args?.ContainsKey("profile") == true ? GetJsonArg(args, "profile") : (JsonElement?)null;
    Log($"Tool flame_graph: title=\"{title}\"");

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

CallToolResult HandleFeatureFlags(IDictionary<string, JsonElement>? args)
{
    var environment = GetStringArg(args, "environment", "production");
    var filter = GetStringArg(args, "filter", null);
    Log($"Tool feature_flags: env={environment}, filter={filter ?? "none"}");

    var flagData = GetSimulatedFlags();

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

CallToolResult HandleDatabaseQuery(IDictionary<string, JsonElement>? args)
{
    var dateRange = GetStringArg(args, "dateRange", "30d");
    var category = GetStringArg(args, "category", null);
    var status = GetStringArg(args, "status", null);
    var salesRep = GetStringArg(args, "salesRep", null);
    var minAmount = args?.ContainsKey("minAmount") == true ? GetDoubleArg(args, "minAmount") : (double?)null;

    Log($"Tool database_query: dateRange={dateRange}, category={category ?? "all"}, status={status ?? "all"}");

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

// --- Helper functions ---

Tool CreateTool(string name, string description, object inputSchema, string resourceUri)
{
    return new Tool
    {
        Name = name,
        Description = description,
        InputSchema = JsonSerializer.SerializeToElement(inputSchema),
        Meta = new JsonObject
        {
            ["ui"] = JsonNode.Parse(JsonSerializer.Serialize(new
            {
                resourceUri,
                visibility = new[] { "model", "app" }
            }))
        }
    };
}

string GetStringArg(IDictionary<string, JsonElement>? args, string key, string? defaultValue = null)
{
    if (args != null && args.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String)
        return el.GetString() ?? defaultValue ?? "";
    return defaultValue ?? "";
}

bool GetBoolArg(IDictionary<string, JsonElement>? args, string key, bool defaultValue = false)
{
    if (args != null && args.TryGetValue(key, out var el))
    {
        if (el.ValueKind == JsonValueKind.True) return true;
        if (el.ValueKind == JsonValueKind.False) return false;
    }
    return defaultValue;
}

double GetDoubleArg(IDictionary<string, JsonElement>? args, string key, double defaultValue = 0)
{
    if (args != null && args.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number)
        return el.GetDouble();
    return defaultValue;
}

JsonElement GetJsonArg(IDictionary<string, JsonElement>? args, string key)
{
    if (args != null && args.TryGetValue(key, out var el))
        return el;
    return JsonSerializer.SerializeToElement<object?>(null);
}

string GetStatusForEnv(FlagData flag, string env) => env switch
{
    "staging" => flag.Status.Staging,
    "development" => flag.Status.Development,
    _ => flag.Status.Production
};

// --- Data models ---

ProfileAnalysis GetSimulatedAnalysis() => new()
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

List<FlagData> GetSimulatedFlags() =>
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

// --- Records ---

record ProfileAnalysis
{
    public string Summary { get; init; } = "";
    public List<string> Findings { get; init; } = [];
    public List<HotPath> HotPaths { get; init; } = [];
    public List<string> Recommendations { get; init; } = [];
}

record HotPath(string Name, string File, int Percent);

record FlagStatus(string Production, string Staging, string Development);

record FlagData(string Key, string Description, List<string> Tags, FlagStatus Status, int Rollout);
