using FluentAssertions;
using Moq;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.ControlCalidad.Api.Tests.Services;

public class AuditoriaControlCalidadServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateAuditoria_WhenDtoIsValid()
    {
        var revisionesRepoMock = new Mock<IRevisioneRepository>();
        revisionesRepoMock
            .Setup(r => r.GetByIdAsync(7))
            .ReturnsAsync(new Revisione
            {
                RevisionId = 7,
                CodigoRevision = "REV-007",
                Modulo = "ControlCalidad",
                Estado = "Pendiente",
                FechaRevision = DateTime.UtcNow
            });

        var auditoriasRepoMock = new Mock<IControlCalidadAuditoriaRepository>();
        auditoriasRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Auditoria>()))
            .Returns(Task.CompletedTask);

        var unitOfWorkMock = BuildUnitOfWork(revisionesRepoMock, auditoriasRepoMock);
        var service = new AuditoriaControlCalidadService(unitOfWorkMock.Object);

        var dto = new AuditoriaCreateDto
        {
            RevisionId = 7,
            Tipo = " Interna ",
            Resultado = " Conforme "
        };

        var result = await service.CreateAsync(dto, "qa.user");

        result.RevisionId.Should().Be(7);
        result.Tipo.Should().Be("Interna");
        result.Resultado.Should().Be("Conforme");
        result.Responsable.Should().Be("qa.user");
        result.FechaRegistro.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        auditoriasRepoMock.Verify(r => r.AddAsync(It.Is<Auditoria>(x =>
            x.RevisionId == 7 &&
            x.Tipo == "Interna" &&
            x.Resultado == "Conforme" &&
            x.Responsable == "qa.user")), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenRevisionDoesNotExist()
    {
        var revisionesRepoMock = new Mock<IRevisioneRepository>();
        revisionesRepoMock
            .Setup(r => r.GetByIdAsync(404))
            .ReturnsAsync((Revisione?)null);

        var service = new AuditoriaControlCalidadService(BuildUnitOfWork(revisionesRepoMock, new Mock<IControlCalidadAuditoriaRepository>()).Object);
        var dto = new AuditoriaCreateDto
        {
            RevisionId = 404,
            Tipo = "Interna",
            Resultado = "Conforme"
        };

        var action = async () => await service.CreateAsync(dto, "qa.user");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("La revisión asociada no existe");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenTipoIsInvalid()
    {
        var service = new AuditoriaControlCalidadService(BuildUnitOfWork(
            new Mock<IRevisioneRepository>(),
            new Mock<IControlCalidadAuditoriaRepository>()).Object);

        var dto = new AuditoriaCreateDto
        {
            RevisionId = 1,
            Tipo = "Legal",
            Resultado = "Conforme"
        };

        var action = async () => await service.CreateAsync(dto, "qa.user");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tipo de auditoría no permitido");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenResultadoIsInvalid()
    {
        var service = new AuditoriaControlCalidadService(BuildUnitOfWork(
            new Mock<IRevisioneRepository>(),
            new Mock<IControlCalidadAuditoriaRepository>()).Object);

        var dto = new AuditoriaCreateDto
        {
            RevisionId = 1,
            Tipo = "Interna",
            Resultado = "Parcial"
        };

        var action = async () => await service.CreateAsync(dto, "qa.user");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Resultado no permitido");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenResponsableIsMissing()
    {
        var service = new AuditoriaControlCalidadService(BuildUnitOfWork(
            new Mock<IRevisioneRepository>(),
            new Mock<IControlCalidadAuditoriaRepository>()).Object);

        var dto = new AuditoriaCreateDto
        {
            RevisionId = 1,
            Tipo = "Interna",
            Resultado = "Conforme"
        };

        var action = async () => await service.CreateAsync(dto, " ");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se pudo determinar el responsable desde los claims");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldNormalizePagingValues()
    {
        var query = new AuditoriaQueryDto
        {
            PageNumber = 0,
            PageSize = 1000,
            SortBy = "FechaRegistro",
            SortDirection = "desc"
        };

        var auditoriasRepoMock = new Mock<IControlCalidadAuditoriaRepository>();
        auditoriasRepoMock
            .Setup(r => r.GetPagedAsync(query))
            .ReturnsAsync(new List<Auditoria>());
        auditoriasRepoMock
            .Setup(r => r.CountFilteredAsync(query))
            .ReturnsAsync(0);

        var service = new AuditoriaControlCalidadService(BuildUnitOfWork(new Mock<IRevisioneRepository>(), auditoriasRepoMock).Object);

        var result = await service.GetPagedAsync(query);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetByRevisionIdAsync_ShouldReturnAuditoriasFromRepository()
    {
        var expected = new List<Auditoria>
        {
            new()
            {
                AuditoriaId = 1,
                RevisionId = 5,
                Tipo = "Interna",
                Resultado = "Conforme",
                Responsable = "qa.user",
                FechaRegistro = DateTime.UtcNow
            },
            new()
            {
                AuditoriaId = 2,
                RevisionId = 5,
                Tipo = "Seguimiento",
                Resultado = "No Conforme",
                Responsable = "qa.user",
                FechaRegistro = DateTime.UtcNow
            }
        };

        var auditoriasRepoMock = new Mock<IControlCalidadAuditoriaRepository>();
        auditoriasRepoMock
            .Setup(r => r.GetByRevisionIdAsync(5))
            .ReturnsAsync(expected);

        var service = new AuditoriaControlCalidadService(BuildUnitOfWork(new Mock<IRevisioneRepository>(), auditoriasRepoMock).Object);

        var result = await service.GetByRevisionIdAsync(5);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(x => x.Resultado == "Conforme");
        result.Should().ContainSingle(x => x.Resultado == "No Conforme");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenTipoIsInvalid()
    {
        var existing = new Auditoria
        {
            AuditoriaId = 10,
            RevisionId = 3,
            Tipo = "Interna",
            Resultado = "Conforme",
            Responsable = "qa.user",
            FechaRegistro = DateTime.UtcNow
        };

        var auditoriasRepoMock = new Mock<IControlCalidadAuditoriaRepository>();
        auditoriasRepoMock
            .Setup(r => r.GetByIdAsync(existing.AuditoriaId))
            .ReturnsAsync(existing);

        var service = new AuditoriaControlCalidadService(BuildUnitOfWork(new Mock<IRevisioneRepository>(), auditoriasRepoMock).Object);

        var dto = new AuditoriaUpdateDto { Tipo = "NoValido" };

        var action = async () => await service.UpdateAsync(existing.AuditoriaId, dto);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tipo de auditoría no permitido");
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenAuditoriaDoesNotExist()
    {
        var auditoriasRepoMock = new Mock<IControlCalidadAuditoriaRepository>();
        auditoriasRepoMock
            .Setup(r => r.GetByIdAsync(111))
            .ReturnsAsync((Auditoria?)null);

        var unitOfWorkMock = BuildUnitOfWork(new Mock<IRevisioneRepository>(), auditoriasRepoMock);
        var service = new AuditoriaControlCalidadService(unitOfWorkMock.Object);

        var result = await service.DeleteAsync(111);

        result.Should().BeFalse();
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
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
