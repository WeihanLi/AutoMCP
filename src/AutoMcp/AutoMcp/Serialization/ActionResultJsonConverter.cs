using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;

namespace AutoMcp.Serialization;

/// <summary>
/// A converter which handles serialization of ActionResult&lt;T&gt; instances.
/// When serializing, it checks if the value is an ObjectResult and extracts the inner value.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public class ActionResultJsonConverter<T> : JsonConverter<ActionResult<T>>, IJsonSchemaWriter<T>
{
    /// <inheritdoc/>
    public override ActionResult<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize the inner type and wrap it in an ActionResult
        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return new ActionResult<T>(new ObjectResult(value));
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ActionResult<T> value, JsonSerializerOptions options)
    {
        // Check if the ActionResult contains an ObjectResult
        if (value.Result is ObjectResult objectResult)
        {
            // Serialize the inner value from the ObjectResult
            JsonSerializer.Serialize(writer, objectResult.Value, options);
        }
        else if (value.Result != null)
        {
            // If it's some other IActionResult type, serialize the Result itself
            JsonSerializer.Serialize(writer, value.Result, options);
        }
        else
        {
            // If Result is null, serialize the Value property
            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }

    /// <inheritdoc/>
    public JsonNode GetJsonSchemaAsNode(JsonSerializerOptions options)
    {
        // ActionResult<T> is a wrapper, so the schema should represent the inner type T
        var innerTypeSchema = JsonSchemaExporter.GetJsonSchemaAsNode(options, typeof(T));
        return innerTypeSchema;
    }
}
