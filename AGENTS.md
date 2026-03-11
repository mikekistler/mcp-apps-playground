# MCP Apps Playground

Example MCP server with interactive HTML UIs. For MCP Apps development guidance, see the **mcp-apps skill**.

## Project Structure

```
Program.cs              # Server entry point — DI setup, transport selection
PlaygroundTools.cs      # Tool definitions with [McpServerTool] attributes
PlaygroundResources.cs  # Resource definitions with [McpServerResource] attributes
ui/*.html               # HTML UI templates (loaded at runtime)
```