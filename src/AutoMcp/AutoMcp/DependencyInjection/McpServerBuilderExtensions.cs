using AutoMcp.AI;
using AutoMcp.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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
    private const string _loggerName = $"{nameof(AutoMcp)}.{nameof(McpAuthenticationExtensions)}";

    /// <summary>
    /// Automatically generates MCP tools for all API endpoints defined in the ASP.NET Core application.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="serializerOptions">The JSON serializer options. If none is specified, the options will be retrieved from the service builder services.</param>
    /// <param name="apiDescriptionGroupCollectionProvider">The API description group collection provider. If none is specified, the provider will be retrieved from the service builder services.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    public static IMcpServerBuilder WithAutoMcp(this IMcpServerBuilder builder, JsonSerializerOptions? serializerOptions = null, IApiDescriptionGroupCollectionProvider? apiDescriptionGroupCollectionProvider = null)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.ConfigureOptions<ConfigureActionResultJsonOptions>();

        // Build a temporary service provider to get API descriptions
        using var tempProvider = builder.Services.BuildServiceProvider();

        apiDescriptionGroupCollectionProvider ??= tempProvider.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var latestApiGroup = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items.LastOrDefault();

        serializerOptions ??= tempProvider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        var logger = tempProvider.GetService<ILoggerFactory>()?.CreateLogger(_loggerName);

        var tools = new List<McpServerTool>();

        if (latestApiGroup != null)
        {
            foreach (var apiDescription in latestApiGroup.Items)
            {
                // Get the action descriptor which contains the method info
                var actionDescriptor = apiDescription.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                if (actionDescriptor?.MethodInfo != null)
                {
                    try
                    {
                        // Create a tool for each API endpoint method
                        tools.Add(McpServerTool.Create(
                            new ActionDescriptorAiFunction(actionDescriptor, serializerOptions),
                            options: new McpServerToolCreateOptions
                            {
                                UseStructuredContent = true,
                            }
                            ));
                        logger?.LogDebug("Created tool for API endpoint '{ApiEndpoint}'", apiDescription.RelativePath);
                    }
                    catch (Exception e)
                    {
                        // Skip tools that can't be created (e.g., due to unsupported parameter types)
                        logger?.LogError(e, "Failed to create tool for API endpoint '{ApiEndpoint}'", apiDescription.RelativePath);
                    }
                }
            }
        }

        return builder.WithTools(tools);
    }
}
