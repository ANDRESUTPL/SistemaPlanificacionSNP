using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.Mapping;

namespace SistemaPlanificacionSNP.MacroPlanificacion.Api.Tests.Common;

public abstract class TestBase : IDisposable
{
    protected readonly MacroPlanificacionDbContext DbContext;
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

    private static MacroPlanificacionDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MacroPlanificacionDbContext>()
            .UseInMemoryDatabase($"MacroPlanificacionApiTests_{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        return new TestMacroPlanificacionDbContext(options);
    }

    public virtual void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TestMacroPlanificacionDbContext : MacroPlanificacionDbContext
    {
        public TestMacroPlanificacionDbContext(DbContextOptions<MacroPlanificacionDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }
}