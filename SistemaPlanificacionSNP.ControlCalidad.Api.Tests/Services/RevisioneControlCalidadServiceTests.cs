using FluentAssertions;
using Moq;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.ControlCalidad.Api.Tests.Services;

public class RevisioneControlCalidadServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateRevisionInPendienteState_WhenDtoIsValid()
    {
        var revisionesRepoMock = new Mock<IRevisioneRepository>();
        revisionesRepoMock
            .Setup(r => r.GetByCodigoAsync("REV-001"))
            .ReturnsAsync((Revisione?)null);

        revisionesRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Revisione>()))
            .Returns(Task.CompletedTask);

        var auditoriasRepoMock = new Mock<IControlCalidadAuditoriaRepository>();

        var unitOfWorkMock = BuildUnitOfWork(revisionesRepoMock, auditoriasRepoMock);
        unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new RevisioneControlCalidadService(unitOfWorkMock.Object);
        var dto = new RevisioneCreateDto
        {
            CodigoRevision = " REV-001 ",
            Modulo = " Planificacion ",
            Estado = " Pendiente ",
            Observaciones = " Revisión inicial "
        };

        var result = await service.CreateAsync(dto);

        result.CodigoRevision.Should().Be("REV-001");
        result.Modulo.Should().Be("Planificacion");
        result.Estado.Should().Be("Pendiente");
        result.Observaciones.Should().Be("Revisión inicial");
        result.FechaRevision.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        revisionesRepoMock.Verify(r => r.AddAsync(It.Is<Revisione>(x =>
            x.CodigoRevision == "REV-001" &&
            x.Modulo == "Planificacion" &&
            x.Estado == "Pendiente")), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenRevisionDoesNotExist()
    {
        var revisionesRepoMock = new Mock<IRevisioneRepository>();
        revisionesRepoMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Revisione?)null);

        var unitOfWorkMock = BuildUnitOfWork(revisionesRepoMock, new Mock<IControlCalidadAuditoriaRepository>());
        var service = new RevisioneControlCalidadService(unitOfWorkMock.Object);

        var result = await service.GetByIdAsync(999);

        result.Should().BeNull();
        revisionesRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldAggregateTotalsFromRepositories()
    {
        var revisionesRepoMock = new Mock<IRevisioneRepository>();
        revisionesRepoMock
            .Setup(r => r.CountFilteredAsync(It.IsAny<RevisioneQueryDto>()))
            .ReturnsAsync(15);
        revisionesRepoMock
            .Setup(r => r.CountByEstadoAsync("Pendiente"))
            .ReturnsAsync(6);
        revisionesRepoMock
            .Setup(r => r.CountByEstadoAsync("Aprobada"))
            .ReturnsAsync(5);
        revisionesRepoMock
            .Setup(r => r.CountByEstadoAsync("Rechazada"))
            .ReturnsAsync(4);

        var auditoriasRepoMock = new Mock<IControlCalidadAuditoriaRepository>();
        auditoriasRepoMock
            .Setup(r => r.CountAllAsync())
            .ReturnsAsync(9);
        auditoriasRepoMock
            .Setup(r => r.CountByResultadoAsync("Conforme"))
            .ReturnsAsync(7);
        auditoriasRepoMock
            .Setup(r => r.CountByResultadoAsync("No Conforme"))
            .ReturnsAsync(2);

        var unitOfWorkMock = BuildUnitOfWork(revisionesRepoMock, auditoriasRepoMock);
        var service = new RevisioneControlCalidadService(unitOfWorkMock.Object);

        var result = await service.GetDashboardAsync();

        result.TotalRevisiones.Should().Be(15);
        result.RevisionesPendientes.Should().Be(6);
        result.RevisionesAprobadas.Should().Be(5);
        result.RevisionesRechazadas.Should().Be(4);
        result.TotalAuditorias.Should().Be(9);
        result.AuditoriasConformes.Should().Be(7);
        result.AuditoriasNoConformes.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCodigoAlreadyExists()
    {
        var revisionesRepoMock = new Mock<IRevisioneRepository>();
        revisionesRepoMock
            .Setup(r => r.GetByCodigoAsync("REV-001"))
            .ReturnsAsync(new Revisione { RevisionId = 10, CodigoRevision = "REV-001", Modulo = "X", Estado = "Pendiente", FechaRevision = DateTime.UtcNow });

        var unitOfWorkMock = BuildUnitOfWork(revisionesRepoMock, new Mock<IControlCalidadAuditoriaRepository>());
        var service = new RevisioneControlCalidadService(unitOfWorkMock.Object);

        var dto = new RevisioneCreateDto
        {
            CodigoRevision = "REV-001",
            Modulo = "Planificacion",
            Estado = "Pendiente"
        };

        var action = async () => await service.CreateAsync(dto);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("El código de revisión ya existe");

        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenEstadoIsInvalid()
    {
        var service = new RevisioneControlCalidadService(BuildUnitOfWork(
            new Mock<IRevisioneRepository>(),
            new Mock<IControlCalidadAuditoriaRepository>()).Object);

        var dto = new RevisioneCreateDto
        {
            CodigoRevision = "REV-777",
            Modulo = "Planificacion",
            Estado = "En Proceso"
        };

        var action = async () => await service.CreateAsync(dto);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Estado no permitido");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenFechaRevisionIsInFuture()
    {
        var service = new RevisioneControlCalidadService(BuildUnitOfWork(
            new Mock<IRevisioneRepository>(),
            new Mock<IControlCalidadAuditoriaRepository>()).Object);

        var dto = new RevisioneCreateDto
        {
            CodigoRevision = "REV-888",
            Modulo = "Planificacion",
            Estado = "Pendiente",
            FechaRevision = DateTime.UtcNow.AddHours(1)
        };

        var action = async () => await service.CreateAsync(dto);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("La fecha de revisión no puede estar en el futuro");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldNormalizePagingValues()
    {
        var query = new RevisioneQueryDto
        {
            PageNumber = 0,
            PageSize = 500,
            SortBy = "FechaRevision",
            SortDirection = "desc"
        };

        var revisionesRepoMock = new Mock<IRevisioneRepository>();
        revisionesRepoMock
            .Setup(r => r.GetPagedAsync(query))
            .ReturnsAsync(new List<Revisione>());
        revisionesRepoMock
            .Setup(r => r.CountFilteredAsync(query))
            .ReturnsAsync(0);

        var service = new RevisioneControlCalidadService(BuildUnitOfWork(revisionesRepoMock, new Mock<IControlCalidadAuditoriaRepository>()).Object);

        var result = await service.GetPagedAsync(query);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenEstadoIsInvalid()
    {
        var existing = new Revisione
        {
            RevisionId = 4,
            CodigoRevision = "REV-004",
            Modulo = "Planificacion",
            Estado = "Pendiente",
            FechaRevision = DateTime.UtcNow
        };

        var revisionesRepoMock = new Mock<IRevisioneRepository>();
        revisionesRepoMock
            .Setup(r => r.GetByIdAsync(existing.RevisionId))
            .ReturnsAsync(existing);

        var service = new RevisioneControlCalidadService(BuildUnitOfWork(revisionesRepoMock, new Mock<IControlCalidadAuditoriaRepository>()).Object);

        var dto = new RevisioneUpdateDto
        {
            Estado = "Invalido"
        };

        var action = async () => await service.UpdateAsync(existing.RevisionId, dto);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Estado no permitido");
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenRevisionHasAuditorias()
    {
        var existing = new Revisione
        {
            RevisionId = 8,
            CodigoRevision = "REV-008",
            Modulo = "Control",
            Estado = "Pendiente",
            FechaRevision = DateTime.UtcNow,
            Auditoria = new List<Auditoria>
            {
                new()
                {
                    AuditoriaId = 1,
                    RevisionId = 8,
                    Tipo = "Interna",
                    Resultado = "Conforme",
                    Responsable = "qa.user",
                    FechaRegistro = DateTime.UtcNow
                }
            }
        };

        var revisionesRepoMock = new Mock<IRevisioneRepository>();
        revisionesRepoMock
            .Setup(r => r.GetByIdWithAuditoriasAsync(existing.RevisionId))
            .ReturnsAsync(existing);

        var service = new RevisioneControlCalidadService(BuildUnitOfWork(revisionesRepoMock, new Mock<IControlCalidadAuditoriaRepository>()).Object);

        var action = async () => await service.DeleteAsync(existing.RevisionId);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se puede eliminar una revisión con auditorías asociadas");
    }

    private static Mock<IControlCalidadUnitOfWork> BuildUnitOfWork(
        Mock<IRevisioneRepository> revisionesRepoMock,
        Mock<IControlCalidadAuditoriaRepository> auditoriasRepoMock)
    {
        var unitOfWorkMock = new Mock<IControlCalidadUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Revisiones).Returns(revisionesRepoMock.Object);
        unitOfWorkMock.SetupGet(u => u.AuditoriasControlCalidad).Returns(auditoriasRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        return unitOfWorkMock;
    }
}
