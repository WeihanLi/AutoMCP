using Microsoft.AspNetCore.OData.Query;
using System.Collections.Generic;

namespace AutoMcp.OData.Serialization;

/// <summary>
/// Factory interface for creating ODataQueryOptions instances.
/// </summary>
public interface IODataQueryOptionsFactory
{
    /// <summary>
    /// Creates an ODataQueryOptions for the specified entity type.
    /// </summary>
    /// <param name="context">The OData query context.</param>
    /// <param name="queryParameters">The query parameters.</param>
    /// <returns>An ODataQueryOptions for the specified entity type.</returns>
    ODataQueryOptions<TEntity> Create<TEntity>(ODataQueryContext context, IDictionary<string, string> queryParameters) where TEntity : class;

    /// <summary>
    /// Creates an ODataQueryOptions for the specified entity type.
    /// </summary>
    /// <param name="queryParameters">The query parameters.</param>
    /// <returns>An ODataQueryOptions for the specified entity type.</returns>
    ODataQueryOptions<TEntity> Create<TEntity>(IDictionary<string, string> queryParameters) where TEntity : class;
}