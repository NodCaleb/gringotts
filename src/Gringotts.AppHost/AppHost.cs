var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddContainer("postgres", "postgres:15-alpine");

var apiService = builder.AddProject<Projects.Gringotts_ApiService>("apiservice")
 .WithHttpHealthCheck("/health")
 .WaitFor(postgres);

builder.AddProject<Projects.Gringotts_Web>("webfrontend")
 .WithExternalHttpEndpoints()
 .WithHttpHealthCheck("/health")
 .WithReference(apiService)
 .WaitFor(apiService);

builder.AddProject<Projects.Gringotts_Bot>("bot")
 .WithExternalHttpEndpoints()
 .WithReference(apiService)
 .WaitFor(apiService);

builder.Build().Run();
