using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;

namespace SistemaPlanificacionSNP.ControlCalidad.Api.Tests.Repositories;

public class ControlCalidadAuditoriaRepositoryTests
{
    [Fact]
    public async Task GetPagedAsync_ShouldReturnFilteredAndPagedResults()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ControlCalidadDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SqliteControlCalidadDbContextForTests(options);
        await context.Database.EnsureCreatedAsync();

        var revision = BuildRevision("REV-AUD-1");
        context.Revisiones.Add(revision);
        await context.SaveChangesAsync();

        context.Auditorias.AddRange(
            BuildAuditoria(revision.RevisionId, "Interna", "Conforme", "ana", DateTime.UtcNow.AddDays(-3)),
            BuildAuditoria(revision.RevisionId, "Interna", "Conforme", "ana", DateTime.UtcNow.AddDays(-2)),
            BuildAuditoria(revision.RevisionId, "Externa", "No Conforme", "luis", DateTime.UtcNow.AddDays(-1))
        );
        await context.SaveChangesAsync();

        var repository = new ControlCalidadAuditoriaRepository(context);
        var query = new AuditoriaQueryDto
        {
            RevisionId = revision.RevisionId,
            Tipo = "Interna",
            Resultado = "Conforme",
            Responsable = "ana",
            PageNumber = 1,
            PageSize = 1,
            SortBy = "fecharegistro",
            SortDirection = "asc"
        };

        var result = await repository.GetPagedAsync(query);
        var total = await repository.CountFilteredAsync(query);

        result.Should().HaveCount(1);
        result[0].Tipo.Should().Be("Interna");
        result[0].Resultado.Should().Be("Conforme");
        total.Should().Be(2);
    }

    [Fact]
    public async Task GetByRevisionIdAsync_ShouldReturnDescendingByFechaRegistro()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ControlCalidadDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SqliteControlCalidadDbContextForTests(options);
        await context.Database.EnsureCreatedAsync();

        var revision = BuildRevision("REV-AUD-2");
        context.Revisiones.Add(revision);
        await context.SaveChangesAsync();

        context.Auditorias.AddRange(
            BuildAuditoria(revision.RevisionId, "Interna", "Conforme", "ana", DateTime.UtcNow.AddHours(-2)),
            BuildAuditoria(revision.RevisionId, "Interna", "No Conforme", "ana", DateTime.UtcNow.AddHours(-1))
        );
        await context.SaveChangesAsync();

        var repository = new ControlCalidadAuditoriaRepository(context);

        var result = await repository.GetByRevisionIdAsync(revision.RevisionId);

        result.Should().HaveCount(2);
        result[0].FechaRegistro.Should().BeAfter(result[1].FechaRegistro);
    }

    [Fact]
    public async Task CountMethods_ShouldReturnCorrectValues()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ControlCalidadDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SqliteControlCalidadDbContextForTests(options);
        await context.Database.EnsureCreatedAsync();

        var revision = BuildRevision("REV-AUD-3");
        context.Revisiones.Add(revision);
        await context.SaveChangesAsync();

        context.Auditorias.AddRange(
            BuildAuditoria(revision.RevisionId, "Interna", "Conforme", "ana", DateTime.UtcNow),
            BuildAuditoria(revision.RevisionId, "Externa", "No Conforme", "luis", DateTime.UtcNow),
            BuildAuditoria(revision.RevisionId, "Seguimiento", "Conforme", "ana", DateTime.UtcNow)
        );
        await context.SaveChangesAsync();

        var repository = new ControlCalidadAuditoriaRepository(context);

        var total = await repository.CountAllAsync();
        var conformes = await repository.CountByResultadoAsync("Conforme");

        total.Should().Be(3);
        conformes.Should().Be(2);
    }

    [Fact]
    public async Task AddUpdateRemove_ShouldPersistExpectedChanges()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ControlCalidadDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SqliteControlCalidadDbContextForTests(options);
        await context.Database.EnsureCreatedAsync();

        var revision = BuildRevision("REV-AUD-CRUD");
        context.Revisiones.Add(revision);
        await context.SaveChangesAsync();

        var repository = new ControlCalidadAuditoriaRepository(context);
        var auditoria = BuildAuditoria(revision.RevisionId, "Interna", "Conforme", "ana", DateTime.UtcNow);

        await repository.AddAsync(auditoria);
        await context.SaveChangesAsync();

        var created = await repository.GetByIdAsync(auditoria.AuditoriaId);
        created.Should().NotBeNull();

        created!.Resultado = "No Conforme";
        await repository.UpdateAsync(created);
        await context.SaveChangesAsync();

        var updated = await repository.GetByIdAsync(auditoria.AuditoriaId);
        updated.Should().NotBeNull();
        updated!.Resultado.Should().Be("No Conforme");

        await repository.RemoveAsync(updated);
        await context.SaveChangesAsync();

        var deleted = await repository.GetByIdAsync(auditoria.AuditoriaId);
        deleted.Should().BeNull();
    }

    private static Revisione BuildRevision(string codigo)
    {
        return new Revisione
        {
            CodigoRevision = codigo,
            Modulo = "ControlCalidad",
            Estado = "Pendiente",
            FechaRevision = DateTime.UtcNow,
            Observaciones = "Obs"
        };
    }

    private static Auditoria BuildAuditoria(int revisionId, string tipo, string resultado, string responsable, DateTime fechaRegistro)
    {
        return new Auditoria
        {
            RevisionId = revisionId,
            Tipo = tipo,
            Resultado = resultado,
            Responsable = responsable,
            FechaRegistro = fechaRegistro
        };
    }

    private sealed class SqliteControlCalidadDbContextForTests : ControlCalidadDbContext
    {
        public SqliteControlCalidadDbContextForTests(DbContextOptions<ControlCalidadDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (!string.IsNullOrWhiteSpace(property.GetDefaultValueSql()))
                    {
                        property.SetDefaultValueSql(null);
                    }
                }
            }
        }
    }
}
