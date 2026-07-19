using FluentAssertions;
using Moq;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.MacroPlanificacion.Api.Tests.Services;

public class MacroPlanNacionalServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreatePlan_WhenDtoIsValid()
    {
        var unitOfWorkMock = BuildUnitOfWork();
        var service = new MacroPlanNacionalService(unitOfWorkMock.Object);

        var dto = new MacroPlanNacionalCreateDto
        {
            Nombre = " Plan Nacional 2030 ",
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = " Activo "
        };

        var result = await service.CreateAsync(dto, "user-123");

        result.Nombre.Should().Be("Plan Nacional 2030");
        result.Estado.Should().Be("Activo");
        result.PeriodoInicio.Should().Be(2025);
        result.PeriodoFin.Should().Be(2030);
        result.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        unitOfWorkMock.Verify(u => u.PlanesNacionales.AddAsync(It.Is<PlanesNacionalesDesarrollo>(x =>
            x.Nombre == "Plan Nacional 2030" &&
            x.Estado == "Activo" &&
            x.PeriodoInicio == 2025 &&
            x.PeriodoFin == 2030)), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenActorIsMissing()
    {
        var service = new MacroPlanNacionalService(BuildUnitOfWork().Object);
        var dto = new MacroPlanNacionalCreateDto
        {
            Nombre = "Plan",
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = "Activo"
        };

        var action = async () => await service.CreateAsync(dto, " ");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se pudo determinar la identidad del usuario desde los claims");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenPeriodoInicioIsGreaterThanPeriodoFin()
    {
        var service = new MacroPlanNacionalService(BuildUnitOfWork().Object);
        var dto = new MacroPlanNacionalCreateDto
        {
            Nombre = "Plan",
            PeriodoInicio = 2031,
            PeriodoFin = 2030,
            Estado = "Activo"
        };

        var action = async () => await service.CreateAsync(dto, "user-123");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("PeriodoInicio no puede ser mayor a PeriodoFin");
    }

    [Fact]
    public async Task UpdateAsync_ShouldApplyPartialChanges_AndKeepFechaCreacion()
    {
        var existing = new PlanesNacionalesDesarrollo
        {
            PlanNacionalId = 10,
            Nombre = "Plan original",
            PeriodoInicio = 2020,
            PeriodoFin = 2025,
            Estado = "Activo",
            FechaCreacion = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc)
        };

        var unitOfWorkMock = BuildUnitOfWork(planById: existing);
        var service = new MacroPlanNacionalService(unitOfWorkMock.Object);

        var dto = new MacroPlanNacionalUpdateDto
        {
            Nombre = null,
            PeriodoFin = 2030,
            Estado = " Suspendido "
        };

        var result = await service.UpdateAsync(10, dto, "user-123");

        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Plan original");
        result.PeriodoInicio.Should().Be(2020);
        result.PeriodoFin.Should().Be(2030);
        result.Estado.Should().Be("Suspendido");
        result.FechaCreacion.Should().Be(existing.FechaCreacion);

        unitOfWorkMock.Verify(u => u.PlanesNacionales.UpdateAsync(existing), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenPlanDoesNotExist()
    {
        var unitOfWorkMock = BuildUnitOfWork();
        var service = new MacroPlanNacionalService(unitOfWorkMock.Object);

        var result = await service.UpdateAsync(404, new MacroPlanNacionalUpdateDto { Nombre = "Nuevo" }, "user-123");

        result.Should().BeNull();
        unitOfWorkMock.Verify(u => u.PlanesNacionales.UpdateAsync(It.IsAny<PlanesNacionalesDesarrollo>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenPlanHasObjectives()
    {
        var existing = new PlanesNacionalesDesarrollo
        {
            PlanNacionalId = 10,
            Nombre = "Plan con objetivos",
            PeriodoInicio = 2020,
            PeriodoFin = 2025,
            Estado = "Activo",
            ObjetivosEstrategicos = new List<ObjetivosEstrategico>
            {
                new() { ObjetivoEstrategicoId = 1, PlanNacionalId = 10, Codigo = "OBJ-1", Nombre = "Objetivo 1" }
            }
        };

        var unitOfWorkMock = BuildUnitOfWork(planWithObjectives: existing);
        var service = new MacroPlanNacionalService(unitOfWorkMock.Object);

        var action = async () => await service.DeleteAsync(10, "user-123");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se puede eliminar el plan porque tiene objetivos asociados");

        unitOfWorkMock.Verify(u => u.PlanesNacionales.RemoveAsync(It.IsAny<PlanesNacionalesDesarrollo>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetResumenAsync_ShouldReturnAggregatedValuesFromRepositories()
    {
        var unitOfWorkMock = BuildUnitOfWork();
        unitOfWorkMock.Setup(u => u.PlanesNacionales.CountTotalAsync()).ReturnsAsync(7);
        unitOfWorkMock.Setup(u => u.ObjetivosEstrategicos.CountTotalAsync()).ReturnsAsync(19);
        unitOfWorkMock.Setup(u => u.PlanesNacionales.GetConteoPorEstadoAsync()).ReturnsAsync(new List<MacroConteoEstadoDto>
        {
            new() { Estado = "Activo", Total = 4 },
            new() { Estado = "Suspendido", Total = 3 }
        });
        unitOfWorkMock.Setup(u => u.PlanesNacionales.GetConteoPorVigenciaAsync()).ReturnsAsync(new List<MacroConteoVigenciaDto>
        {
            new() { PeriodoInicio = 2020, PeriodoFin = 2025, TotalPlanes = 4 },
            new() { PeriodoInicio = 2026, PeriodoFin = 2030, TotalPlanes = 3 }
        });

        var service = new MacroPlanNacionalService(unitOfWorkMock.Object);

        var result = await service.GetResumenAsync();

        result.TotalPlanes.Should().Be(7);
        result.TotalObjetivos.Should().Be(19);
        result.PlanesPorEstado.Should().ContainSingle(x => x.Estado == "Activo" && x.Total == 4);
        result.PlanesPorVigencia.Should().ContainSingle(x => x.PeriodoInicio == 2026 && x.PeriodoFin == 2030 && x.TotalPlanes == 3);
    }

    private static Mock<IMacroPlanificacionUnitOfWork> BuildUnitOfWork(
        PlanesNacionalesDesarrollo? planById = null,
        PlanesNacionalesDesarrollo? planWithObjectives = null)
    {
        var planesRepoMock = new Mock<IPlanesNacionalesDesarrolloRepository>();
        planesRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(planById);
        planesRepoMock.Setup(r => r.GetByIdWithObjetivosAsync(It.IsAny<int>())).ReturnsAsync(planWithObjectives);
        planesRepoMock.Setup(r => r.AddAsync(It.IsAny<PlanesNacionalesDesarrollo>())).Returns(Task.CompletedTask);
        planesRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PlanesNacionalesDesarrollo>())).Returns(Task.CompletedTask);
        planesRepoMock.Setup(r => r.RemoveAsync(It.IsAny<PlanesNacionalesDesarrollo>())).Returns(Task.CompletedTask);

        var objetivosRepoMock = new Mock<IObjetivosEstrategicoRepository>();
        objetivosRepoMock.Setup(r => r.AddAsync(It.IsAny<ObjetivosEstrategico>())).Returns(Task.CompletedTask);
        objetivosRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ObjetivosEstrategico>())).Returns(Task.CompletedTask);
        objetivosRepoMock.Setup(r => r.RemoveAsync(It.IsAny<ObjetivosEstrategico>())).Returns(Task.CompletedTask);

        var unitOfWorkMock = new Mock<IMacroPlanificacionUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.PlanesNacionales).Returns(planesRepoMock.Object);
        unitOfWorkMock.SetupGet(u => u.ObjetivosEstrategicos).Returns(objetivosRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        return unitOfWorkMock;
    }
}