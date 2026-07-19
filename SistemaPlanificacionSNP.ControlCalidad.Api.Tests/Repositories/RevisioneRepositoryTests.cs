using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;

namespace SistemaPlanificacionSNP.ControlCalidad.Api.Tests.Repositories;

public class RevisioneRepositoryTests
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

        context.Revisiones.AddRange(
            BuildRevision("REV-001", "Planificacion", "Pendiente", DateTime.UtcNow.AddDays(-3)),
            BuildRevision("REV-002", "Planificacion", "Pendiente", DateTime.UtcNow.AddDays(-2)),
            BuildRevision("REV-003", "Planificacion", "Aprobada", DateTime.UtcNow.AddDays(-1))
        );
        await context.SaveChangesAsync();

        var repository = new RevisioneRepository(context);
        var query = new RevisioneQueryDto
        {
            Estado = "Pendiente",
            Modulo = "Planificacion",
            PageNumber = 1,
            PageSize = 1,
            SortBy = "codigorevision",
            SortDirection = "asc"
        };

        var result = await repository.GetPagedAsync(query);
        var total = await repository.CountFilteredAsync(query);

        result.Should().HaveCount(1);
        result[0].CodigoRevision.Should().Be("REV-001");
        total.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdWithAuditoriasAsync_ShouldIncludeRelatedAuditorias()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ControlCalidadDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SqliteControlCalidadDbContextForTests(options);
        await context.Database.EnsureCreatedAsync();

        var revision = BuildRevision("REV-010", "Control", "Pendiente", DateTime.UtcNow);
        context.Revisiones.Add(revision);
        await context.SaveChangesAsync();

        context.Auditorias.AddRange(
            new Auditoria
            {
                RevisionId = revision.RevisionId,
                Tipo = "Interna",
                Resultado = "Conforme",
                Responsable = "qa.user",
                FechaRegistro = DateTime.UtcNow.AddMinutes(-5)
            },
            new Auditoria
            {
                RevisionId = revision.RevisionId,
                Tipo = "Seguimiento",
                Resultado = "No Conforme",
                Responsable = "qa.user",
                FechaRegistro = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        var repository = new RevisioneRepository(context);

        var result = await repository.GetByIdWithAuditoriasAsync(revision.RevisionId);

        result.Should().NotBeNull();
        result!.Auditoria.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByCodigoAsync_ShouldReturnMatchingRevision()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ControlCalidadDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SqliteControlCalidadDbContextForTests(options);
        await context.Database.EnsureCreatedAsync();

        var revision = BuildRevision("REV-COD", "Control", "Rechazada", DateTime.UtcNow);
        context.Revisiones.Add(revision);
        await context.SaveChangesAsync();

        var repository = new RevisioneRepository(context);

        var result = await repository.GetByCodigoAsync("REV-COD");

        result.Should().NotBeNull();
        result!.Estado.Should().Be("Rechazada");
    }

    [Fact]
    public async Task CountByEstadoAsync_ShouldCountOnlyMatchingStatus()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ControlCalidadDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SqliteControlCalidadDbContextForTests(options);
        await context.Database.EnsureCreatedAsync();

        context.Revisiones.AddRange(
            BuildRevision("REV-100", "M1", "Pendiente", DateTime.UtcNow),
            BuildRevision("REV-101", "M1", "Pendiente", DateTime.UtcNow),
            BuildRevision("REV-102", "M1", "Aprobada", DateTime.UtcNow)
        );
        await context.SaveChangesAsync();

        var repository = new RevisioneRepository(context);

        var result = await repository.CountByEstadoAsync("Pendiente");

        result.Should().Be(2);
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

        var repository = new RevisioneRepository(context);
        var revision = BuildRevision("REV-CRUD", "Inicial", "Pendiente", DateTime.UtcNow);

        await repository.AddAsync(revision);
        await context.SaveChangesAsync();

        var created = await repository.GetByCodigoAsync("REV-CRUD");
        created.Should().NotBeNull();

        created!.Modulo = "Actualizado";
        await repository.UpdateAsync(created);
        await context.SaveChangesAsync();

        var updated = await repository.GetByIdAsync(created.RevisionId);
        updated.Should().NotBeNull();
        updated!.Modulo.Should().Be("Actualizado");

        await repository.RemoveAsync(updated);
        await context.SaveChangesAsync();

        var deleted = await repository.GetByIdAsync(created.RevisionId);
        deleted.Should().BeNull();
    }

    private static Revisione BuildRevision(string codigo, string modulo, string estado, DateTime fecha)
    {
        return new Revisione
        {
            CodigoRevision = codigo,
            Modulo = modulo,
            Estado = estado,
            FechaRevision = fecha,
            Observaciones = "Obs"
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
