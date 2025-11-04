var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.AutoMcp_Example>("automcp-example");

var inspector = builder.AddMcpInspector("McpInspector", new McpInspectorOptions { InspectorVersion = "0.17.2" })
    .WithReference(api);

builder.Build().Run();
