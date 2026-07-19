using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Infrastructure.Data;

namespace SistemaPlanificacionSNP.MacroPlanificacion.Api.Tests.Common;

public abstract class SqliteTestBase : IAsyncDisposable
{
    public readonly SqliteConnection Connection;
    public readonly MacroPlanificacionDbContext DbContext;

    protected SqliteTestBase()
    {
        Connection = new SqliteConnection("Data Source=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<MacroPlanificacionDbContext>()
            .UseSqlite(Connection)
            .EnableSensitiveDataLogging()
            .Options;

        DbContext = new SqliteMacroPlanificacionDbContextForTests(options);
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

    private sealed class SqliteMacroPlanificacionDbContextForTests : MacroPlanificacionDbContext
    {
        public SqliteMacroPlanificacionDbContextForTests(DbContextOptions<MacroPlanificacionDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
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