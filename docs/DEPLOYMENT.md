# Deployment Guide

This guide covers deploying the PoE2 Build Optimizer MCP server in various environments.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for local builds)
- [Docker](https://www.docker.com/) (for containerized deployment)
- Git

## Deployment Options

### 1. Local Development

```bash
# Clone the repository
git clone https://github.com/dochoss/poe2-mcp.git
cd poe2-mcp

# Restore and build
dotnet restore
dotnet build

# Run the server
cd src/Poe2Mcp.Server
dotnet run
```

### 2. Production Build

```bash
# Build release version
dotnet publish src/Poe2Mcp.Server -c Release -o ./publish

# Run the published application
cd publish
dotnet Poe2Mcp.Server.dll
```

### 3. Docker Deployment

#### Quick Start with Docker

```bash
# Build the Docker image
docker build -t poe2-mcp-server .

# Run with Docker
docker run -it --name poe2-mcp \
  -v poe2mcp-data:/app/data \
  -e AI__ApiKey=your-api-key-here \
  poe2-mcp-server
```

#### Using Docker Compose

1. Create an `.env` file with your settings:

```bash
AI_API_KEY=your-anthropic-api-key
AI_MODEL=claude-3-5-sonnet-20241022
```

2. Start the service:

```bash
docker-compose up -d
```

3. View logs:

```bash
docker-compose logs -f
```

4. Stop the service:

```bash
docker-compose down
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `AI__ApiKey` | Anthropic API key for AI features | `null` |
| `AI__Model` | AI model to use | `claude-3-5-sonnet-20241022` |
| `AI__MaxTokens` | Maximum tokens for AI responses | `1024` |
| `AI__Temperature` | AI response temperature | `0.7` |
| `Database__ConnectionString` | SQLite database path | `Data Source=poe2_optimizer.db` |
| `Cache__MemoryCacheExpirationMinutes` | In-memory cache TTL | `30` |
| `Cache__SqliteCacheExpirationMinutes` | SQLite cache TTL | `1440` |

### Configuration Files

- `appsettings.json` - Main configuration
- `appsettings.Development.json` - Development overrides (git-ignored)
- `appsettings.docker.json` - Docker-specific configuration

Example `appsettings.Development.json`:

```json
{
  "AI": {
    "ApiKey": "sk-ant-your-api-key-here"
  },
  "Trade": {
    "SessionId": "your-poe-session-id"
  }
}
```

## MCP Client Configuration

### Claude Desktop

Add to your Claude Desktop configuration (`claude_desktop_config.json`):

**Local Installation:**

```json
{
  "mcpServers": {
    "poe2-optimizer": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/poe2-mcp/src/Poe2Mcp.Server"],
      "env": {
        "AI__ApiKey": "your-api-key"
      }
    }
  }
}
```

**Published Application:**

```json
{
  "mcpServers": {
    "poe2-optimizer": {
      "command": "dotnet",
      "args": ["/path/to/publish/Poe2Mcp.Server.dll"],
      "env": {
        "AI__ApiKey": "your-api-key"
      }
    }
  }
}
```

**Docker:**

```json
{
  "mcpServers": {
    "poe2-optimizer": {
      "command": "docker",
      "args": ["run", "-i", "--rm", "-e", "AI__ApiKey=your-api-key", "poe2-mcp-server"]
    }
  }
}
```

### VS Code with MCP Extension

Add to your MCP extension settings:

```json
{
  "mcp.servers": {
    "poe2-optimizer": {
      "command": "dotnet",
      "args": ["run", "--project", "${workspaceFolder}/src/Poe2Mcp.Server"]
    }
  }
}
```

## Health Checks

The server includes a `health_check` tool that verifies:

- Server status
- Database connectivity
- Cache service status
- API client availability

Call it via MCP:

```
Use the health_check tool to verify the server is running correctly.
```

## Troubleshooting

### Common Issues

**Server won't start:**
- Verify .NET 10 SDK is installed: `dotnet --version`
- Check for port conflicts
- Review logs for detailed error messages

**Database errors:**
- Ensure the data directory exists and is writable
- Check SQLite database path in configuration
- Run migrations if needed

**AI features not working:**
- Verify AI__ApiKey is set correctly
- Check API key permissions
- Review rate limits

**Cache issues:**
- Clear the cache using the `clear_cache` tool
- Check disk space for SQLite cache

### Logging

Increase log verbosity for debugging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Poe2Mcp": "Trace"
    }
  }
}
```

### Getting Help

1. Check the [README.md](../README.md) for basic usage
2. Review [CONVERSION_PLAN.md](../CONVERSION_PLAN.md) for feature status
3. Open an issue on GitHub for bugs or feature requests

## Security Considerations

1. **API Keys**: Never commit API keys to source control. Use environment variables or `appsettings.Development.json` (git-ignored).

2. **Database**: The SQLite database may contain cached character data. Secure the data directory appropriately.

3. **Docker**: The container runs as a non-root user for security. Don't override this unless necessary.

4. **Network**: The MCP server uses stdio transport by default and doesn't expose network ports.

## Performance Tuning

### Memory

For systems with limited memory, adjust cache settings:

```json
{
  "Cache": {
    "MemoryCacheExpirationMinutes": 15,
    "MaxMemoryCacheEntries": 100
  }
}
```

### Database

For better SQLite performance:

```json
{
  "Database": {
    "ConnectionString": "Data Source=poe2_optimizer.db;Cache=Shared;Mode=ReadWriteCreate"
  }
}
```

### Rate Limiting

Adjust rate limits based on your API access:

```json
{
  "RateLimit": {
    "PoeApiRequestsPerSecond": 5,
    "PoeNinjaRequestsPerSecond": 3,
    "TradeApiRequestsPerSecond": 3
  }
}
```

## Upgrading

1. Pull latest changes:
   ```bash
   git pull origin main
   ```

2. Restore packages:
   ```bash
   dotnet restore
   ```

3. Build:
   ```bash
   dotnet build
   ```

4. If using Docker:
   ```bash
   docker-compose build
   docker-compose up -d
   ```

---

For more information, see the main [README.md](../README.md) and [API Documentation](./API.md).
