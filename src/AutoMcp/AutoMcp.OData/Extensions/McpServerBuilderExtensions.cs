using AutoMcp.OData.Serialization;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.UriParser;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring MCP server builders with automatic MCP tool generation for API endpoints.
/// </summary>
/// <remarks>This class contains static extension methods that enable automatic discovery and registration of MCP
/// tools for all API endpoints in an ASP.NET Core application. These methods simplify the process of exposing API
/// endpoints as MCP tools, allowing for streamlined integration with MCP-based workflows. The extensions are intended
/// to be used during server builder configuration and do not require direct instantiation.</remarks>
public static class McpServerBuilderExtensions
{
    /// <summary>
    /// Adds OData query option support to the MCP server builder.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    public static IMcpServerBuilder WithOData(this IMcpServerBuilder builder)
    {
        builder.Services.AddOData();
        builder.Services.AddDefaultODataServices();
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IODataQueryContextFactory, ODataQueryContextFactory>();
        builder.Services.AddSingleton<IODataQueryOptionsFactory, ODataQueryOptionsFactory>();
        builder.Services.ConfigureOptions<ConfigureODataJsonOptions>();
        
        // Register case-insensitive OData URI resolver
        builder.Services.AddSingleton<ODataUriResolver>(sp => new UnqualifiedODataUriResolver { EnableCaseInsensitive = true });

        return builder;
    }
}
