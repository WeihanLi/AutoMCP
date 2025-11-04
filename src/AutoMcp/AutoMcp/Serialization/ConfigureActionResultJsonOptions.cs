using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AutoMcp.Serialization
{
    /// <summary>
    /// Configures JSON serialization options to include the ActionResultJsonConverterFactory.
    /// </summary>
    public class ConfigureActionResultJsonOptions : IConfigureOptions<JsonOptions>
    {
        /// <inheritdoc/>
        public void Configure(JsonOptions options)
        {
            options.JsonSerializerOptions.Converters.Insert(0, new ActionResultJsonConverterFactory());
        }
    }
}
