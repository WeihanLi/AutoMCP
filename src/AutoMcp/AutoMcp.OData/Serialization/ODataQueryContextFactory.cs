using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System;
using System.Collections.Generic;

namespace AutoMcp.OData.Serialization;

/// <summary>
/// Factory class for creating ODataQueryContext instances with EDM model caching.
/// </summary>
public class ODataQueryContextFactory : IODataQueryContextFactory
{
    private readonly IMemoryCache _memoryCache;
    private const string _cacheKeyPrefix = "ODataQueryContextFactory_";

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataQueryContextFactory"/> class.
    /// </summary>
    /// <param name="memoryCache">The memory cache for storing EDM models.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="memoryCache"/> is null.</exception>
    public ODataQueryContextFactory(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    /// <inheritdoc/>
    public ODataQueryContext CreateODataQueryContext<T>(IDictionary<string, string> normalizedQueryParameters)
        where T : class
    {
        var entityClrType = typeof(T);
        var model = GetEdmModel(entityClrType);
        Microsoft.OData.UriParser.ODataPath path = new();
        var context = new ODataQueryContext(model, entityClrType, path);

        return context;
    }

    /// <summary>
    /// Gets or creates a cached EDM model for the specified entity type.
    /// </summary>
    /// <param name="entityClrType">The CLR type of the entity to create an EDM model for.</param>
    /// <returns>An <see cref="IEdmModel"/> representing the entity type.</returns>
    /// <remarks>
    /// This method uses an in-memory cache to avoid repeatedly building the same EDM model.
    /// The cache key is constructed using the entity type's full name.
    /// </remarks>
    private IEdmModel GetEdmModel(Type entityClrType)
    {
        var key = _cacheKeyPrefix + entityClrType.FullName!;
        var cachedModel = _memoryCache.GetOrCreate(key, _ =>
        {
            var builder = new ODataConventionModelBuilder();
            var entityTypeConfiguration = builder.AddEntityType(entityClrType);
            builder.AddEntitySet(entityClrType.Name, entityTypeConfiguration);
            var model = builder.GetEdmModel();

            return model;
        });

        return cachedModel ?? throw new InvalidOperationException("Failed to create EDM model.");
    }
}
