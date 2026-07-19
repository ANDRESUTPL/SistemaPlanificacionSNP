using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.Mapping;

namespace SistemaPlanificacionSNP.Parametrizacion.Api.Tests.Common;

public abstract class TestBase : IDisposable
{
    protected readonly ParametrizacionDbContext DbContext;
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

        return config.CreateMapper();
    }

    private static ParametrizacionDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ParametrizacionDbContext>()
            .UseInMemoryDatabase($"ParametrizacionApiTests_{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        return new ParametrizacionDbContext(options);
    }

    public virtual void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}