using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Infrastructure.Data;

namespace SistemaPlanificacionSNP.Parametrizacion.Api.Tests.Common;

public abstract class SqliteTestBase : IAsyncDisposable
{
    public readonly SqliteConnection Connection;
    public readonly ParametrizacionDbContext DbContext;

    protected SqliteTestBase()
    {
        Connection = new SqliteConnection("Data Source=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<ParametrizacionDbContext>()
            .UseSqlite(Connection)
            .EnableSensitiveDataLogging()
            .Options;

        DbContext = new SqliteParametrizacionDbContextForTests(options);
    }

    public async Task InitializeAsync()
    {
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await Connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private sealed class SqliteParametrizacionDbContextForTests : ParametrizacionDbContext
    {
        public SqliteParametrizacionDbContextForTests(DbContextOptions<ParametrizacionDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (!string.IsNullOrWhiteSpace(property.GetDefaultValueSql()))
                    {
                        property.SetDefaultValueSql(null);
                    }
                }
            }
        }
    }
}