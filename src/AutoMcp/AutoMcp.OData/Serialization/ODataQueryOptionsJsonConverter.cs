using AutoMcp.Serialization;
using Microsoft.AspNetCore.OData.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace AutoMcp.OData.Serialization;

/// <summary>
/// JSON converter for serializing and deserializing <see cref="ODataQueryOptions{T}"/> instances.
/// </summary>
/// <typeparam name="T">The entity type for the OData query options.</typeparam>
/// <remarks>
/// This converter handles the conversion between JSON objects with OData query parameters (prefixed with $)
/// and <see cref="ODataQueryOptions{T}"/> instances. It also provides JSON Schema generation for OData query parameters.
/// </remarks>
public class ODataQueryOptionsJsonConverter<T> : JsonConverter<ODataQueryOptions<T>>, IJsonSchemaWriter
    where T : class
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(ti =>
        {
            if (ti.Type == typeof(ODataRawQueryOptions))
            {
                foreach (var property in ti.Properties)
                {
                    property.Name = "$" + property.Name;
                    property.Set = (obj, value) =>
                    {
                        var pi = ti.Type.GetProperty(property.Name[1..], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        pi!.SetValue(obj, value);
                    };
                }
            }
        })
    };
    private readonly IODataQueryOptionsFactory _oDataQueryOptionsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataQueryOptionsJsonConverter{T}"/> class.
    /// </summary>
    /// <param name="oDataQueryOptionsFactory">The factory for creating OData query options instances.</param>
    public ODataQueryOptionsJsonConverter(IODataQueryOptionsFactory oDataQueryOptionsFactory)
    {
        _oDataQueryOptionsFactory = oDataQueryOptionsFactory;
    }

    /// <summary>
    /// Reads and converts the JSON to <see cref="ODataQueryOptions{T}"/>.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>The converted <see cref="ODataQueryOptions{T}"/> instance.</returns>
    /// <remarks>
    /// This method deserializes the JSON as a dictionary of query parameters and creates an OData query options instance
    /// using the configured factory.
    /// </remarks>
    public override ODataQueryOptions<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var queryParameters = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, _jsonOptions) ?? [];
        var stringQueryParameters = queryParameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);

        return _oDataQueryOptionsFactory.Create<T>(stringQueryParameters);
    }

    /// <summary>
    /// Writes the <see cref="ODataQueryOptions{T}"/> as JSON.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <remarks>
    /// This method serializes the raw query values from the OData query options instance.
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, ODataQueryOptions<T> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.RawValues, options);

    /// <inheritdoc/>
    /// <remarks>
    /// This method generates a JSON Schema for OData query parameters by getting the schema for <see cref="ODataRawQueryOptions"/>
    /// and prefixing all property names with the '$' character to match OData query parameter conventions (e.g., $filter, $orderby, $top).
    /// </remarks>
    public JsonNode GetJsonSchemaAsNode(JsonSerializerOptions options)
    {
        var schema = options.GetJsonSchemaAsNode(typeof(ODataRawQueryOptions), new() { TreatNullObliviousAsNonNullable = true });

        // Add $ prefix to all properties to match OData query parameter conventions
        if (schema is JsonObject schemaObject &&
            schemaObject.TryGetPropertyValue("properties", out var propertiesNode) &&
            propertiesNode is JsonObject properties)
        {
            var updatedProperties = new JsonObject();

            foreach (var property in properties)
            {
                // Add $ prefix to each property name
                var prefixedName = "$" + property.Key;
                updatedProperties[prefixedName] = property.Value?.DeepClone();
            }

            schemaObject["properties"] = updatedProperties;
        }

        return schema;
    }
}
