using AutoMcp.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutoMcp.AI;

/// <summary>
/// An AI function that wraps an ASP.NET Core ControllerActionDescriptor.
/// </summary>
public class ActionDescriptorAiFunction : AIFunction
{
    private readonly ControllerActionDescriptor _actionDescriptor;

    /// <summary>
    /// Initializes a new instance of the ActionDescriptorAiFunction class using the specified controller action
    /// descriptor and JSON serializer options.
    /// </summary>
    /// <remarks>This constructor extracts metadata from the provided action descriptor to generate function
    /// and response schemas suitable for AI integration scenarios. The generated JSON schemas reflect the method's
    /// parameters and possible response types, enabling dynamic schema-driven processing. The Name and Description
    /// properties are derived from the action descriptor and its endpoint metadata.</remarks>
    /// <param name="actionDescriptor">The controller action descriptor that provides metadata about the target action method, including its name,
    /// controller, and endpoint information.</param>
    /// <param name="jsonSerializerOptions">The options to use for JSON serialization and schema generation. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if the jsonSerializerOptions parameter is null.</exception>
    public ActionDescriptorAiFunction(ControllerActionDescriptor actionDescriptor, JsonSerializerOptions jsonSerializerOptions)
    {
        JsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
        _actionDescriptor = actionDescriptor;
        UnderlyingMethod = actionDescriptor.MethodInfo;
        Name = $"{actionDescriptor.ControllerName}_{actionDescriptor.ActionName}";
        Description = actionDescriptor.EndpointMetadata.OfType<DescriptionAttribute>().FirstOrDefault()?.Description ?? "";

        var schemaCreateOptions = new AIJsonSchemaCreateOptions()
        {
            TransformSchemaNode = (context, node) =>
            {
                if (context.TypeInfo.Converter is IJsonSchemaWriter jsonSchemaWriter)
                    return jsonSchemaWriter.GetJsonSchemaAsNode(context.TypeInfo.Options);
                return node;
            },

        };

        JsonSchema = AIJsonUtilities.CreateFunctionJsonSchema(UnderlyingMethod, serializerOptions: jsonSerializerOptions, inferenceOptions: schemaCreateOptions);

        var rawResponseTypes = actionDescriptor.EndpointMetadata.OfType<ProducesResponseTypeAttribute>().Select(a => a.Type)
            .Concat(actionDescriptor.EndpointMetadata.OfType<ProducesErrorResponseTypeAttribute>().Select(a => a.Type));

        var responseTypes = rawResponseTypes
            .DistinctBy(type => type)
            .Select(type => AIJsonUtilities.CreateJsonSchema(type, serializerOptions: jsonSerializerOptions, inferenceOptions: schemaCreateOptions))
            .ToList();

        var methodResponseType = AIJsonUtilities.CreateJsonSchema(actionDescriptor.MethodInfo.ReturnType, serializerOptions: jsonSerializerOptions, inferenceOptions: schemaCreateOptions);
        responseTypes.Add(methodResponseType);

        var schemaObject = new
        {
            oneOf = responseTypes
        };
        ReturnJsonSchema = JsonSerializer.SerializeToElement(schemaObject);
    }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override string Description { get; }

    /// <inheritdoc/>
    public override JsonElement JsonSchema { get; }

    /// <inheritdoc/>
    public override JsonElement? ReturnJsonSchema { get; }

    /// <inheritdoc/>
    public override MethodInfo? UnderlyingMethod { get; }

    /// <inheritdoc/>
    public override JsonSerializerOptions JsonSerializerOptions { get; }

    /// <inheritdoc/>
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        try
        {
            // Get required services
            var services = arguments.Services ?? throw new ArgumentException($"{nameof(arguments.Services)} is required.", nameof(arguments));
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException("No HttpContext present.");
            var jsonOptions = provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

            // Modify the HttpContext to match the target controller action
            ModifyRequestToFitTargetMethod(httpContext, arguments);

            // Create the controller and parameters
            var controller = ActivatorUtilities.CreateInstance(provider, _actionDescriptor.ControllerTypeInfo.AsType());
            var parameters = CreateParameterArray(arguments, jsonOptions);

            // Execute the action method
            var result = await InvokeControllerMethod(controller, parameters);

            return result;
        }
        catch (Exception ex)
        {
            return new ProblemDetails
            {
                Detail = ex.Message,
                Status = 500,
                Title = $"An error occured while invoking {_actionDescriptor.ControllerName}.{_actionDescriptor.ActionName}",
                Extensions =
                {
                    ["ExceptionType"] = ex.GetType().Name,
                    ["StackTrace"] = ex.StackTrace,
                }
            };
        }
    }

    private async Task<object?> InvokeControllerMethod(object controller, object?[] parameters)
    {
        // Execute the underlying action method sync or async based on its return type
        object? result;
        if (typeof(Task).IsAssignableFrom(_actionDescriptor.MethodInfo.ReturnType))
        {
            dynamic task = _actionDescriptor.MethodInfo.Invoke(controller, parameters)!;
            await task;
            result = task.GetAwaiter().GetResult();
        }
        else
            result = _actionDescriptor.MethodInfo.Invoke(controller, parameters);

        // Unwrap ActionResult if necessary
        result = UnwrapActionResult(result);

        return result;
    }

    private object?[] CreateParameterArray(AIFunctionArguments arguments, JsonSerializerOptions jsonOptions)
    {
        DeserializeParameters(arguments, jsonOptions);
        var parameters = _actionDescriptor.Parameters.Select(p => arguments.GetValueOrDefault(p.Name, p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)).ToArray();
        return parameters;
    }

    private void ModifyRequestToFitTargetMethod(HttpContext httpContext, AIFunctionArguments arguments)
    {
        // Set the HTTP method
        var method = _actionDescriptor.EndpointMetadata.OfType<IHttpMethodMetadata>().FirstOrDefault()?.HttpMethods?.FirstOrDefault();
        method ??= _actionDescriptor.ActionConstraints?.OfType<HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods?.FirstOrDefault();
        if (method is null)
            throw new InvalidOperationException($"No HTTP method found for the target action '{_actionDescriptor.ActionName}'.");

        httpContext.Request.Method = method;

        // Set the route values
        var currentRouteValues = new RouteValueDictionary(_actionDescriptor.RouteValues);
        foreach (var parameterDescriptor in _actionDescriptor.Parameters)
        {
            if (arguments.TryGetValue(parameterDescriptor.Name, out var obj) && obj is JsonElement jsonElement)
            {
                currentRouteValues[parameterDescriptor.Name] = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => parameterDescriptor.ParameterType == typeof(int) ? jsonElement.GetInt32() :
                                                               parameterDescriptor.ParameterType == typeof(long) ? jsonElement.GetInt64() :
                                                               parameterDescriptor.ParameterType == typeof(float) ? jsonElement.GetSingle() :
                                                               parameterDescriptor.ParameterType == typeof(double) ? jsonElement.GetDouble() :
                                                               parameterDescriptor.ParameterType == typeof(decimal) ? jsonElement.GetDecimal() :
                                                               null,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => null
                };
            }
        }
        var routeValuesFeature = httpContext.Features.Get<IRouteValuesFeature>();
        if (routeValuesFeature == null)
        {
            routeValuesFeature = new RouteValuesFeature();
            httpContext.Features.Set(routeValuesFeature);
        }
        routeValuesFeature.RouteValues = currentRouteValues;


        // Set the request path
        var linkGenerator = arguments.Services!.GetRequiredService<LinkGenerator>();
        var path = linkGenerator.GetPathByAction(_actionDescriptor.ActionName, _actionDescriptor.ControllerName, currentRouteValues);
        if (path != null)
            httpContext.Request.Path = path;

        // Set the endpoint
        _actionDescriptor.EndpointMetadata.Add(_actionDescriptor);
        var endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(_actionDescriptor.EndpointMetadata), _actionDescriptor.DisplayName);
        httpContext.SetEndpoint(endpoint);
    }

    private static object? UnwrapActionResult(object? result)
    {
        if (result is IConvertToActionResult actionResult)
            result = actionResult.Convert();
        if (result is ObjectResult objectResult)
            return objectResult.Value;
        return result;
    }

    private void DeserializeParameters(AIFunctionArguments arguments, JsonSerializerOptions jsonOptions)
    {
        foreach (var parameter in _actionDescriptor.Parameters)
        {
            if (arguments.TryGetValue(parameter.Name, out var obj) && obj is JsonElement jsonElement)
            {
                // Deserialize the JsonElement to the correct parameter type
                try
                {
                    var deserializedValue = JsonSerializer.Deserialize(jsonElement.GetRawText(), parameter.ParameterType, jsonOptions);
                    arguments[parameter.Name] = deserializedValue;
                }
                catch (JsonException)
                {
                    // If deserialization fails, keep the original value or set to null/default
                    arguments[parameter.Name] = parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null;
                }
            }
        }
    }
}
