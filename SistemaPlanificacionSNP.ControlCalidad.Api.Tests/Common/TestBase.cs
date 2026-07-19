using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.Mapping;

namespace SistemaPlanificacionSNP.ControlCalidad.Api.Tests.Common;

public abstract class TestBase : IDisposable
{
    protected readonly ControlCalidadDbContext DbContext;
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

    private static ControlCalidadDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ControlCalidadDbContext>()
            .UseInMemoryDatabase($"ControlCalidadApiTests_{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        return new ControlCalidadDbContext(options);
    }

    public virtual void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
