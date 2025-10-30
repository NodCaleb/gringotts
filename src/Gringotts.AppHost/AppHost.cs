var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var gringottsDb = postgres.AddDatabase("gringottsdb");

var seeder = builder.AddProject<Projects.Gringotts_Seeder>("db-seeder")
    .WithReference(gringottsDb)
    .WaitFor(gringottsDb);

var apiService = builder.AddProject<Projects.Gringotts_ApiService>("gringotts-api")
 .WithHttpHealthCheck("/health")
 .WaitFor(gringottsDb)
 .WithReference(gringottsDb)
 .WaitForCompletion(seeder);

builder.AddProject<Projects.Gringotts_Web>("web-frontend")
 .WithExternalHttpEndpoints()
 .WithHttpHealthCheck("/health")
 .WithReference(apiService)
 .WaitFor(apiService);

builder.AddProject<Projects.Gringotts_Bot>("gringotts-bot")
 .WithExternalHttpEndpoints()
 .WithReference(apiService)
 .WaitFor(apiService);

builder.Build().Run();
