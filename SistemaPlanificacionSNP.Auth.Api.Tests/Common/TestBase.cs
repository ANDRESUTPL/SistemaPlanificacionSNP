using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.Mapping;

namespace SistemaPlanificacionSNP.Auth.Api.Tests.Common;

public abstract class TestBase : IDisposable
{
    protected readonly AuthDbContext DbContext;
    protected readonly IMapper Mapper;

    protected TestBase()
    {
        Mapper = CreateMapper();
        DbContext = CreateInMemoryContext();
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            },
            NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }

    private static AuthDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase($"AuthApiTests_{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        return new TestAuthDbContext(options);
    }

    public virtual void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TestAuthDbContext : AuthDbContext
    {
        public TestAuthDbContext(DbContextOptions<AuthDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Prevents the SQL Server hardcoded config from AuthDbContext in tests.
        }
    }
}
