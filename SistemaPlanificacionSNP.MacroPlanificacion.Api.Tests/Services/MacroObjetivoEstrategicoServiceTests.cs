using FluentAssertions;
using Moq;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.MacroPlanificacion.Api.Tests.Services;

public class MacroObjetivoEstrategicoServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateObjective_WhenPlanExistsAndCodigoIsUnique()
    {
        var plan = new PlanesNacionalesDesarrollo
        {
            PlanNacionalId = 10,
            Nombre = "Plan Nacional 2030",
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = "Activo"
        };

        var unitOfWorkMock = BuildUnitOfWork(planById: plan);
        var service = new MacroObjetivoEstrategicoService(unitOfWorkMock.Object);

        var dto = new MacroObjetivoEstrategicoCreateDto
        {
            PlanNacionalId = 10,
            Codigo = " OBJ-001 ",
            Nombre = " Objetivo estratégico ",
            Descripcion = " Descripción inicial "
        };

        var result = await service.CreateAsync(dto, "user-123");

        result.PlanNacionalId.Should().Be(10);
        result.Codigo.Should().Be("OBJ-001");
        result.Nombre.Should().Be("Objetivo estratégico");
        result.Descripcion.Should().Be("Descripción inicial");

        unitOfWorkMock.Verify(u => u.ObjetivosEstrategicos.AddAsync(It.Is<ObjetivosEstrategico>(x =>
            x.PlanNacionalId == 10 &&
            x.Codigo == "OBJ-001" &&
            x.Nombre == "Objetivo estratégico" &&
            x.Descripcion == "Descripción inicial")), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenPlanDoesNotExist()
    {
        var service = new MacroObjetivoEstrategicoService(BuildUnitOfWork().Object);
        var dto = new MacroObjetivoEstrategicoCreateDto
        {
            PlanNacionalId = 999,
            Codigo = "OBJ-001",
            Nombre = "Objetivo",
            Descripcion = null
        };

        var action = async () => await service.CreateAsync(dto, "user-123");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("El plan nacional no existe");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCodigoAlreadyExistsForPlan()
    {
        var plan = new PlanesNacionalesDesarrollo
        {
            PlanNacionalId = 10,
            Nombre = "Plan Nacional 2030",
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = "Activo"
        };

        var unitOfWorkMock = BuildUnitOfWork(planById: plan, duplicateObjective: new ObjetivosEstrategico
        {
            ObjetivoEstrategicoId = 1,
            PlanNacionalId = 10,
            Codigo = "OBJ-001",
            Nombre = "Objetivo existente"
        });

        var service = new MacroObjetivoEstrategicoService(unitOfWorkMock.Object);
        var dto = new MacroObjetivoEstrategicoCreateDto
        {
            PlanNacionalId = 10,
            Codigo = " OBJ-001 ",
            Nombre = "Nuevo objetivo",
            Descripcion = null
        };

        var action = async () => await service.CreateAsync(dto, "user-123");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Ya existe un objetivo con el mismo código para el plan nacional");
    }

    [Fact]
    public async Task UpdateAsync_ShouldApplyPartialChanges_AndPreservePlanId()
    {
        var existing = new ObjetivosEstrategico
        {
            ObjetivoEstrategicoId = 20,
            PlanNacionalId = 10,
            Codigo = "OBJ-001",
            Nombre = "Objetivo original",
            Descripcion = "Descripción original"
        };

        var unitOfWorkMock = BuildUnitOfWork(existingObjective: existing);
        unitOfWorkMock.Setup(u => u.ObjetivosEstrategicos.GetByCodigoAsync(10, "OBJ-002")).ReturnsAsync((ObjetivosEstrategico?)null);

        var service = new MacroObjetivoEstrategicoService(unitOfWorkMock.Object);
        var dto = new MacroObjetivoEstrategicoUpdateDto
        {
            Codigo = " OBJ-002 ",
            Nombre = null,
            Descripcion = " Nueva descripción "
        };

        var result = await service.UpdateAsync(20, dto, "user-123");

        result.Should().NotBeNull();
        result!.PlanNacionalId.Should().Be(10);
        result.Codigo.Should().Be("OBJ-002");
        result.Nombre.Should().Be("Objetivo original");
        result.Descripcion.Should().Be("Nueva descripción");

        unitOfWorkMock.Verify(u => u.ObjetivosEstrategicos.UpdateAsync(existing), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenDuplicateCodigoBelongsToAnotherObjective()
    {
        var existing = new ObjetivosEstrategico
        {
            ObjetivoEstrategicoId = 20,
            PlanNacionalId = 10,
            Codigo = "OBJ-001",
            Nombre = "Objetivo original"
        };

        var duplicate = new ObjetivosEstrategico
        {
            ObjetivoEstrategicoId = 21,
            PlanNacionalId = 10,
            Codigo = "OBJ-002",
            Nombre = "Objetivo duplicado"
        };

        var unitOfWorkMock = BuildUnitOfWork(existingObjective: existing, duplicateObjective: duplicate);
        var service = new MacroObjetivoEstrategicoService(unitOfWorkMock.Object);

        var dto = new MacroObjetivoEstrategicoUpdateDto
        {
            Codigo = "OBJ-002"
        };

        var action = async () => await service.UpdateAsync(20, dto, "user-123");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Ya existe un objetivo con el mismo código para el plan nacional");

        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenObjectiveDoesNotExist()
    {
        var service = new MacroObjetivoEstrategicoService(BuildUnitOfWork().Object);

        var result = await service.DeleteAsync(404, "user-123");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_ShouldNormalizePagingValuesBeforeRepositoryCall()
    {
        var query = new MacroObjetivoEstrategicoQueryDto
        {
            PageNumber = 0,
            PageSize = 1000,
            SortBy = "Codigo",
            SortDirection = "desc"
        };

        MacroObjetivoEstrategicoQueryDto? capturedQuery = null;
        var objetivosRepoMock = new Mock<IObjetivosEstrategicoRepository>();
        objetivosRepoMock.Setup(r => r.GetPagedAsync(It.IsAny<MacroObjetivoEstrategicoQueryDto>()))
            .Callback<MacroObjetivoEstrategicoQueryDto>(q => capturedQuery = q)
            .ReturnsAsync(new List<ObjetivosEstrategico>());
        objetivosRepoMock.Setup(r => r.CountFilteredAsync(It.IsAny<MacroObjetivoEstrategicoQueryDto>())).ReturnsAsync(0);

        var unitOfWorkMock = BuildUnitOfWork();
        unitOfWorkMock.SetupGet(u => u.ObjetivosEstrategicos).Returns(objetivosRepoMock.Object);

        var service = new MacroObjetivoEstrategicoService(unitOfWorkMock.Object);

        var result = await service.GetPagedAsync(query);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(100);
        capturedQuery.Should().NotBeNull();
        capturedQuery!.PageNumber.Should().Be(1);
        capturedQuery.PageSize.Should().Be(100);
    }

    private static Mock<IMacroPlanificacionUnitOfWork> BuildUnitOfWork(
        PlanesNacionalesDesarrollo? planById = null,
        ObjetivosEstrategico? existingObjective = null,
        ObjetivosEstrategico? duplicateObjective = null)
    {
        var planesRepoMock = new Mock<IPlanesNacionalesDesarrolloRepository>();
        planesRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(planById);
        planesRepoMock.Setup(r => r.AddAsync(It.IsAny<PlanesNacionalesDesarrollo>())).Returns(Task.CompletedTask);
        planesRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PlanesNacionalesDesarrollo>())).Returns(Task.CompletedTask);
        planesRepoMock.Setup(r => r.RemoveAsync(It.IsAny<PlanesNacionalesDesarrollo>())).Returns(Task.CompletedTask);

        var objetivosRepoMock = new Mock<IObjetivosEstrategicoRepository>();
        objetivosRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(existingObjective);
        objetivosRepoMock.Setup(r => r.GetByCodigoAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((int planNacionalId, string codigo) =>
                duplicateObjective != null &&
                duplicateObjective.PlanNacionalId == planNacionalId &&
                duplicateObjective.Codigo == codigo
                    ? duplicateObjective
                    : null);
        objetivosRepoMock.Setup(r => r.AddAsync(It.IsAny<ObjetivosEstrategico>())).Returns(Task.CompletedTask);
        objetivosRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ObjetivosEstrategico>())).Returns(Task.CompletedTask);
        objetivosRepoMock.Setup(r => r.RemoveAsync(It.IsAny<ObjetivosEstrategico>())).Returns(Task.CompletedTask);
        objetivosRepoMock.Setup(r => r.GetPagedAsync(It.IsAny<MacroObjetivoEstrategicoQueryDto>())).ReturnsAsync(new List<ObjetivosEstrategico>());
        objetivosRepoMock.Setup(r => r.CountFilteredAsync(It.IsAny<MacroObjetivoEstrategicoQueryDto>())).ReturnsAsync(0);

        var unitOfWorkMock = new Mock<IMacroPlanificacionUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.PlanesNacionales).Returns(planesRepoMock.Object);
        unitOfWorkMock.SetupGet(u => u.ObjetivosEstrategicos).Returns(objetivosRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        return unitOfWorkMock;
    }
}