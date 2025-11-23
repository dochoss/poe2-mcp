using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Poe2Mcp.Core;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Multi-tier cache manager
/// L1: In-memory (fastest, limited size)
/// L3: SQLite (persistent, slower)
/// Note: L2 Redis is optional and not implemented in this version
/// </summary>
public class CacheService : ICacheService, IAsyncDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheService> _logger;
    private readonly CacheOptions _options;
    private readonly SemaphoreSlim _sqliteLock = new(1, 1);
    private SqliteConnection? _sqliteConnection;
    private readonly string _sqlitePath;
    private bool _isInitialized;

    public CacheService(
        IMemoryCache memoryCache,
        ILogger<CacheService> logger,
        IOptions<CacheOptions> options)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // Create cache directory if it doesn't exist
        var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), "data", "cache");
        Directory.CreateDirectory(cacheDir);
        _sqlitePath = Path.Combine(cacheDir, "cache.db");
    }

    /// <summary>
    /// Initialize the cache service (must be called before use)
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        try
        {
            await _sqliteLock.WaitAsync(cancellationToken);
            try
            {
                _sqliteConnection = new SqliteConnection($"Data Source={_sqlitePath}");
                await _sqliteConnection.OpenAsync(cancellationToken);
                await InitializeSqliteSchemaAsync(cancellationToken);
                _isInitialized = true;
                _logger.LogInformation("Cache service initialized with SQLite at {Path}", _sqlitePath);
            }
            finally
            {
                _sqliteLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize cache service");
            throw;
        }
    }

    private async Task InitializeSqliteSchemaAsync(CancellationToken cancellationToken)
    {
        if (_sqliteConnection == null)
            throw new InvalidOperationException("SQLite connection not initialized");

        var createTableCommand = _sqliteConnection.CreateCommand();
        createTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS cache (
                key TEXT PRIMARY KEY,
                value TEXT,
                expires_at TEXT,
                created_at TEXT DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_expires_at ON cache(expires_at);
        ";
        await createTableCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // Try L1: Memory cache
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("L1 cache hit: {Key}", key);
            return cachedValue;
        }

        // Try L3: SQLite cache
        if (_isInitialized && _sqliteConnection != null)
        {
            await _sqliteLock.WaitAsync(cancellationToken);
            try
            {
                var command = _sqliteConnection.CreateCommand();
                command.CommandText = "SELECT value, expires_at FROM cache WHERE key = $key";
                command.Parameters.AddWithValue("$key", key);

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var valueJson = reader.GetString(0);
                    var expiresAtStr = reader.GetString(1);
                    var expiresAt = DateTime.Parse(expiresAtStr);

                    if (DateTime.UtcNow < expiresAt)
                    {
                        _logger.LogDebug("L3 cache hit: {Key}", key);
                        var value = JsonSerializer.Deserialize<T>(valueJson);
                        
                        if (value != null)
                        {
                            // Store in L1 for next time (5 min TTL in L1)
                            var l1Options = new MemoryCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                            };
                            _memoryCache.Set(key, value, l1Options);
                        }
                        
                        return value;
                    }
                    else
                    {
                        // Expired, delete from SQLite
                        var deleteCommand = _sqliteConnection.CreateCommand();
                        deleteCommand.CommandText = "DELETE FROM cache WHERE key = $key";
                        deleteCommand.Parameters.AddWithValue("$key", key);
                        await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SQLite get error for key {Key}", key);
            }
            finally
            {
                _sqliteLock.Release();
            }
        }

        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var expiresAt = DateTime.UtcNow.Add(ttl);

        // Set in L1: Memory cache
        var memoryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt
        };
        _memoryCache.Set(key, value, memoryOptions);

        // Set in L3: SQLite cache
        if (_isInitialized && _sqliteConnection != null)
        {
            await _sqliteLock.WaitAsync(cancellationToken);
            try
            {
                var valueJson = JsonSerializer.Serialize(value);
                var command = _sqliteConnection.CreateCommand();
                command.CommandText = @"
                    INSERT OR REPLACE INTO cache (key, value, expires_at) 
                    VALUES ($key, $value, $expiresAt)
                ";
                command.Parameters.AddWithValue("$key", key);
                command.Parameters.AddWithValue("$value", valueJson);
                command.Parameters.AddWithValue("$expiresAt", expiresAt.ToString("o"));
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SQLite set error for key {Key}", key);
            }
            finally
            {
                _sqliteLock.Release();
            }
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // Delete from L1
        _memoryCache.Remove(key);

        // Delete from L3
        if (_isInitialized && _sqliteConnection != null)
        {
            await _sqliteLock.WaitAsync(cancellationToken);
            try
            {
                var command = _sqliteConnection.CreateCommand();
                command.CommandText = "DELETE FROM cache WHERE key = $key";
                command.Parameters.AddWithValue("$key", key);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SQLite delete error for key {Key}", key);
            }
            finally
            {
                _sqliteLock.Release();
            }
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        // Clear L1 - IMemoryCache doesn't have a Clear method, so we'll rely on expiration
        // For production, consider using a custom IMemoryCache implementation with Clear support

        // Clear L3
        if (_isInitialized && _sqliteConnection != null)
        {
            await _sqliteLock.WaitAsync(cancellationToken);
            try
            {
                var command = _sqliteConnection.CreateCommand();
                command.CommandText = "DELETE FROM cache";
                await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation("Cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SQLite clear error");
            }
            finally
            {
                _sqliteLock.Release();
            }
        }
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        // L1 cleanup happens automatically via expiration

        // L3 cleanup
        if (_isInitialized && _sqliteConnection != null)
        {
            await _sqliteLock.WaitAsync(cancellationToken);
            try
            {
                var command = _sqliteConnection.CreateCommand();
                command.CommandText = "DELETE FROM cache WHERE expires_at < $now";
                command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
                var deleted = await command.ExecuteNonQueryAsync(cancellationToken);
                
                if (deleted > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} expired cache entries", deleted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SQLite cleanup error");
            }
            finally
            {
                _sqliteLock.Release();
            }
        }
    }

    public async Task<Dictionary<string, object>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new Dictionary<string, object>();

        // L3 SQLite stats
        if (_isInitialized && _sqliteConnection != null)
        {
            await _sqliteLock.WaitAsync(cancellationToken);
            try
            {
                var command = _sqliteConnection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM cache";
                var count = await command.ExecuteScalarAsync(cancellationToken);
                stats["l3_sqlite_items"] = count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SQLite stats error");
                stats["l3_sqlite_items"] = 0;
            }
            finally
            {
                _sqliteLock.Release();
            }
        }

        return stats;
    }

    public async ValueTask DisposeAsync()
    {
        if (_sqliteConnection != null)
        {
            await _sqliteConnection.DisposeAsync();
            _sqliteConnection = null;
        }
        
        _sqliteLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
