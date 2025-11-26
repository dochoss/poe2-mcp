# Testing PoE2 MCP Server

This guide shows you how to test the MCP server and call its tools.

## Prerequisites

1. The MCP server must be running on `http://localhost:20002/poe2mcp`

## Starting the Server

Open a terminal and run:

```powershell
dotnet run --project src\Poe2Mcp.Server
```

You should see:
```
Listening on http://localhost:20002/poe2mcp
```

## Testing Methods

### Method 1: Using PowerShell Script (Easiest)

We've created a test script for you. Run:

```powershell
.\test-mcp-server.ps1
```

This will:
1. Check if the server is running
2. List all available MCP tools
3. Demonstrate calling the `analyze_character` tool

To test with your own character:

```powershell
# Load the script functions
. .\test-mcp-server.ps1

# Call with your data
Invoke-AnalyzeCharacter -Account "YourAccountName" -Character "YourCharacterName" -League "Standard"
```

### Method 2: Using curl

#### Initialize MCP Session

```bash
curl -X POST http://localhost:20002/poe2mcp/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 0,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {
        "roots": { "listChanged": false },
        "sampling": {}
      },
      "clientInfo": {
        "name": "test-client",
        "version": "1.0.0"
      }
    }
  }'
```

#### List Available Tools

```bash
curl -X POST http://localhost:20002/poe2mcp/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/list",
    "params": {}
  }'
```

#### Call analyze_character Tool

```bash
curl -X POST http://localhost:20002/poe2mcp/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "analyze_character",
      "arguments": {
        "account": "YourAccountName",
        "character": "YourCharacterName",
        "league": "Standard"
      }
    }
  }'
```

### Method 3: Using PowerShell Invoke-RestMethod

#### Initialize MCP Session

```powershell
$body = @{
    jsonrpc = "2.0"
    id = 0
    method = "initialize"
    params = @{
        protocolVersion = "2024-11-05"
        capabilities = @{
            roots = @{ listChanged = $false }
            sampling = @{}
        }
        clientInfo = @{
            name = "test-client"
            version = "1.0.0"
        }
    }
} | ConvertTo-Json -Depth 10

Invoke-RestMethod -Uri "http://localhost:20002/poe2mcp/mcp" `
                  -Method POST `
                  -Body $body `
                  -ContentType "application/json"
```

#### List Available Tools

```powershell
$body = @{
    jsonrpc = "2.0"
    id = 1
    method = "tools/list"
    params = @{}
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:20002/poe2mcp/mcp" `
                  -Method POST `
                  -Body $body `
                  -ContentType "application/json"
```

#### Call analyze_character Tool

```powershell
$body = @{
    jsonrpc = "2.0"
    id = 2
    method = "tools/call"
    params = @{
        name = "analyze_character"
        arguments = @{
            account = "YourAccountName"
            character = "YourCharacterName"
            league = "Standard"
        }
    }
} | ConvertTo-Json -Depth 10

Invoke-RestMethod -Uri "http://localhost:20002/poe2mcp/mcp" `
                  -Method POST `
                  -Body $body `
                  -ContentType "application/json"
```

## MCP Protocol Basics

The Model Context Protocol (MCP) uses JSON-RPC 2.0 over HTTP POST with **Streamable HTTP** transport. Every request has:

- `jsonrpc`: Always "2.0"
- `id`: A unique identifier for the request
- `method`: The MCP method to call
- `params`: Parameters for the method

### MCP Connection Flow

1. **Initialize** - Client sends `initialize` request to establish protocol version and capabilities
2. **List Tools** - Client requests available tools with `tools/list`
3. **Call Tool** - Client invokes a tool with `tools/call`

### Common MCP Methods

- `initialize` - Initialize MCP session (required first step)
- `tools/list` - List all available tools
- `tools/call` - Call a specific tool
- `prompts/list` - List all available prompts
- `resources/list` - List all available resources

### Example Initialize Request

```powershell
$body = @{
    jsonrpc = "2.0"
    id = 0
    method = "initialize"
    params = @{
        protocolVersion = "2024-11-05"
        capabilities = @{
            roots = @{ listChanged = $false }
            sampling = @{}
        }
        clientInfo = @{
            name = "test-client"
            version = "1.0.0"
        }
    }
} | ConvertTo-Json -Depth 10

Invoke-RestMethod -Uri "http://localhost:20002/poe2mcp/mcp" `
                  -Method POST `
                  -Body $body `
                  -ContentType "application/json"
```

## Available Tools

Based on your codebase, these tools are available:

1. **analyze_character** - Analyze a PoE2 character's build
   - Parameters: `account`, `character`, `league`

2. **compare_to_top_players** - Compare character to ladder leaders
   - Parameters: `account`, `character`, `league`, `topN`

## Expected Response (Current Implementation)

Since the `analyze_character` tool is currently a placeholder, you'll get:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "success": true,
    "account": "YourAccountName",
    "character": "YourCharacterName",
    "league": "Standard",
    "message": "Character analysis tool registered. Full implementation coming soon."
  }
}
```

## Next Steps

To make the tool actually analyze characters, you'll need to implement the full functionality in `src\Poe2Mcp.Server\Tools\CharacterTools.cs` to:

1. Call `IPoeApiClient.GetCharacterAsync()`
2. Use the calculators (EHP, damage, etc.)
3. Use the analyzers (weakness detection, build scoring, etc.)
4. Return comprehensive analysis results

## Troubleshooting

### Server not responding
- Check that the server is running: `dotnet run --project src\Poe2Mcp.Server`
- Verify the URL is correct: `http://localhost:20002/poe2mcp/mcp`

### Connection refused
- Make sure port 20002 is not blocked by firewall
- Check if another application is using port 20002

### JSON-RPC errors
- Ensure the request body is valid JSON
- Check that all required parameters are included
- Verify the tool name matches exactly (case-sensitive)
