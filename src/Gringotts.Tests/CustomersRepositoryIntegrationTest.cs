using Dapper;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Infrastructure.Repositories;
using Npgsql;
using System.Data;

namespace Gringotts.Tests;

public class CustomersRepositoryIntegrationTest : IDisposable, IClassFixture<DbFixture>
{
    private readonly IDbConnection _dbConnection;

    private readonly ICustomersRepository _customersRepository;

    public CustomersRepositoryIntegrationTest(DbFixture dbFixture)
    {
        _customersRepository = new PostgreCustomersRepository();
        _dbConnection = new NpgsqlConnection(dbFixture.ConnectionString);
        _dbConnection.Open();

        SetupDatabase();
    }

    public void Dispose()
    {
        // Clean up the database after tests
        const string dropTables = "" +
            "DROP TABLE IF EXISTS public.transactions;" +
            "DROP TABLE IF EXISTS public.employees;" +
            "DROP TABLE IF EXISTS public.customers;" +
            "";

        _dbConnection.Execute(dropTables);

        _dbConnection.Close();
        _dbConnection.Dispose();
    }

    private void SetupDatabase()
    {
        // Attempt to locate the SQL file from the Gringotts.Seeder project
        var relativePath = Path.Combine("Gringotts.Seeder", "sql", "create_tables_postgres.sql");
        var sqlFile = FindFileInParentDirectories(relativePath);
        if (sqlFile == null)
        {
            throw new FileNotFoundException($"Could not locate SQL file '{relativePath}' in parent directories.\nMake sure the file exists in the repository at 'Gringotts.Seeder/sql/create_tables_postgres.sql'.");
        }

        var sql = File.ReadAllText(sqlFile);

        _dbConnection.Execute(sql);
    }

    private static string? FindFileInParentDirectories(string relativePath)
    {
        // Start from the test assembly base directory and walk up until we find the file or reach the root
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, relativePath);
            if (File.Exists(candidate))
                return candidate;

            var parent = Directory.GetParent(dir);
            if (parent == null)
                break;

            dir = parent.FullName;
        }

        return null;
    }

    [Fact]
    public async Task AddAsync_Should_Insert_And_GetByIdShouldReturnInserted()
    {
        using var insertTransaction = _dbConnection.BeginTransaction();

        var customer = new Domain.Entities.Customer
        {
            UserName = "test_customer_add",
            PersonalName = "Test Personal",
            CharacterName = "Test Character",
            Balance =123.45m
        };

        var added = await _customersRepository.AddAsync(customer, _dbConnection, insertTransaction, CancellationToken.None);

        Assert.NotNull(added);
        Assert.NotEqual(0L, added.Id);

        var fetched = await _customersRepository.GetByIdAsync(added.Id, _dbConnection, insertTransaction, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("test_customer_add", fetched!.UserName);
        Assert.Equal("Test Personal", fetched.PersonalName);
        Assert.Equal("Test Character", fetched.CharacterName);
        Assert.Equal(123.45m, fetched.Balance);

        insertTransaction.Commit();
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Inserted()
    {
        using var transaction = _dbConnection.BeginTransaction();

        var c1 = new Domain.Entities.Customer { UserName = "all_customer_1", PersonalName = "P1", CharacterName = "C1", Balance =10m };
        var c2 = new Domain.Entities.Customer { UserName = "all_customer_2", PersonalName = "P2", CharacterName = "C2", Balance =20m };

        await _customersRepository.AddAsync(c1, _dbConnection, transaction, CancellationToken.None);
        await _customersRepository.AddAsync(c2, _dbConnection, transaction, CancellationToken.None);

        var all = await _customersRepository.GetAllAsync(_dbConnection, transaction, CancellationToken.None);

        Assert.NotNull(all);
        Assert.True(all.Count >=2);
        Assert.Contains(all, x => x.UserName == "all_customer_1");
        Assert.Contains(all, x => x.UserName == "all_customer_2");

        transaction.Commit();
    }

    [Fact]
    public async Task UpdateAsync_Should_Modify_Existing_Customer()
    {
        using var transaction = _dbConnection.BeginTransaction();

        var customer = new Domain.Entities.Customer { UserName = "to_update_cust", PersonalName = "Old P", CharacterName = "Old C", Balance =50m };
        var added = await _customersRepository.AddAsync(customer, _dbConnection, transaction, CancellationToken.None);

        added.UserName = "updated_customer";
        added.PersonalName = "New P";
        added.CharacterName = "New C";
        added.Balance =999.99m;

        await _customersRepository.UpdateAsync(added, _dbConnection, transaction, CancellationToken.None);

        var fetched = await _customersRepository.GetByIdAsync(added.Id, _dbConnection, transaction, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("updated_customer", fetched!.UserName);
        Assert.Equal("New P", fetched.PersonalName);
        Assert.Equal("New C", fetched.CharacterName);
        Assert.Equal(999.99m, fetched.Balance);

        transaction.Commit();
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Customer()
    {
        using var transaction = _dbConnection.BeginTransaction();

        var customer = new Domain.Entities.Customer { UserName = "to_delete_cust", PersonalName = "Del P", CharacterName = "Del C", Balance =0m };
        var added = await _customersRepository.AddAsync(customer, _dbConnection, transaction, CancellationToken.None);

        await _customersRepository.DeleteAsync(added.Id, _dbConnection, transaction, CancellationToken.None);

        var fetched = await _customersRepository.GetByIdAsync(added.Id, _dbConnection, transaction, CancellationToken.None);

        Assert.Null(fetched);

        transaction.Commit();
    }

}
