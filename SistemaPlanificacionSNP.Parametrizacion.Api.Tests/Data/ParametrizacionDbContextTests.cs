using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Parametrizacion;
using SistemaPlanificacionSNP.Parametrizacion.Api.Tests.Common;

namespace SistemaPlanificacionSNP.Parametrizacion.Api.Tests.Data;

public class ParametrizacionDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldRejectDuplicateCatalogoCodigo()
    {
        await using var fixture = new DbContextFixture();
        await fixture.InitializeAsync();

        fixture.DbContext.Catalogos.AddRange(
            BuildCatalogo("ESTADOS", "Estados"),
            BuildCatalogo("ESTADOS", "Estados duplicado"));

        var action = async () => await fixture.DbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldRejectDuplicatePeriodoCodigo()
    {
        await using var fixture = new DbContextFixture();
        await fixture.InitializeAsync();

        fixture.DbContext.PeriodosPlanificacion.AddRange(
            BuildPeriodo("P2025", "Periodo 2025"),
            BuildPeriodo("P2025", "Periodo 2025 duplicado"));

        var action = async () => await fixture.DbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldRejectDuplicateEntidadCodigo()
    {
        await using var fixture = new DbContextFixture();
        await fixture.InitializeAsync();

        var periodo = BuildPeriodo("P2025", "Periodo 2025");
        fixture.DbContext.PeriodosPlanificacion.Add(periodo);
        await fixture.DbContext.SaveChangesAsync();

        fixture.DbContext.EntidadesPublicas.AddRange(
            BuildEntidad(periodo.PeriodoPlanificacionId, "ENT-001", "Entidad A", "EA"),
            BuildEntidad(periodo.PeriodoPlanificacionId, "ENT-001", "Entidad B", "EB"));

        var action = async () => await fixture.DbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldRejectItemCatalogoWithMissingCatalogo()
    {
        await using var fixture = new DbContextFixture();
        await fixture.InitializeAsync();

        fixture.DbContext.ItemsCatalogo.Add(new ItemCatalogo
        {
            CatalogoId = 999,
            Codigo = "ACTIVO",
            Nombre = "Activo",
            Orden = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });

        var action = async () => await fixture.DbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldRejectEntidadPublicaWithMissingPeriodo()
    {
        await using var fixture = new DbContextFixture();
        await fixture.InitializeAsync();

        fixture.DbContext.EntidadesPublicas.Add(BuildEntidad(999, "ENT-404", "Entidad inválida", "EI"));

        var action = async () => await fixture.DbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task DeletePeriodo_ShouldFail_WhenPeriodoHasEntidadesPublicas()
    {
        await using var fixture = new DbContextFixture();
        await fixture.InitializeAsync();

        var periodo = BuildPeriodo("P2025", "Periodo 2025");
        fixture.DbContext.PeriodosPlanificacion.Add(periodo);
        await fixture.DbContext.SaveChangesAsync();

        fixture.DbContext.EntidadesPublicas.Add(BuildEntidad(periodo.PeriodoPlanificacionId, "ENT-001", "Entidad A", "EA"));
        await fixture.DbContext.SaveChangesAsync();
        fixture.DbContext.ChangeTracker.Clear();

        var persistedPeriodo = await fixture.DbContext.PeriodosPlanificacion
            .SingleAsync(x => x.PeriodoPlanificacionId == periodo.PeriodoPlanificacionId);

        fixture.DbContext.PeriodosPlanificacion.Remove(persistedPeriodo);
        var action = async () => await fixture.DbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistCatalogoWithItems_WhenRelationshipIsValid()
    {
        await using var fixture = new DbContextFixture();
        await fixture.InitializeAsync();

        var catalogo = BuildCatalogo("ESTADOS", "Estados");
        catalogo.Items.Add(new ItemCatalogo
        {
            Codigo = "ACTIVO",
            Nombre = "Activo",
            Orden = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });

        fixture.DbContext.Catalogos.Add(catalogo);
        await fixture.DbContext.SaveChangesAsync();
        fixture.DbContext.ChangeTracker.Clear();

        var persisted = await fixture.DbContext.Catalogos
            .Include(x => x.Items)
            .SingleAsync(x => x.Codigo == "ESTADOS");

        persisted.Items.Should().ContainSingle(x => x.Codigo == "ACTIVO");
    }

    private static Catalogo BuildCatalogo(string codigo, string nombre)
    {
        return new Catalogo
        {
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = nombre,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
    }

    private static PeriodoPlanificacion BuildPeriodo(string codigo, string nombre)
    {
        return new PeriodoPlanificacion
        {
            Codigo = codigo,
            Nombre = nombre,
            FechaInicio = new DateTime(2025, 1, 1),
            FechaFin = new DateTime(2025, 12, 31),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
    }

    private static EntidadPublica BuildEntidad(int periodoId, string codigo, string nombre, string sigla)
    {
        return new EntidadPublica
        {
            PeriodoPlanificacionId = periodoId,
            Codigo = codigo,
            Nombre = nombre,
            Sigla = sigla,
            Mision = "Misión de prueba",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
    }

    private sealed class DbContextFixture : SqliteTestBase
    {
    }
}