using FluentAssertions;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.MacroPlanificacion.Api.Tests.Common;

namespace SistemaPlanificacionSNP.MacroPlanificacion.Api.Tests.Repositories;

public class PlanesNacionalesDesarrolloRepositoryTests
{
    [Fact]
    public async Task GetPagedAsync_ShouldApplyFiltersSortingAndPaging()
    {
        await using var fixture = new RepositoryFixture();
        await fixture.InitializeAsync();

        var plans = SeedPlans(fixture);
        fixture.DbContext.ChangeTracker.Clear();

        var repository = new PlanesNacionalesDesarrolloRepository(fixture.DbContext);
        var query = new MacroPlanNacionalQueryDto
        {
            Estado = "Activo",
            PeriodoInicio = 2020,
            PeriodoFin = 2035,
            Busqueda = "Plan",
            PageNumber = 2,
            PageSize = 3,
            SortBy = "PlanNacionalId",
            SortDirection = "asc"
        };

        var items = await repository.GetPagedAsync(query);
        var total = await repository.CountFilteredAsync(query);

        total.Should().Be(5);
        items.Should().HaveCount(2);
        items.Select(x => x.PlanNacionalId).Should().Equal(plans[6].PlanNacionalId, plans[8].PlanNacionalId);
        items.Select(x => x.Nombre).Should().Equal("Plan 07", "Plan 09");
    }

    [Fact]
    public async Task GetByIdWithObjetivosAsync_ShouldIncludeObjectives()
    {
        await using var fixture = new RepositoryFixture();
        await fixture.InitializeAsync();

        var plan = new PlanesNacionalesDesarrollo
        {
            Nombre = "Plan con objetivos",
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = "Activo",
            FechaCreacion = DateTime.UtcNow
        };

        fixture.DbContext.PlanesNacionalesDesarrollos.Add(plan);
        await fixture.DbContext.SaveChangesAsync();

        fixture.DbContext.ObjetivosEstrategicos.AddRange(
            new ObjetivosEstrategico { PlanNacionalId = plan.PlanNacionalId, Codigo = "OBJ-001", Nombre = "Objetivo 1" },
            new ObjetivosEstrategico { PlanNacionalId = plan.PlanNacionalId, Codigo = "OBJ-002", Nombre = "Objetivo 2" });
        await fixture.DbContext.SaveChangesAsync();

        fixture.DbContext.ChangeTracker.Clear();

        var repository = new PlanesNacionalesDesarrolloRepository(fixture.DbContext);
        var result = await repository.GetByIdWithObjetivosAsync(plan.PlanNacionalId);

        result.Should().NotBeNull();
        result!.ObjetivosEstrategicos.Should().HaveCount(2);
        result.ObjetivosEstrategicos.Select(x => x.Codigo).Should().ContainInOrder("OBJ-001", "OBJ-002");
    }

    [Fact]
    public async Task CountMethods_ShouldReturnGroupedAggregates()
    {
        await using var fixture = new RepositoryFixture();
        await fixture.InitializeAsync();

        SeedPlans(fixture);
        fixture.DbContext.ChangeTracker.Clear();

        var repository = new PlanesNacionalesDesarrolloRepository(fixture.DbContext);

        var total = await repository.CountTotalAsync();
        var byState = await repository.GetConteoPorEstadoAsync();
        var byVigency = await repository.GetConteoPorVigenciaAsync();

        total.Should().Be(10);
        byState.Should().ContainSingle(x => x.Estado == "Activo" && x.Total == 5);
        byState.Should().ContainSingle(x => x.Estado == "Suspendido" && x.Total == 5);
        byVigency.Should().HaveCount(10);
    }

    [Fact]
    public async Task AddUpdateRemove_ShouldPersistExpectedChanges()
    {
        await using var fixture = new RepositoryFixture();
        await fixture.InitializeAsync();

        var repository = new PlanesNacionalesDesarrolloRepository(fixture.DbContext);
        var entity = new PlanesNacionalesDesarrollo
        {
            Nombre = "Plan CRUD",
            PeriodoInicio = 2031,
            PeriodoFin = 2035,
            Estado = "Activo",
            FechaCreacion = DateTime.UtcNow
        };

        await repository.AddAsync(entity);
        await fixture.DbContext.SaveChangesAsync();

        var created = await repository.GetByIdAsync(entity.PlanNacionalId);
        created.Should().NotBeNull();
        created!.Nombre.Should().Be("Plan CRUD");

        created.Nombre = "Plan CRUD actualizado";
        await repository.UpdateAsync(created);
        await fixture.DbContext.SaveChangesAsync();

        var updated = await repository.GetByIdAsync(entity.PlanNacionalId);
        updated.Should().NotBeNull();
        updated!.Nombre.Should().Be("Plan CRUD actualizado");

        await repository.RemoveAsync(updated);
        await fixture.DbContext.SaveChangesAsync();

        var deleted = await repository.GetByIdAsync(entity.PlanNacionalId);
        deleted.Should().BeNull();
    }

    private static List<PlanesNacionalesDesarrollo> SeedPlans(RepositoryFixture fixture)
    {
        var plans = Enumerable.Range(1, 10)
            .Select(index => new PlanesNacionalesDesarrollo
            {
                Nombre = $"Plan {index:00}",
                PeriodoInicio = 2020 + index,
                PeriodoFin = 2025 + index,
                Estado = index % 2 == 1 ? "Activo" : "Suspendido",
                FechaCreacion = DateTime.UtcNow.AddDays(-index)
            })
            .ToList();

        fixture.DbContext.PlanesNacionalesDesarrollos.AddRange(plans);
        fixture.DbContext.SaveChanges();

        return plans;
    }

    private sealed class RepositoryFixture : SqliteTestBase
    {
    }
}