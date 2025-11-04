using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoMcp.Serialization;

/// <summary>
/// A factory to create an <see cref="ActionResultJsonConverter{T}"/> with the correct generic type.
/// </summary>
public class ActionResultJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        if (typeToConvert.GetGenericTypeDefinition() != typeof(ActionResult<>))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var resultType = typeToConvert.GetGenericArguments()[0];

        var converter = Activator.CreateInstance(
            typeof(ActionResultJsonConverter<>).MakeGenericType(resultType)) as JsonConverter;

        return converter;
    }
}
