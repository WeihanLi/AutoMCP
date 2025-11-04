using Microsoft.AspNetCore.OData.Query;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoMcp.OData.Serialization;

/// <summary>
/// JSON converter factory for creating instances of <see cref="ODataQueryOptionsJsonConverter{T}"/>.
/// </summary>
/// <remarks>
/// This factory creates converters for any <see cref="ODataQueryOptions{T}"/> type, enabling
/// JSON serialization and deserialization of OData query options.
/// </remarks>
public class ODataQueryOptionsJsonConverterFactory : JsonConverterFactory
{
    private readonly IODataQueryOptionsFactory _oDataQueryOptionsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataQueryOptionsJsonConverterFactory"/> class.
    /// </summary>
    /// <param name="oDataQueryOptionsFactory">The factory for creating OData query options instances.</param>
    public ODataQueryOptionsJsonConverterFactory(IODataQueryOptionsFactory oDataQueryOptionsFactory)
    {
        _oDataQueryOptionsFactory = oDataQueryOptionsFactory;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns <c>true</c> if the type is a generic <see cref="ODataQueryOptions{T}"/> type; otherwise, <c>false</c>.
    /// </remarks>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }
        if (typeToConvert.GetGenericTypeDefinition() != typeof(ODataQueryOptions<>))
        {
            return false;
        }
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Creates an instance of <see cref="ODataQueryOptionsJsonConverter{T}"/> where T is the entity type
    /// from the <see cref="ODataQueryOptions{T}"/> generic argument.
    /// </remarks>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var entityType = typeToConvert.GetGenericArguments()[0];
        var converter = Activator.CreateInstance(
            typeof(ODataQueryOptionsJsonConverter<>).MakeGenericType(entityType),
            _oDataQueryOptionsFactory) as JsonConverter;
        return converter;
    }
}