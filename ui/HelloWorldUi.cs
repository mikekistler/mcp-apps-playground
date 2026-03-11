namespace McpAppsPlayground.Ui;

public static class HelloWorldUi
{
    public static string GetHtml() => """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>Hello World</title>
          <style>
            :root {
              --vscode-bg: #1e1e1e;
              --vscode-editor-bg: #252526;
              --vscode-input-bg: #3c3c3c;
              --vscode-border: #454545;
              --vscode-text: #cccccc;
              --vscode-text-muted: #858585;
              --vscode-accent: #0078d4;
              --vscode-accent-hover: #1c8ae6;
              --vscode-success: #4ec9b0;
              --vscode-font: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            }
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body {
              font-family: var(--vscode-font);
              font-size: 13px;
              background: var(--vscode-bg);
              color: var(--vscode-text);
              padding: 12px;
            }
            .header {
              display: flex;
              align-items: center;
              gap: 8px;
              margin-bottom: 12px;
              padding-bottom: 8px;
              border-bottom: 1px solid var(--vscode-border);
            }
            .header-icon { font-size: 18px; }
            .greeting { font-size: 14px; font-weight: 600; color: var(--vscode-text); }
            .subtitle { font-size: 11px; color: var(--vscode-text-muted); margin-top: 2px; }
            .form-group { display: flex; gap: 6px; margin-bottom: 8px; }
            input {
              flex: 1; padding: 5px 8px;
              background: var(--vscode-input-bg);
              border: 1px solid var(--vscode-border);
              border-radius: 2px; color: var(--vscode-text);
              font-size: 13px; font-family: inherit; outline: none;
            }
            input:focus { border-color: var(--vscode-accent); }
            input::placeholder { color: var(--vscode-text-muted); }
            input:disabled { opacity: 0.6; }
            button {
              padding: 5px 12px; border: none; border-radius: 2px;
              font-size: 13px; font-family: inherit; cursor: pointer;
              transition: background 0.1s;
            }
            button:disabled { opacity: 0.5; cursor: not-allowed; }
            .btn-secondary {
              background: var(--vscode-input-bg); color: var(--vscode-text);
              border: 1px solid var(--vscode-border);
            }
            .btn-secondary:hover:not(:disabled) { background: #4a4a4a; }
            .btn-primary { background: var(--vscode-accent); color: white; }
            .btn-primary:hover:not(:disabled) { background: var(--vscode-accent-hover); }
            .status {
              font-size: 11px; color: var(--vscode-text-muted);
              display: flex; align-items: center; gap: 4px;
            }
            .status.success { color: var(--vscode-success); }
          </style>
        </head>
        <body>
          <div class="header">
            <span class="header-icon">👋</span>
            <div>
              <div class="greeting" id="greeting">Hello, World!</div>
              <div class="subtitle">MCP Apps Playground</div>
            </div>
          </div>
          <div class="form-group">
            <input type="text" id="nameInput" placeholder="Enter name..." value="World" disabled />
            <button class="btn-secondary" id="greetBtn" onclick="greetAgain()" disabled>Preview</button>
            <button class="btn-primary" id="submitBtn" onclick="submitForm()" disabled>Send</button>
          </div>
          <div class="status" id="status">Initializing...</div>

          <script>
            let nextRequestId = 1;
            const pendingRequests = new Map();
            let hostContext = null;
            let isInitialized = false;
            let currentName = 'World';

            function generateId() { return nextRequestId++; }

            function sendRequest(method, params) {
              const id = generateId();
              const request = { jsonrpc: '2.0', id: id, method: method, params: params || {} };
              return new Promise((resolve, reject) => {
                pendingRequests.set(id, { resolve, reject });
                window.parent.postMessage(request, '*');
              });
            }

            function sendNotification(method, params) {
              window.parent.postMessage({ jsonrpc: '2.0', method: method, params: params || {} }, '*');
            }

            window.addEventListener('message', (event) => {
              try {
                const message = event.data;
                if (!message || message.jsonrpc !== '2.0') return;
                if (message.id !== undefined && (message.result !== undefined || message.error)) {
                  const pending = pendingRequests.get(message.id);
                  if (pending) {
                    pendingRequests.delete(message.id);
                    if (message.error) pending.reject(new Error(message.error.message || 'Unknown error'));
                    else pending.resolve(message.result);
                  }
                  return;
                }
                if (message.method) handleHostNotification(message.method, message.params);
              } catch (error) { console.error('Error handling message:', error); }
            });

            function handleHostNotification(method, params) {
              switch (method) {
                case 'ui/notifications/tool-input-partial':
                  if (params?.arguments?.name) updateGreeting(params.arguments.name);
                  break;
                case 'ui/notifications/tool-input':
                  if (params?.arguments) {
                    currentName = params.arguments.name || 'World';
                    updateGreeting(currentName);
                    document.getElementById('nameInput').value = currentName;
                  }
                  updateStatus('Ready');
                  break;
                case 'ui/notifications/tool-result':
                  if (params?.structuredContent?.name) updateGreeting(params.structuredContent.name);
                  updateStatus('Tool completed', true);
                  break;
                case 'ui/notifications/tool-cancelled':
                  updateStatus('Cancelled: ' + (params?.reason || 'Unknown reason'));
                  break;
                case 'ui/notifications/host-context-changed':
                  if (params) { hostContext = { ...hostContext, ...params }; applyHostContext(); }
                  break;
              }
            }

            function applyHostContext() {
              if (!hostContext) return;
              if (hostContext.theme) document.documentElement.style.colorScheme = hostContext.theme;
              if (hostContext.styles?.variables) {
                const root = document.documentElement;
                for (const [key, value] of Object.entries(hostContext.styles.variables)) {
                  if (value) root.style.setProperty(key, value);
                }
              }
              if (hostContext.styles?.css?.fonts) {
                const style = document.createElement('style');
                style.textContent = hostContext.styles.css.fonts;
                document.head.appendChild(style);
              }
            }

            function updateGreeting(name) {
              const safeName = escapeHtml(name);
              document.getElementById('greeting').textContent = 'Hello, ' + safeName + '!';
              document.title = 'Hello ' + safeName;
            }

            function escapeHtml(text) {
              const div = document.createElement('div');
              div.textContent = text;
              return div.innerHTML;
            }

            function updateStatus(message, isSuccess = false) {
              const el = document.getElementById('status');
              el.textContent = message;
              el.className = 'status' + (isSuccess ? ' success' : '');
            }

            function enableControls() {
              document.getElementById('nameInput').disabled = false;
              document.getElementById('greetBtn').disabled = false;
              document.getElementById('submitBtn').disabled = false;
            }

            function greetAgain() {
              const name = document.getElementById('nameInput').value.trim();
              if (!name) { updateStatus('Please enter a name'); return; }
              currentName = name;
              updateGreeting(name);
              updateStatus('Preview updated');
            }

            async function submitForm() {
              const name = document.getElementById('nameInput').value.trim() || 'World';
              updateStatus('Sending...');
              document.getElementById('submitBtn').disabled = true;
              document.getElementById('nameInput').disabled = true;
              document.getElementById('greetBtn').disabled = true;
              try {
                const result = await sendRequest('ui/message', {
                  content: [{ type: 'text', text: 'User selected name: ' + name + '\n\nPlease greet them with: Hello, ' + name + '!' }]
                });
                if (result?.isError) throw new Error('Host rejected the message');
                updateStatus('Message sent', true);
                setTimeout(() => {
                  document.getElementById('submitBtn').disabled = false;
                  document.getElementById('nameInput').disabled = false;
                  document.getElementById('greetBtn').disabled = false;
                }, 1000);
              } catch (error) {
                console.error('Submit failed:', error);
                updateStatus('Error: ' + error.message);
                document.getElementById('submitBtn').disabled = false;
                document.getElementById('nameInput').disabled = false;
                document.getElementById('greetBtn').disabled = false;
              }
            }

            async function initialize() {
              try {
                updateStatus('Connecting to host...');
                const initResult = await sendRequest('ui/initialize', {
                  protocolVersion: '2025-06-18',
                  capabilities: {},
                  clientInfo: { name: 'mcp-apps-playground-greeting', version: '1.0.0' }
                });
                hostContext = initResult.hostContext || {};
                applyHostContext();
                sendNotification('ui/notifications/initialized', {});
                isInitialized = true;
                enableControls();
                updateStatus('Ready');
              } catch (error) {
                console.error('Initialization failed:', error);
                updateStatus('Standalone mode');
                enableControls();
              }
            }

            initialize();
          <\/script>
        </body>
        </html>
        """;
}
