# PoE2 MCP Server Testing Script
# This script helps you test the MCP server and its tools

Write-Host "=== PoE2 MCP Server Test Script ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:20002/poe2mcp/mcp"

# Function to test if server is running
function Test-ServerRunning {
    Write-Host "1. Testing if MCP server is running..." -ForegroundColor Yellow
    
    # MCP servers respond to initialize method, not GET requests
    $body = @{
        jsonrpc = "2.0"
        id = 0
        method = "initialize"
        params = @{
            protocolVersion = "2024-11-05"
            capabilities = @{
                roots = @{
                    listChanged = $false
                }
                sampling = @{}
            }
            clientInfo = @{
                name = "test-client"
                version = "1.0.0"
            }
        }
    } | ConvertTo-Json -Depth 10

    try {
        $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Body $body -ContentType "application/json" -ErrorAction Stop
        Write-Host "   ? Server is running!" -ForegroundColor Green
        Write-Host "   Server: $($response.result.serverInfo.name) v$($response.result.serverInfo.version)" -ForegroundColor Gray
        return $true
    }
    catch {
        Write-Host "   ? Server is NOT running!" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Please start the server first with: dotnet run --project src\Poe2Mcp.Server" -ForegroundColor Red
        return $false
    }
}

# Function to list available tools
function Get-AvailableTools {
    Write-Host ""
    Write-Host "2. Fetching available MCP tools..." -ForegroundColor Yellow
    
    $body = @{
        jsonrpc = "2.0"
        id = 1
        method = "tools/list"
        params = @{}
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Body $body -ContentType "application/json"
        
        Write-Host "   ? Available tools:" -ForegroundColor Green
        foreach ($tool in $response.result.tools) {
            Write-Host "     - $($tool.name): $($tool.description)" -ForegroundColor White
        }
        return $response.result.tools
    }
    catch {
        Write-Host "   ? Failed to fetch tools: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Function to call analyze_character tool
function Invoke-AnalyzeCharacter {
    param(
        [string]$Account,
        [string]$Character,
        [string]$League = "Standard"
    )
    
    Write-Host ""
    Write-Host "3. Calling analyze_character tool..." -ForegroundColor Yellow
    Write-Host "   Account: $Account" -ForegroundColor Gray
    Write-Host "   Character: $Character" -ForegroundColor Gray
    Write-Host "   League: $League" -ForegroundColor Gray
    
    $body = @{
        jsonrpc = "2.0"
        id = 2
        method = "tools/call"
        params = @{
            name = "analyze_character"
            arguments = @{
                account = $Account
                character = $Character
                league = $League
            }
        }
    } | ConvertTo-Json -Depth 10

    try {
        $response = Invoke-RestMethod -Uri $baseUrl -Method POST -Body $body -ContentType "application/json"
        
        Write-Host ""
        Write-Host "   ? Tool response:" -ForegroundColor Green
        Write-Host ($response | ConvertTo-Json -Depth 10) -ForegroundColor White
        return $response
    }
    catch {
        Write-Host "   ? Failed to call tool: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "   Response: $responseBody" -ForegroundColor Red
        }
        return $null
    }
}

# Main execution
Write-Host "This script will test your MCP server at: $baseUrl" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if server is running
if (-not (Test-ServerRunning)) {
    exit 1
}

# Step 2: List available tools
$tools = Get-AvailableTools

# Step 3: Test analyze_character tool
Write-Host ""
Write-Host "--- Example Usage ---" -ForegroundColor Cyan
Write-Host ""
Write-Host "To test with your own character, run:" -ForegroundColor Yellow
Write-Host '  Invoke-AnalyzeCharacter -Account "YourAccountName" -Character "YourCharacterName" -League "Standard"' -ForegroundColor White
Write-Host ""
Write-Host "Or test with sample data:" -ForegroundColor Yellow
$result = Invoke-AnalyzeCharacter -Account "TestAccount" -Character "TestCharacter" -League "Standard"

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
