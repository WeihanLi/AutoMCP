using Microsoft.AspNetCore.OData.Query;
using System.Collections.Generic;

namespace AutoMcp.OData.Serialization;

/// <summary>
/// Factory interface for creating ODataQueryContext instances.
/// </summary>
public interface IODataQueryContextFactory
{
    /// <summary>
    /// Creates an ODataQueryContext for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="normalizedQueryParameters">The normalized query parameters.</param>
    /// <returns>An ODataQueryContext for the specified entity type.</returns>
    ODataQueryContext CreateODataQueryContext<T>(IDictionary<string, string> normalizedQueryParameters) where T : class;
}