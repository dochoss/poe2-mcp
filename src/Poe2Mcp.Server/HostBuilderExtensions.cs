using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections;
using System.Reflection;

namespace Poe2Mcp.Server;

/// <summary>
/// This represents the extension entity for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class HostApplicationBuilderExtensions
{
  /// <summary>
  /// Builds the application with the specified <paramref name="useStreamableHttp"/> option.
  /// </summary>
  /// <param name="builder"><see cref="IHostApplicationBuilder"/> instance.</param>
  /// <param name="useStreamableHttp">Value indicating whether to use streamable HTTP or not.</param>
  /// <returns>Returns the <see cref="IHost"/> instance.</returns>
  public static IHost BuildApp(this IHostApplicationBuilder builder, bool useStreamableHttp)
  {
    if (useStreamableHttp == true)
    {
      builder.Services.AddMcpServer()
                      .WithHttpTransport(o => o.Stateless = true)
                      .WithPromptsFromAssembly(Assembly.GetEntryAssembly())
                      .WithResourcesFromAssembly(Assembly.GetEntryAssembly())
                      .WithToolsFromAssembly(Assembly.GetEntryAssembly());

      var webApp = (builder as WebApplicationBuilder)!.Build();

      // Disable HTTPS redirection in development environment to avoid issues with self-signed certificates
      if (!webApp.Environment.IsDevelopment())
      {
        webApp.UseHttpsRedirection();
      }

      webApp.MapMcp("/mcp");

      return webApp;
    }

    builder.Services.AddMcpServer()
                    .WithStdioServerTransport()
                    .WithPromptsFromAssembly(Assembly.GetEntryAssembly())
                    .WithResourcesFromAssembly(Assembly.GetEntryAssembly())
                    .WithToolsFromAssembly(Assembly.GetEntryAssembly());

    var consoleApp = (builder as HostApplicationBuilder)!.Build();

    return consoleApp;
  }

  /// <summary>
  /// Checks whether to use streamable HTTP or not.
  /// </summary>
  /// <param name="env"><see cref="IDictionary"/> instance representing environment variables.</param>
  /// <param name="args">List of arguments passed from the command line.</param>
  /// <returns>Returns <c>True</c> if streamable HTTP is enabled; otherwise, <c>False</c>.</returns>
  public static bool UseStreamableHttp(IDictionary env, string[] args)
  {
    var useHttp = env.Contains("UseHttp") &&
                  bool.TryParse(env["UseHttp"]?.ToString()?.ToLowerInvariant(), out var result) && result;
    if (args.Length == 0)
    {
      return useHttp;
    }

    useHttp = args.Contains("--http", StringComparer.InvariantCultureIgnoreCase);

    return useHttp;
  }
}
