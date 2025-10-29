using Dapper;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Infrastructure.Repositories;
using Npgsql;
using System.Data;

namespace Gringotts.Infrastructure.IntegrationTests;

public class TransactionsRepositoryIntegrationTest : IDisposable, IClassFixture<DbFixture>
{
    private readonly IDbConnection _dbConnection;

    private readonly ITransactionsRepository _transactionsRepository;

    private readonly ICustomersRepository _customersRepository;

    public TransactionsRepositoryIntegrationTest(DbFixture dbFixture)
    {
        _transactionsRepository = new PostgreTransactionsRepository();
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
        using var tx = _dbConnection.BeginTransaction();

        // create recipient and optional sender customers
        var recipient = new Domain.Entities.Customer
        {
            UserName = "tx_recipient",
            PersonalName = "Rec P",
            CharacterName = "Rec C",
            Balance =50m
        };

        var sender = new Domain.Entities.Customer
        {
            UserName = "tx_sender",
            PersonalName = "Send P",
            CharacterName = "Send C",
            Balance =100m
        };

        var addedRecipient = await _customersRepository.AddAsync(recipient, _dbConnection, tx, CancellationToken.None);
        var addedSender = await _customersRepository.AddAsync(sender, _dbConnection, tx, CancellationToken.None);

        var transaction = new Domain.Entities.Transaction
        {
            Date = DateTime.UtcNow,
            SenderId = addedSender.Id,
            RecipientId = addedRecipient.Id,
            Amount =12.34m,
            Description = "integration test add"
        };

        var addedTx = await _transactionsRepository.AddAsync(transaction, _dbConnection, tx, CancellationToken.None);

        Assert.NotNull(addedTx);
        Assert.NotEqual(Guid.Empty, addedTx.Id);

        var fetched = await _transactionsRepository.GetByIdAsync(addedTx.Id, _dbConnection, tx, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal(addedTx.Id, fetched!.Id);
        Assert.Equal(transaction.Amount, fetched.Amount);
        Assert.Equal(transaction.Description, fetched.Description);
        Assert.Equal(transaction.RecipientId, fetched.RecipientId);
        Assert.Equal(transaction.SenderId, fetched.SenderId);

        tx.Commit();
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Inserted()
    {
        using var tx = _dbConnection.BeginTransaction();

        var r = new Domain.Entities.Customer { UserName = "all_tx_rec", PersonalName = "P1", CharacterName = "C1", Balance =0m };
        var s = new Domain.Entities.Customer { UserName = "all_tx_snd", PersonalName = "P2", CharacterName = "C2", Balance =0m };

        var ar = await _customersRepository.AddAsync(r, _dbConnection, tx, CancellationToken.None);
        var asd = await _customersRepository.AddAsync(s, _dbConnection, tx, CancellationToken.None);

        var t1 = new Domain.Entities.Transaction { Date = DateTime.UtcNow, SenderId = asd.Id, RecipientId = ar.Id, Amount =1m, Description = "t1" };
        var t2 = new Domain.Entities.Transaction { Date = DateTime.UtcNow.AddMinutes(1), SenderId = asd.Id, RecipientId = ar.Id, Amount =2m, Description = "t2" };

        await _transactionsRepository.AddAsync(t1, _dbConnection, tx, CancellationToken.None);
        await _transactionsRepository.AddAsync(t2, _dbConnection, tx, CancellationToken.None);

        var all = await _transactionsRepository.GetAllAsync(_dbConnection, tx, CancellationToken.None);

        Assert.NotNull(all);
        Assert.True(all.Count >=2);
        Assert.Contains(all, x => x.Description == "t1");
        Assert.Contains(all, x => x.Description == "t2");

        tx.Commit();
    }

    [Fact]
    public async Task UpdateAsync_Should_Modify_Existing_Transaction()
    {
        using var tx = _dbConnection.BeginTransaction();

        var r = new Domain.Entities.Customer { UserName = "upd_tx_rec", PersonalName = "P", CharacterName = "C", Balance =0m };
        var s = new Domain.Entities.Customer { UserName = "upd_tx_snd", PersonalName = "P", CharacterName = "C", Balance =0m };

        var ar = await _customersRepository.AddAsync(r, _dbConnection, tx, CancellationToken.None);
        var asd = await _customersRepository.AddAsync(s, _dbConnection, tx, CancellationToken.None);

        var t = new Domain.Entities.Transaction { Date = DateTime.UtcNow, SenderId = asd.Id, RecipientId = ar.Id, Amount =5m, Description = "to_update" };
        var added = await _transactionsRepository.AddAsync(t, _dbConnection, tx, CancellationToken.None);

        added.Amount =99.99m;
        added.Description = "updated_desc";

        await _transactionsRepository.UpdateAsync(added, _dbConnection, tx, CancellationToken.None);

        var fetched = await _transactionsRepository.GetByIdAsync(added.Id, _dbConnection, tx, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal(99.99m, fetched!.Amount);
        Assert.Equal("updated_desc", fetched.Description);

        tx.Commit();
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Transaction()
    {
        using var tx = _dbConnection.BeginTransaction();

        var r = new Domain.Entities.Customer { UserName = "del_tx_rec", PersonalName = "P", CharacterName = "C", Balance =0m };
        var s = new Domain.Entities.Customer { UserName = "del_tx_snd", PersonalName = "P", CharacterName = "C", Balance =0m };

        var ar = await _customersRepository.AddAsync(r, _dbConnection, tx, CancellationToken.None);
        var asd = await _customersRepository.AddAsync(s, _dbConnection, tx, CancellationToken.None);

        var t = new Domain.Entities.Transaction { Date = DateTime.UtcNow, SenderId = asd.Id, RecipientId = ar.Id, Amount =3m, Description = "to_delete" };
        var added = await _transactionsRepository.AddAsync(t, _dbConnection, tx, CancellationToken.None);

        await _transactionsRepository.DeleteAsync(added.Id, _dbConnection, tx, CancellationToken.None);

        var fetched = await _transactionsRepository.GetByIdAsync(added.Id, _dbConnection, tx, CancellationToken.None);

        Assert.Null(fetched);

        tx.Commit();
    }

}
