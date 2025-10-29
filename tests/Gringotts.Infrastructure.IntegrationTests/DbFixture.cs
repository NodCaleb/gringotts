using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace Gringotts.Infrastructure.IntegrationTests;

public class DbFixture : IAsyncLifetime
{
    private readonly IContainer _container;

    public string ConnectionString => ((PostgreSqlContainer)_container).GetConnectionString();

    public DbFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

}
