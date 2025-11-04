using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoMcp.Serialization;

/// <summary>
/// Defines a contract for types that can generate JSON Schema representations.
/// </summary>
/// <typeparam name="T">The type for which the JSON Schema is generated.</typeparam>
public interface IJsonSchemaWriter<T> : IJsonSchemaWriter
{
}

/// <summary>
/// Defines a contract for types that can generate JSON Schema representations.
/// </summary>
public interface IJsonSchemaWriter
{
    /// <summary>
    /// Gets the JSON Schema representation as a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use when generating the schema.</param>
    /// <returns>A <see cref="JsonNode"/> containing the JSON Schema representation.</returns>
    JsonNode GetJsonSchemaAsNode(JsonSerializerOptions options);
}
