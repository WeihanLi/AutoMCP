# AutoMCP

Automatically create an MCP (Model Context Protocol) Server from your ASP.NET Core APIs.

## 🚀 Overview

AutoMCP bridges the gap between your ASP.NET Core APIs and AI-powered tools by automatically exposing your API endpoints as MCP tools. With just a few lines of code, your existing API controllers become accessible to AI assistants and other MCP-compatible clients.

## 📦 Installation

Install the main package:

```bash
dotnet add package AutoMcp
```

For OData support, also install:

```bash
dotnet add package AutoMcp.OData
```

## 🎯 Quick Start

Add AutoMCP to your ASP.NET Core application in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add MCP server with automatic API endpoint discovery
builder.Services.AddMcpServer()
 .WithHttpTransport()
    .WithOData()    // Optional: Add OData query support. Must be called before WithAutoMcp()
    .WithAutoMcp();      // Automatically expose API endpoints as MCP tools

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.MapMcp("mcp");  // Map MCP endpoint at /mcp

app.Run();
```



### Try the Example

The repository includes a complete working example with .NET Aspire support. To run it:

```bash
# Clone the repository
git clone https://github.com/wertzui/AutoMCP
cd AutoMCP/src/AutoMcp

# Run the Aspire app host
dotnet run --project AutoMcp.AppHost
```
The Aspire dashboard will open, showing the running example API with AutoMCP configured. And the MCP insprector will be pointed to the correct endpoint.

## 🔧 Core Methods

### `WithAutoMcp()`

The `WithAutoMcp()` method is the heart of AutoMCP. It automatically discovers all API endpoints in your ASP.NET Core application and generates corresponding MCP tools.

#### What it does:
- **Automatic Discovery**: Scans all controller actions and creates MCP tools for each endpoint
- **Schema Generation**: Automatically generates JSON schemas for request parameters and response types
- **Metadata Extraction**: Uses endpoint metadata like `[Description]` or `[ProducesResponseType]` attributes to provide context to AI clients
- **Action Results**: Properly unwraps ASP.NET Core `ActionResult<T>` types

#### Configuration Options:

```csharp
builder.Services.AddMcpServer()
    .WithAutoMcp(
        serializerOptions: customJsonOptions,  // Optional: Custom JSON serialization options
        apiDescriptionGroupCollectionProvider: provider  // Optional: Custom API description provider
    );
```

#### Example Controller:

```csharp
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    [Description("Get the weather forecast for the given date.")]
    public ActionResult<WeatherForecast> Get(DateOnly date)
    {
        var forecast = new WeatherForecast
        {
        Date = date,
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = "Sunny"
        };
        
        return Ok(forecast);
    }
}
```

This controller action automatically becomes an MCP tool named `WeatherForecast_Get` with:
- The description from the `[Description]` attribute
- Proper parameter schema for the `date` parameter
- Response schema based on the `WeatherForecast` type

### `WithOData()`

The `WithOData()` method adds support for OData query options, enabling powerful filtering, sorting, and pagination capabilities in your MCP tools.

#### What it does:
- **Query Support**: Enables `$filter`, `$orderby`, `$top`, `$skip`, `$select`, and other OData query options
- **JSON Serialization**: Adds custom JSON converters to properly serialize and deserialize `ODataQueryOptions<T>`
- **Schema Integration**: Integrates OData query schemas into MCP tool definitions
- **Type Preservation**: Maintains type information across the MCP boundary

#### Configuration:

```csharp
builder.Services.AddMcpServer()
 .WithAutoMcp()
    .WithOData();  // Enable OData query support
```

#### Example with OData:

```csharp
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly IQueryable<WeatherForecast> _repository = 
     GetWeatherForecasts().AsQueryable();

    [HttpGet("multiple", Name = "GetMultipleWeatherForecasts")]
    [Description("Get multiple weather forecasts with filtering and sorting options.")]
    public ActionResult<IEnumerable<WeatherForecast>> GetMultiple(
        ODataQueryOptions<WeatherForecast> options)
    {
     return Ok(options.ApplyTo(_repository));
    }
}
```

With `WithOData()`, AI clients can now query your API with powerful filters:
- `$filter=temperatureC gt 20` - Get forecasts warmer than 20°C
- `$orderby=date desc` - Sort by date descending
- `$top=10` - Get only the first 10 results
- `$skip=5&$top=10` - Pagination support

## 🎨 Features

### Automatic Tool Generation
- ✅ Discovers all API endpoints automatically
- ✅ Generates proper JSON schemas for parameters and responses
- ✅ Handles complex types and nested objects
- ✅ Supports async and sync controller actions

### OData Integration
- ✅ Full OData query syntax support
- ✅ Filtering, sorting, pagination
- ✅ Complex query expressions

### ASP.NET Core Integration
- ✅ Works with standard ASP.NET Core controllers
- ✅ Respects routing and HTTP method constraints
- ✅ Integrates with ASP.NET Core dependency injection
- ✅ Maintains HttpContext throughout the request pipeline

### Developer Experience
- ✅ Minimal configuration required
- ✅ Uses existing API metadata and attributes
- ✅ Comprehensive error handling and logging

## 📚 How It Works

1. **Discovery Phase**: `WithAutoMcp()` scans your application's API description to find all endpoints
2. **Schema Generation**: For each endpoint, it generates:
   - Input parameter schemas using reflection and JSON schema generation
   - Output schemas based on return types and `[ProducesResponseType]` attributes
3. **Tool Registration**: Each endpoint becomes an MCP tool with a name like `{ControllerName}_{ActionName}`
4. **Invocation**: When an AI client calls the tool:
   - Parameters are deserialized using the configured JSON options
   - A simulated HTTP request is created with proper routing
   - The controller action is invoked through ASP.NET Core's pipeline
   - Results are serialized and returned to the client

## 🔍 Advanced Configuration

### Custom JSON Serialization

```csharp
var customJsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

builder.Services.AddMcpServer()
    .WithAutoMcp(serializerOptions: customJsonOptions);
```

## 📖 Example Use Cases

### Data Analytics
Query large datasets with OData filtering and pagination.

### Content Management
Retrieve and filter content with complex query expressions.

### Business Intelligence
Enable AI assistants to query business data with natural language converted to OData queries.

## 🛠️ Requirements

- .NET 9.0 or later
- ASP.NET Core

## 📄 License

This project is released into the public domain under The Unlicense. See [LICENSE](LICENSE) for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## 📞 Support

- GitHub Issues: [Report bugs or request features](https://github.com/wertzui/AutoMCP/issues)
- Discussions: [Ask questions and share ideas](https://github.com/wertzui/AutoMCP/discussions)
