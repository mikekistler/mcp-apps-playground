# MCP Apps Playground

A demo MCP server showcasing interactive UI capabilities using the [MCP Apps Extension (SEP-1865)](https://github.com/modelcontextprotocol/ext-apps).

## Features

- 🔧 **MCP Tools** - `hello_world`, `list_sort`, `flame_graph`, `feature_flags`, and `database_query` tools
- 📱 **Apps Extension** - HTML UI via `ui://` resources with `text/html;profile=mcp-app`
- 📦 **structuredContent** - Data passed to UI via `ui/notifications/tool-input`
- 💬 **Bidirectional** - UIs can send messages back to chat via `ui/message`
- 🚀 **stdio & HTTP Transport** — `--http` flag for HTTP transport

## Tools

### `list_sort` — Interactive List Reordering

**Before:** Agent receives list data from an MCP tool → proposes a sorted order based on its analysis → user reads text output and requests adjustments → multiple back-and-forth messages to align with actual preferences.

**With MCP Apps:** Agent displays a drag-and-drop interface alongside its suggested order. User applies domain knowledge to reorder items visually, or clicks "Ask AI to Sort" for the agent's reasoning—true collaboration where both contribute.

> 🖱️ Drag-and-drop reordering · 🤖 "Ask AI to Sort" · ↩️ Reset · 💾 Save to chat

---

### `flame_graph` — Performance Profiler Visualization

**Before:** Agent receives CPU profile data from an MCP tool → analyzes the JSON and identifies bottlenecks → user sees only the agent's text summary → no way to validate hypotheses or apply domain-specific context.

**With MCP Apps:** Agent renders an interactive flame graph and can annotate suspected hot paths. User explores the visualization with their own domain knowledge—confirming or rejecting the agent's hypotheses, drilling into areas the agent might have overlooked.

> 🔍 Click-to-zoom hierarchy · 💬 Hover tooltips · 🧭 Breadcrumb nav · 📊 Send frame to chat

---

### `feature_flags` — Feature Flag Selector

**Before:** Agent fetches flag configuration from an MCP tool → summarizes which flags exist and their status → user cross-references with deployment context → asks agent to generate integration code separately.

**With MCP Apps:** Agent displays a searchable flag picker with live environment status. User selects flags based on their release priorities, switches between prod/staging/dev views, and generates SDK code—agent provides data, user drives decisions.

> 🌍 Environment tabs · 🔎 Search & filter · ☑️ Multi-select · 📝 Generate SDK code

## Quick Start

```bash
# Build
dotnet build

# Run with stdio transport (for Claude Desktop, Cursor, VS Code)
dotnet run

# Run with HTTP transport
dotnet run -- --http
```

## Project Structure

```
Program.cs              # Server entry point — DI setup, transport selection
PlaygroundTools.cs      # Tool definitions with [McpServerTool] attributes
PlaygroundResources.cs  # Resource definitions with [McpServerResource] attributes
McpAppsPlayground.csproj # Project file
ui/
├── greeting.html       # Greeting UI template
├── list-sort.html      # Interactive list sorting UI
├── flame-graph.html    # Performance flame graph visualization
├── feature-flags.html  # Feature flag selector UI
└── database-query.html # Sales database query UI
```

## MCP Configuration

### VS Code

Update `.vscode/mcp.json`:

```json
{
  "servers": {
    "mcp-apps-playground": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "${workspaceFolder}"]
    }
  }
}
```

### Claude Desktop / Cursor

```json
{
  "mcpServers": {
    "mcp-apps-playground": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/mcp-apps-playground"]
    }
  }
}
```

## How It Works

### 1. UI Resource Declaration

UI resources are declared as methods on a class annotated with `[McpServerResourceType]`. Each method uses `[McpServerResource]` with a `ui://` URI template and `text/html;profile=mcp-app` MIME type:

```csharp
[McpServerResourceType]
public sealed class PlaygroundResources
{
    [McpServerResource(
        UriTemplate = "ui://mcp-apps-playground/greeting",
        Name = "greeting-ui",
        MimeType = "text/html;profile=mcp-app")]
    [Description("Interactive greeting UI panel")]
    public static string GetGreetingUi()
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ui", "greeting.html"));
}
```

### 2. Tool with UI Annotation

Tools are defined as methods with `[McpServerTool]` attributes. Data is passed to the UI via `StructuredContent`. The `_meta.ui` annotation linking each tool to its UI resource is injected via `AddListToolsFilter`:

```csharp
[McpServerToolType]
public sealed class PlaygroundTools
{
    [McpServerTool(Name = "hello_world")]
    [Description("Display a Hello World greeting")]
    public static CallToolResult HelloWorld(
        [Description("Name to greet")] string name)
    {
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"Hello, {name}!" }],
            StructuredContent = JsonSerializer.SerializeToElement(new { name })
        };
    }
}
```

### 3. UI Communication

UIs communicate with the MCP host via postMessage JSON-RPC:

```javascript
// Initialize handshake (required)
const result = await sendRequest('ui/initialize', {
  protocolVersion: '2025-06-18',
  capabilities: {},
});
sendNotification('ui/notifications/initialized', {});

// Listen for tool data
window.addEventListener('message', (e) => {
  if (e.data.method === 'ui/notifications/tool-input') {
    const { arguments: args } = e.data.params;
    // Update UI with args
  }
});

// Send message to chat
await sendRequest('ui/message', {
  content: [{ type: 'text', text: 'User selected: ...' }]
});
```

## Resources

- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [MCP Apps Extension](https://github.com/modelcontextprotocol/ext-apps)
- [MCP Specification](https://spec.modelcontextprotocol.io)
- [MCP Inspector](https://github.com/modelcontextprotocol/inspector)

## License

MIT
