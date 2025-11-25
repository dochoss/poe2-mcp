# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Poe2Mcp.sln ./
COPY src/Poe2Mcp.Core/Poe2Mcp.Core.csproj src/Poe2Mcp.Core/
COPY src/Poe2Mcp.Server/Poe2Mcp.Server.csproj src/Poe2Mcp.Server/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build
RUN dotnet build -c Release --no-restore

# Publish
RUN dotnet publish src/Poe2Mcp.Server/Poe2Mcp.Server.csproj -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r poe2mcp && useradd -r -g poe2mcp poe2mcp

# Copy published application
COPY --from=build /app/publish .

# Create data directory for SQLite database
RUN mkdir -p /app/data && chown -R poe2mcp:poe2mcp /app/data

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=
ENV MCP_TRANSPORT=stdio

# Switch to non-root user
USER poe2mcp

# Entry point for MCP server (stdio transport)
ENTRYPOINT ["dotnet", "Poe2Mcp.Server.dll"]
