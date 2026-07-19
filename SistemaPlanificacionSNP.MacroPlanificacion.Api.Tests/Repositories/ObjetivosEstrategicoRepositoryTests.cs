using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.MacroPlanificacion.Api.Tests.Common;

namespace SistemaPlanificacionSNP.MacroPlanificacion.Api.Tests.Repositories;

public class ObjetivosEstrategicoRepositoryTests
{
    [Fact]
    public async Task GetPagedAsync_ShouldApplyFiltersSortingAndPaging()
    {
        await using var fixture = new RepositoryFixture();
        await fixture.InitializeAsync();

        var plans = SeedPlans(fixture);
        SeedObjectives(fixture, plans);
        fixture.DbContext.ChangeTracker.Clear();

        var repository = new ObjetivosEstrategicoRepository(fixture.DbContext);
        var query = new MacroObjetivoEstrategicoQueryDto
        {
            PlanNacionalId = plans[0].PlanNacionalId,
            Codigo = "OBJ-",
            Busqueda = "Objetivo",
            PageNumber = 2,
            PageSize = 2,
            SortBy = "Codigo",
            SortDirection = "asc"
        };

        var items = await repository.GetPagedAsync(query);
        var total = await repository.CountFilteredAsync(query);

        total.Should().Be(5);
        items.Should().HaveCount(2);
        items.Select(x => x.Codigo).Should().Equal("OBJ-003", "OBJ-004");
    }

    [Fact]
    public async Task GetByCodigoAndGetByPlanNacionalId_ShouldReturnExpectedResults()
    {
        await using var fixture = new RepositoryFixture();
        await fixture.InitializeAsync();

        var plans = SeedPlans(fixture);
        SeedObjectives(fixture, plans);
        fixture.DbContext.ChangeTracker.Clear();

        var repository = new ObjetivosEstrategicoRepository(fixture.DbContext);

        var byCode = await repository.GetByCodigoAsync(plans[0].PlanNacionalId, "OBJ-002");
        var byPlan = await repository.GetByPlanNacionalIdAsync(plans[0].PlanNacionalId);

        byCode.Should().NotBeNull();
        byCode!.Nombre.Should().Be("Objetivo 02");
        byPlan.Should().HaveCount(5);
        byPlan.Select(x => x.Codigo).Should().Equal("OBJ-001", "OBJ-002", "OBJ-003", "OBJ-004", "OBJ-005");
    }

    [Fact]
    public async Task AddUpdateRemove_ShouldPersistExpectedChanges()
    {
        await using var fixture = new RepositoryFixture();
        await fixture.InitializeAsync();

        var plan = SeedPlans(fixture).First();
        fixture.DbContext.ChangeTracker.Clear();

        var repository = new ObjetivosEstrategicoRepository(fixture.DbContext);
        var entity = new ObjetivosEstrategico
        {
            PlanNacionalId = plan.PlanNacionalId,
            Codigo = "OBJ-999",
            Nombre = "Objetivo CRUD",
            Descripcion = "Inicial"
        };

        await repository.AddAsync(entity);
        await fixture.DbContext.SaveChangesAsync();

        var created = await repository.GetByIdAsync(entity.ObjetivoEstrategicoId);
        created.Should().NotBeNull();
        created!.Nombre.Should().Be("Objetivo CRUD");

        created.Nombre = "Objetivo CRUD actualizado";
        created.Descripcion = "Actualizado";
        await repository.UpdateAsync(created);
        await fixture.DbContext.SaveChangesAsync();

        var updated = await repository.GetByIdAsync(entity.ObjetivoEstrategicoId);
        updated.Should().NotBeNull();
        updated!.Nombre.Should().Be("Objetivo CRUD actualizado");
        updated.Descripcion.Should().Be("Actualizado");

        await repository.RemoveAsync(updated);
        await fixture.DbContext.SaveChangesAsync();

        var deleted = await repository.GetByIdAsync(entity.ObjetivoEstrategicoId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithInvalidForeignKey_ShouldFailOnSaveChanges()
    {
        await using var fixture = new RepositoryFixture();
        await fixture.InitializeAsync();

        var repository = new ObjetivosEstrategicoRepository(fixture.DbContext);
        await repository.AddAsync(new ObjetivosEstrategico
        {
            PlanNacionalId = 9999,
            Codigo = "OBJ-404",
            Nombre = "Objetivo inválido"
        });

        var action = async () => await fixture.DbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    private static List<PlanesNacionalesDesarrollo> SeedPlans(RepositoryFixture fixture)
    {
        var plans = new List<PlanesNacionalesDesarrollo>
        {
            new() { Nombre = "Plan Macro 1", PeriodoInicio = 2020, PeriodoFin = 2025, Estado = "Activo", FechaCreacion = DateTime.UtcNow },
            new() { Nombre = "Plan Macro 2", PeriodoInicio = 2021, PeriodoFin = 2026, Estado = "Suspendido", FechaCreacion = DateTime.UtcNow }
        };

        fixture.DbContext.PlanesNacionalesDesarrollos.AddRange(plans);
        fixture.DbContext.SaveChanges();

        return plans;
    }

    private static void SeedObjectives(RepositoryFixture fixture, IReadOnlyList<PlanesNacionalesDesarrollo> plans)
    {
        fixture.DbContext.ObjetivosEstrategicos.AddRange(
            new ObjetivosEstrategico { PlanNacionalId = plans[0].PlanNacionalId, Codigo = "OBJ-001", Nombre = "Objetivo 01" },
            new ObjetivosEstrategico { PlanNacionalId = plans[0].PlanNacionalId, Codigo = "OBJ-002", Nombre = "Objetivo 02" },
            new ObjetivosEstrategico { PlanNacionalId = plans[0].PlanNacionalId, Codigo = "OBJ-003", Nombre = "Objetivo 03" },
            new ObjetivosEstrategico { PlanNacionalId = plans[0].PlanNacionalId, Codigo = "OBJ-004", Nombre = "Objetivo 04" },
            new ObjetivosEstrategico { PlanNacionalId = plans[0].PlanNacionalId, Codigo = "OBJ-005", Nombre = "Objetivo 05" },
            new ObjetivosEstrategico { PlanNacionalId = plans[1].PlanNacionalId, Codigo = "OBJ-101", Nombre = "Otro objetivo" }
        );
        fixture.DbContext.SaveChanges();
    }

    private sealed class RepositoryFixture : SqliteTestBase
    {
    }
}