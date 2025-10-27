var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gringottsDb = postgres.AddDatabase("gringottsdb");

var apiService = builder.AddProject<Projects.Gringotts_ApiService>("apiservice")
 .WithHttpHealthCheck("/health")
 .WaitFor(gringottsDb)
 .WithReference(gringottsDb);

builder.AddProject<Projects.Gringotts_Web>("webfrontend")
 .WithExternalHttpEndpoints()
 .WithHttpHealthCheck("/health")
 .WithReference(apiService)
 .WaitFor(apiService);

//builder.AddProject<Projects.Gringotts_Bot>("bot")
// .WithExternalHttpEndpoints()
// .WithReference(apiService)
// .WaitFor(apiService);

builder.Build().Run();
