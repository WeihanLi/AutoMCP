var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder => builder
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(_ => true) // allow any origin
            .AllowCredentials()); // allow credentials
});

// ======================================
// Add MCP server with AutoMcp and OData support
// This is the main focus of this example
// ======================================
builder.Services.AddMcpServer()
    .WithHttpTransport(o => { o.Stateless = true; })
    .WithOData()
    .WithAutoMcp();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.MapMcp("mcp");

app.Run();
