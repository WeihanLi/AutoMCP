using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;

namespace AutoMcp.OData.Serialization;

/// <summary>
/// Configures JSON serialization options to include the ODataQueryOptionsJsonConverterFactory.
/// </summary>
public class ConfigureODataJsonOptions : IConfigureOptions<JsonOptions>
{
    private readonly IODataQueryOptionsFactory _oDataQueryOptionsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureODataJsonOptions"/> class.
    /// </summary>
    /// <param name="oDataQueryOptionsFactory">The OData query options factory.</param>
    /// <exception cref="ArgumentNullException">oDataQueryOptionsFactory</exception>
    public ConfigureODataJsonOptions(IODataQueryOptionsFactory oDataQueryOptionsFactory)
    {
        _oDataQueryOptionsFactory = oDataQueryOptionsFactory ?? throw new ArgumentNullException(nameof(oDataQueryOptionsFactory));
    }

    /// <inheritdoc/>
    public void Configure(JsonOptions options)
    {
        options.JsonSerializerOptions.Converters.Add(new ODataQueryOptionsJsonConverterFactory(_oDataQueryOptionsFactory));
    }
}
