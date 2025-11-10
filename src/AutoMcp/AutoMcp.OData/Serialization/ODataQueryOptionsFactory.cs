using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace AutoMcp.OData.Serialization;

/// <summary>
/// Factory class for creating ODataQueryOptions instances.
/// </summary>
public class ODataQueryOptionsFactory : IODataQueryOptionsFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IODataQueryContextFactory _oDataQueryContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataQueryOptionsFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="oDataQueryContextFactory">The factory for creating OData query contexts.</param>
    public ODataQueryOptionsFactory(IServiceProvider serviceProvider, IODataQueryContextFactory oDataQueryContextFactory)
    {
        _serviceProvider = serviceProvider;
        _oDataQueryContextFactory = oDataQueryContextFactory;
    }

    /// <inheritdoc/>
    public ODataQueryOptions<TEntity> Create<TEntity>(IDictionary<string, string> queryParameters)
     where TEntity : class
        => Create<TEntity>(_serviceProvider, _oDataQueryContextFactory.CreateODataQueryContext<TEntity>(), queryParameters);

    /// <inheritdoc/>
    public ODataQueryOptions<TEntity> Create<TEntity>(ODataQueryContext context, IDictionary<string, string> queryParameters)
        where TEntity : class
        => Create<TEntity>(_serviceProvider, context, queryParameters);

    /// <summary>
    /// Creates an ODataQueryOptions for the specified entity type using the provided service provider, context, and query parameters.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to create query options for.</typeparam>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="context">The OData query context containing model and path information.</param>
    /// <param name="queryParameters">The query parameters to include in the OData query.</param>
    /// <returns>An <see cref="ODataQueryOptions{TEntity}"/> instance configured with the specified parameters.</returns>
    public static ODataQueryOptions<TEntity> Create<TEntity>(IServiceProvider serviceProvider, ODataQueryContext context, IDictionary<string, string> queryParameters)
     where TEntity : class
          => new(context, CreateRequest(serviceProvider, context, queryParameters));

    /// <summary>
    /// Creates a mock HTTP request with the specified OData context and query parameters.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="context">The OData query context containing model and path information.</param>
    /// <param name="queryParameters">The query parameters to include in the request query string.</param>
    /// <returns>An <see cref="HttpRequest"/> configured with OData features and query parameters.</returns>
    /// <remarks>
    /// This method creates a mock HTTP GET request to localhost with the OData model, path, and services
    /// configured in the HTTP context's OData feature. This allows ODataQueryOptions to be constructed
    /// without requiring an actual HTTP request.
    /// </remarks>
    private static HttpRequest CreateRequest(IServiceProvider serviceProvider, ODataQueryContext context, IDictionary<string, string> queryParameters)
    {
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        request.Method = HttpMethods.Get;
        request.Scheme = "http";
        request.Host = new HostString("localhost");
        request.Path = "/";
        if (queryParameters.Count > 0)
        {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types. - this is safe as we ensure values are non-null strings and QueryHelpers accepts null-string values
            var queryString = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(string.Empty, queryParameters);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            request.QueryString = new QueryString(queryString);
        }
        var oDataFeature = httpContext.ODataFeature();
        oDataFeature.Model = context.Model;
        oDataFeature.Path = context.Path;
        oDataFeature.Services = serviceProvider.CreateScope().ServiceProvider;

        return request;
    }
}
