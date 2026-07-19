using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SistemaPlanificacionSNP.Domain.Entities.Parametrizacion;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Mapping;
using SistemaPlanificacionSNP.Parametrizacion.Api.Services;
using SistemaPlanificacionSNP.Parametrizacion.Api.Tests.Common;

namespace SistemaPlanificacionSNP.Parametrizacion.Api.Tests.Services;

public class ParametrizacionServiceTests
{
    [Fact]
    public async Task GetCatalogosAsync_ShouldReturnCatalogosWithOnlyActiveItemsOrderedByOrden()
    {
        await using var fixture = new ServiceFixture();
        await fixture.InitializeAsync();

        var catalogo = new Catalogo
        {
            Codigo = "ESTADOS",
            Nombre = "Estados",
            Descripcion = "Estados de prueba",
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            Items = new List<ItemCatalogo>
            {
                BuildItem("INACTIVO", "Inactivo", 1, activo: false),
                BuildItem("B", "Segundo", 2, activo: true),
                BuildItem("A", "Primero", 1, activo: true)
            }
        };

        fixture.DbContext.Catalogos.Add(catalogo);
        await fixture.DbContext.SaveChangesAsync();
        fixture.DbContext.ChangeTracker.Clear();

        var service = fixture.CreateService();

        var result = await service.GetCatalogosAsync();

        result.Should().ContainSingle();
        result[0].Codigo.Should().Be("ESTADOS");
        result[0].Items.Should().HaveCount(2);
        result[0].Items.Select(x => x.Codigo).Should().Equal("A", "B");
        result[0].Items.Should().NotContain(x => x.Codigo == "INACTIVO");
    }

    [Fact]
    public async Task GetCatalogoByCodigoAsync_ShouldReturnNull_WhenCatalogoDoesNotExist()
    {
        await using var fixture = new ServiceFixture();
        await fixture.InitializeAsync();

        var service = fixture.CreateService();

        var result = await service.GetCatalogoByCodigoAsync("NO_EXISTE");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateCatalogoAsync_ShouldPersistCatalogoWithActivoAndFechaCreacion()
    {
        await using var fixture = new ServiceFixture();
        await fixture.InitializeAsync();

        var service = fixture.CreateService();
        var dto = new CatalogoCreateDto
        {
            Codigo = "TIPOS",
            Nombre = "Tipos",
            Descripcion = "Tipos de entidad"
        };

        var result = await service.CreateCatalogoAsync(dto);

        result.CatalogoId.Should().BeGreaterThan(0);
        result.Codigo.Should().Be("TIPOS");
        result.Activo.Should().BeTrue();
        result.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        var persisted = await fixture.DbContext.Catalogos.FindAsync(result.CatalogoId);
        persisted.Should().NotBeNull();
        persisted!.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCatalogoAsync_ShouldThrow_WhenCodigoAlreadyExists()
    {
        await using var fixture = new ServiceFixture();
        await fixture.InitializeAsync();

        fixture.DbContext.Catalogos.Add(new Catalogo
        {
            Codigo = "ESTADOS",
            Nombre = "Estados",
            Descripcion = "Existente",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });
        await fixture.DbContext.SaveChangesAsync();

        var service = fixture.CreateService();
        var dto = new CatalogoCreateDto
        {
            Codigo = "ESTADOS",
            Nombre = "Estados duplicados",
            Descripcion = "Duplicado"
        };

        var action = async () => await service.CreateCatalogoAsync(dto);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Ya existe un catálogo con este código.");
    }

    [Fact]
    public async Task CreateItemCatalogoAsync_ShouldThrow_WhenCatalogoDoesNotExist()
    {
        await using var fixture = new ServiceFixture();
        await fixture.InitializeAsync();

        var service = fixture.CreateService();
        var dto = new ItemCatalogoCreateDto
        {
            CatalogoId = 999,
            Codigo = "ACTIVO",
            Nombre = "Activo",
            Orden = 1
        };

        var action = async () => await service.CreateItemCatalogoAsync(dto);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("El catálogo padre no existe.");
    }

    [Fact]
    public async Task CreateItemCatalogoAsync_ShouldPersistItemWithActivoAndFechaCreacion()
    {
        await using var fixture = new ServiceFixture();
        await fixture.InitializeAsync();

        var catalogo = new Catalogo
        {
            Codigo = "ESTADOS",
            Nombre = "Estados",
            Descripcion = "Estados de prueba",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        fixture.DbContext.Catalogos.Add(catalogo);
        await fixture.DbContext.SaveChangesAsync();

        var service = fixture.CreateService();
        var dto = new ItemCatalogoCreateDto
        {
            CatalogoId = catalogo.CatalogoId,
            Codigo = "ACTIVO",
            Nombre = "Activo",
            Descripcion = "Estado activo",
            Orden = 1
        };

        var result = await service.CreateItemCatalogoAsync(dto);

        result.ItemCatalogoId.Should().BeGreaterThan(0);
        result.CatalogoId.Should().Be(catalogo.CatalogoId);
        result.Activo.Should().BeTrue();

        var persisted = await fixture.DbContext.ItemsCatalogo.FindAsync(result.ItemCatalogoId);
        persisted.Should().NotBeNull();
        persisted!.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task GetPeriodosAsync_ShouldReturnOrderedByFechaInicioDescending()
    {
        await using var fixture = new ServiceFixture();
        await fixture.InitializeAsync();

        fixture.DbContext.PeriodosPlanificacion.AddRange(
            BuildPeriodo("P2020", "Periodo 2020", new DateTime(2020, 1, 1), new DateTime(2020, 12, 31)),
            BuildPeriodo("P2025", "Periodo 2025", new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)),
            BuildPeriodo("P2023", "Periodo 2023", new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)));
        await fixture.DbContext.SaveChangesAsync();
        fixture.DbContext.ChangeTracker.Clear();

        var service = fixture.CreateService();

        var result = await service.GetPeriodosAsync();

        result.Select(x => x.Codigo).Should().Equal("P2025", "P2023", "P2020");
    }

    [Fact]
    public async Task CreatePeriodoAsync_ShouldPersistPeriodoWithFechaCreacion()
    {
        await using var fixture = new ServiceFixture();
        await fixture.InitializeAsync();

        var service = fixture.CreateService();
        var dto = new PeriodoPlanificacionCreateUpdateDto
        {
            Codigo = "P2030",
            Nombre = "Periodo 2030",
            FechaInicio = new DateTime(2030, 1, 1),
            FechaFin = new DateTime(2030, 12, 31),
            Activo = true
        };

        var result = await service.CreatePeriodoAsync(dto);

        result.PeriodoPlanificacionId.Should().BeGreaterThan(0);
        result.Codigo.Should().Be("P2030");

        var persisted = await fixture.DbContext.PeriodosPlanificacion.FindAsync(result.PeriodoPlanificacionId);
        persisted.Should().NotBeNull();
        persisted!.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task CreateEntidadAsync_ShouldGenerateCodigoAndUsePeriodoIdOne_WhenDefaultsAreMissing()
    {
        await using var fixture = new ServiceFixture();
        await fixture.InitializeAsync();

        fixture.DbContext.PeriodosPlanificacion.Add(BuildPeriodo("PBASE", "Periodo base", new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)));
        await fixture.DbContext.SaveChangesAsync();

        var service = fixture.CreateService();
        var dto = new EntidadPublicaCreateUpdateDto
        {
            Nombre = "Ministerio de Prueba",
            Sigla = "MDP",
            Tipo = "Ministerio",
            NivelGobierno = "Central"
        };

        var result = await service.CreateEntidadAsync(dto);

        result.EntidadPublicaId.Should().BeGreaterThan(0);
        result.Nombre.Should().Be("Ministerio de Prueba");
        result.Sigla.Should().Be("MDP");

        var persisted = await fixture.DbContext.EntidadesPublicas.FindAsync(result.EntidadPublicaId);
        persisted.Should().NotBeNull();
        persisted!.PeriodoPlanificacionId.Should().Be(1);
        persisted.Codigo.Should().HaveLength(8);
        persisted.Activo.Should().BeTrue();
        persisted.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task GetEntidadesAsync_ShouldReturnEntidadesOrderedByNombre()
    {
        await using var fixture = new ServiceFixture();
        await fixture.InitializeAsync();

        var periodo = BuildPeriodo("PBASE", "Periodo base", new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));
        fixture.DbContext.PeriodosPlanificacion.Add(periodo);
        await fixture.DbContext.SaveChangesAsync();

        fixture.DbContext.EntidadesPublicas.AddRange(
            BuildEntidad(periodo.PeriodoPlanificacionId, "Zeta Pública", "ZP"),
            BuildEntidad(periodo.PeriodoPlanificacionId, "Alfa Pública", "AP"));
        await fixture.DbContext.SaveChangesAsync();
        fixture.DbContext.ChangeTracker.Clear();

        var service = fixture.CreateService();

        var result = await service.GetEntidadesAsync();

        result.Should().HaveCount(2);
        result.Select(x => x.Nombre).Should().Equal("Alfa Pública", "Zeta Pública");
    }

    private static ItemCatalogo BuildItem(string codigo, string nombre, int orden, bool activo)
    {
        return new ItemCatalogo
        {
            Codigo = codigo,
            Nombre = nombre,
            Orden = orden,
            Activo = activo,
            FechaCreacion = DateTime.UtcNow
        };
    }

    private static PeriodoPlanificacion BuildPeriodo(string codigo, string nombre, DateTime fechaInicio, DateTime fechaFin)
    {
        return new PeriodoPlanificacion
        {
            Codigo = codigo,
            Nombre = nombre,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
    }

    private static EntidadPublica BuildEntidad(int periodoId, string nombre, string sigla)
    {
        return new EntidadPublica
        {
            Codigo = Guid.NewGuid().ToString("N")[..8],
            Nombre = nombre,
            Sigla = sigla,
            Mision = "Misión de prueba",
            PeriodoPlanificacionId = periodoId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
    }

    private sealed class ServiceFixture : SqliteTestBase
    {
        private readonly IMapper _mapper;

        public ServiceFixture()
        {
            var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<MappingProfile>();
                },
                NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }

        public ParametrizacionService CreateService()
        {
            return new ParametrizacionService(DbContext, _mapper);
        }
    }
}