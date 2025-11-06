var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var gringottsDb = postgres.AddDatabase("gringottsdb");

var cache = builder.AddRedis("cache");

var seeder = builder.AddProject<Projects.Gringotts_Seeder>("db-seeder")
    .WithReference(gringottsDb)
    .WaitFor(gringottsDb);

var apiService = builder.AddProject<Projects.Gringotts_ApiService>("gringotts-api")
 .WithHttpHealthCheck("/health")
 .WaitFor(gringottsDb)
 .WithReference(gringottsDb)
 .WaitForCompletion(seeder);

builder.AddProject<Projects.Gringotts_Bot>("gringotts-bot")
 .WithExternalHttpEndpoints()
 .WithReference(apiService)
 .WaitFor(apiService)
 .WithReference(cache);

var bff = builder.AddProject<Projects.Gringotts_BFF>("gringotts-bff")
 .WithHttpHealthCheck("/health")
 .WithReference(apiService)
 .WaitFor(apiService)
 .WithReference(cache);

builder.AddProject<Projects.Gringotts_Web>("web-frontend")
 .WithExternalHttpEndpoints()
 .WithHttpHealthCheck("/health")
 .WithReference(bff)
 .WaitFor(bff);

builder.Build().Run();
