# AI Service Configuration

This document explains how to configure AI services for the PoE2 Build Optimizer MCP Server.

## Supported AI Providers

### Current Support
- **Ollama** (Local development) ? Fully supported

### Future Support
- **Azure OpenAI** (Production) - Requires additional NuGet package
- **OpenAI** (Cloud) - Requires additional NuGet package
- **Anthropic Claude** (via proxy) - Requires additional implementation

## Configuration

AI services are configured via the `AI` section in `appsettings.json`:

```json
{
  "AI": {
    "ApiKey": null,
    "Endpoint": "http://localhost:11434",
    "Model": "phi3:mini",
    "MaxTokens": 2048,
    "Temperature": 0.7,
    "RequestTimeoutSeconds": 60
  }
}
```

### Configuration Options

| Property | Description | Default | Required |
|----------|-------------|---------|----------|
| `ApiKey` | API key for cloud services (not needed for Ollama) | `null` | No |
| `Endpoint` | Service endpoint URL | `null` | Yes |
| `Model` | Model name/deployment name | `"gpt-oss-120b"` | Yes |
| `MaxTokens` | Maximum tokens in response (0 = unlimited) | `0` | No |
| `Temperature` | Response creativity (0.0-1.0) | `0.7` | No |
| `RequestTimeoutSeconds` | HTTP request timeout | `30` | No |

## Ollama Setup (Local Development)

### 1. Install Ollama

Download and install Ollama from [https://ollama.ai/](https://ollama.ai/)

### 2. Start Ollama Server

```bash
ollama serve
```

### 3. Pull a Model

```bash
# Lightweight model (recommended for development)
ollama pull phi3:mini

# Or a larger model
ollama pull llama2
ollama pull codellama
```

### 4. Update appsettings.Development.json

```json
{
  "AI": {
    "Endpoint": "http://localhost:11434",
    "Model": "phi3:mini",
    "MaxTokens": 2048,
    "Temperature": 0.7
  }
}
```

### 5. Run the Server

```bash
dotnet run --project src/Poe2Mcp.Server
```

## Azure OpenAI Setup (Production)

### Prerequisites

1. Install additional NuGet package:
   ```bash
   dotnet add package Microsoft.Extensions.AI.OpenAI --version 10.0.1-preview.1.25571.5
   ```

2. Update `AiServiceConfiguration.cs` to add Azure OpenAI support (see [Microsoft.Extensions.AI documentation](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-chat-app))

### Configuration

```json
{
  "AI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "Model": "gpt-4o-mini",
    "MaxTokens": 4096,
    "Temperature": 0.7
  }
}
```

### Authentication Options

**Option 1: API Key** (Development)
```json
{
  "AI": {
    "ApiKey": "your-api-key-here",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "Model": "gpt-4o-mini"
  }
}
```

**Option 2: Managed Identity** (Production - Recommended)
```json
{
  "AI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "Model": "gpt-4o-mini"
  }
}
```

When `ApiKey` is null, the system will use `DefaultAzureCredential` which supports:
- Azure CLI (`az login`)
- Azure PowerShell
- Managed Identity (when deployed to Azure)
- Visual Studio
- VS Code Azure Account

## OpenAI Setup (Cloud)

### Prerequisites

Same as Azure OpenAI - requires additional NuGet package.

### Configuration

```json
{
  "AI": {
    "ApiKey": "sk-your-openai-api-key",
    "Endpoint": "https://api.openai.com/v1",
    "Model": "gpt-4-turbo",
    "MaxTokens": 4096,
    "Temperature": 0.7
  }
}
```

## Disabling AI Features

To disable AI features entirely, simply omit the `Endpoint` configuration:

```json
{
  "AI": {
    "Endpoint": null
  }
}
```

Or remove the AI section completely. The system will gracefully disable AI features and log a warning.

## Environment-Specific Configuration

Use different configuration files for different environments:

- **Development**: `appsettings.Development.json` (Ollama - local)
- **Production**: `appsettings.Production.json` (Azure OpenAI - cloud)

Example development configuration:
```json
{
  "AI": {
    "Endpoint": "http://localhost:11434",
    "Model": "phi3:mini"
  }
}
```

Example production configuration:
```json
{
  "AI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "Model": "gpt-4o-mini"
  }
}
```

## Testing AI Integration

### Check if AI is configured:

```csharp
var chatClient = serviceProvider.GetService<IChatClient>();
if (chatClient != null)
{
    Console.WriteLine("AI is configured and ready");
}
else
{
    Console.WriteLine("AI is not configured");
}
```

### Send a test query:

```csharp
var queryHandler = serviceProvider.GetRequiredService<IQueryHandler>();
var response = await queryHandler.HandleQueryAsync("What is freeze in PoE2?");
Console.WriteLine(response);
```

## Troubleshooting

### "AI endpoint not configured"
- Check that `AI:Endpoint` is set in `appsettings.json`
- Verify the endpoint URL is correct

### "Cannot connect to Ollama"
- Ensure Ollama is running (`ollama serve`)
- Check that the endpoint is `http://localhost:11434`
- Verify no firewall is blocking port 11434

### "Model not found"
- Pull the model first: `ollama pull phi3:mini`
- Check available models: `ollama list`
- Verify the model name in configuration matches exactly

### "Azure OpenAI authentication failed"
- For API key auth: Verify the key is correct
- For managed identity: Ensure you're signed in to Azure CLI (`az login`)
- Check that you have the correct RBAC role (Azure AI Developer)

## Recommended Models

| Provider | Model | Use Case | Context Length |
|----------|-------|----------|----------------|
| Ollama | `phi3:mini` | Development, Fast responses | 4K tokens |
| Ollama | `llama2` | Better quality, Slower | 4K tokens |
| Azure OpenAI | `gpt-4o-mini` | Production, Cost-effective | 128K tokens |
| Azure OpenAI | `gpt-4o` | Production, Best quality | 128K tokens |
| OpenAI | `gpt-4-turbo` | Cloud, Best quality | 128K tokens |

## Security Best Practices

1. **Never commit API keys** to source control
2. Use **environment variables** or **Azure Key Vault** for production
3. Use **Managed Identity** when deploying to Azure
4. Set appropriate **MaxTokens** to control costs
5. Monitor **usage and costs** in Azure Portal

## Additional Resources

- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/)
- [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Ollama Documentation](https://ollama.ai/docs)
- [Path of Exile 2 Build Optimizer](../../README.md)
