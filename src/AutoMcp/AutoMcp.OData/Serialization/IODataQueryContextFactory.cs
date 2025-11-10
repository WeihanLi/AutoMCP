using Microsoft.AspNetCore.OData.Query;

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
    /// <returns>An ODataQueryContext for the specified entity type.</returns>
    ODataQueryContext CreateODataQueryContext<T>() where T : class;
}