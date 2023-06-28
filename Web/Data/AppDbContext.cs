using ADF.Web.Data.Configurations;
using ADF.Web.Models;
using ADF.Web.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;

namespace ADF.Web.Data;

public class AppDbContext : DbContext, IUnitOfWork
{
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        new PipelineConfiguration().Configure(builder.Entity<Pipeline>());

        base.OnModelCreating(builder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }

    public override int SaveChanges()
    {
        return base.SaveChanges();
    }

    private IDbContextTransaction _transaction;
    public IDbContextTransaction GetCurrentTransaction() => _transaction;
    public bool HasActiveTransaction => _transaction != null;

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_transaction != null) return null!;

        _transaction = await Database.BeginTransactionAsync();

        return _transaction;
    }

    public async Task CommitAsync(IDbContextTransaction? transaction=null)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction != _transaction) throw new InvalidOperationException($"Transaction {transaction.TransactionId} is not current");

        try
        {
            await SaveChangesAsync();
            transaction.Commit();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null!;
            }
        }
    }
    private void RollbackTransaction()
    {
        try
        {
            _transaction?.Rollback();
        }
        finally
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null!;
            }
        }
    }
}