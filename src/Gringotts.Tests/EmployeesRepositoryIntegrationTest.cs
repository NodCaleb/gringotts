using Dapper;
using Gringotts.Infrastructure.Contracts;
using Gringotts.Infrastructure.Repositories;
using Npgsql;
using System.Data;

namespace Gringotts.Tests;

public class EmployeesRepositoryIntegrationTest : IDisposable, IClassFixture<DbFixture>
{
    private readonly IDbConnection _dbConnection;

    private readonly IEmployeesRepository _employeesRepository;

    public EmployeesRepositoryIntegrationTest(DbFixture dbFixture)
    {
        _employeesRepository = new PostgreEmployeesRepository();
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

        var employee = new Domain.Entities.Employee
        {
            UserName = "test_user_add",
            AccessCode = 111
        };

        var added = await _employeesRepository.AddAsync(employee, _dbConnection, insertTransaction, CancellationToken.None);

        Assert.NotNull(added);
        Assert.NotEqual(Guid.Empty, added.Id);

        var fetched = await _employeesRepository.GetByIdAsync(added.Id, _dbConnection, insertTransaction, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("test_user_add", fetched!.UserName);
        Assert.Equal(111, fetched.AccessCode);

        insertTransaction.Commit();
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Inserted()
    {
        using var transaction = _dbConnection.BeginTransaction();

        var e1 = new Domain.Entities.Employee { UserName = "all_user_1", AccessCode = 201 };
        var e2 = new Domain.Entities.Employee { UserName = "all_user_2", AccessCode = 202 };

        await _employeesRepository.AddAsync(e1, _dbConnection, transaction, CancellationToken.None);
        await _employeesRepository.AddAsync(e2, _dbConnection, transaction, CancellationToken.None);

        var all = await _employeesRepository.GetAllAsync(_dbConnection, transaction, CancellationToken.None);

        Assert.NotNull(all);
        Assert.True(all.Count >= 2);
        Assert.Contains(all, x => x.UserName == "all_user_1");
        Assert.Contains(all, x => x.UserName == "all_user_2");

        transaction.Commit();
    }

    [Fact]
    public async Task UpdateAsync_Should_Modify_Existing_Employee()
    {
        using var transaction = _dbConnection.BeginTransaction();

        var employee = new Domain.Entities.Employee { UserName = "to_update", AccessCode = 301 };
        var added = await _employeesRepository.AddAsync(employee, _dbConnection, transaction, CancellationToken.None);

        added.UserName = "updated_name";
        added.AccessCode = 999;

        await _employeesRepository.UpdateAsync(added, _dbConnection, transaction, CancellationToken.None);

        var fetched = await _employeesRepository.GetByIdAsync(added.Id, _dbConnection, transaction, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("updated_name", fetched!.UserName);
        Assert.Equal(999, fetched.AccessCode);

        transaction.Commit();
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Employee()
    {
        using var transaction = _dbConnection.BeginTransaction();

        var employee = new Domain.Entities.Employee { UserName = "to_delete", AccessCode = 401 };
        var added = await _employeesRepository.AddAsync(employee, _dbConnection, transaction, CancellationToken.None);

        await _employeesRepository.DeleteAsync(added.Id, _dbConnection, transaction, CancellationToken.None);

        var fetched = await _employeesRepository.GetByIdAsync(added.Id, _dbConnection, transaction, CancellationToken.None);

        Assert.Null(fetched);

        transaction.Commit();
    }
}
