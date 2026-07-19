using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;

namespace SistemaPlanificacionSNP.MacroPlanificacion.Api.Tests.Integration;

public class MacroPlanificacionApiIntegrationTests : IClassFixture<MacroPlanificacionApiWebApplicationFactory>
{
    private readonly MacroPlanificacionApiWebApplicationFactory _factory;

    public MacroPlanificacionApiIntegrationTests(MacroPlanificacionApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePlan_ThenCreateObjective_ThenGetHierarchyAndResumen_WithBearerToken_ShouldSucceed()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await _factory.ResetDatabaseAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.GenerateJwtToken("user-123"));

        var createPlanPayload = new MacroPlanNacionalCreateDto
        {
            Nombre = "Plan Nacional Integracion",
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = "Activo"
        };

        var createPlanResponse = await client.PostAsJsonAsync("/api/planesNacionales/crear", createPlanPayload);
        createPlanResponse.EnsureSuccessStatusCode();

        var createdPlanBody = await createPlanResponse.Content.ReadFromJsonAsync<ApiResponse<MacroPlanNacionalDto>>();
        createdPlanBody.Should().NotBeNull();
        createdPlanBody!.Success.Should().BeTrue();
        createdPlanBody.Data.Should().NotBeNull();

        var planId = createdPlanBody.Data!.PlanNacionalId;
        planId.Should().BeGreaterThan(0);
        createdPlanBody.Data.Nombre.Should().Be(createPlanPayload.Nombre);

        var createObjectivePayload = new MacroObjetivoEstrategicoCreateDto
        {
            PlanNacionalId = planId,
            Codigo = "OBJ-INT-001",
            Nombre = "Objetivo Integracion",
            Descripcion = "Creado desde prueba E2E"
        };

        var createObjectiveResponse = await client.PostAsJsonAsync("/api/objetivosEstrategicos/crear", createObjectivePayload);
        createObjectiveResponse.EnsureSuccessStatusCode();

        var createdObjectiveBody = await createObjectiveResponse.Content.ReadFromJsonAsync<ApiResponse<MacroObjetivoEstrategicoDto>>();
        createdObjectiveBody.Should().NotBeNull();
        createdObjectiveBody!.Success.Should().BeTrue();
        createdObjectiveBody.Data.Should().NotBeNull();
        createdObjectiveBody.Data!.PlanNacionalId.Should().Be(planId);
        createdObjectiveBody.Data.Codigo.Should().Be("OBJ-INT-001");

        var hierarchyResponse = await client.GetAsync($"/api/planesNacionales/{planId}/jerarquia");
        hierarchyResponse.EnsureSuccessStatusCode();

        var hierarchyBody = await hierarchyResponse.Content.ReadFromJsonAsync<ApiResponse<MacroPlanNacionalDetalleDto>>();
        hierarchyBody.Should().NotBeNull();
        hierarchyBody!.Success.Should().BeTrue();
        hierarchyBody.Data.Should().NotBeNull();
        hierarchyBody.Data!.PlanNacionalId.Should().Be(planId);
        hierarchyBody.Data.Objetivos.Should().ContainSingle(x => x.Codigo == "OBJ-INT-001");

        var resumenResponse = await client.GetAsync("/api/planesNacionales/resumen");
        resumenResponse.EnsureSuccessStatusCode();

        var resumenBody = await resumenResponse.Content.ReadFromJsonAsync<ApiResponse<MacroPlanNacionalResumenDto>>();
        resumenBody.Should().NotBeNull();
        resumenBody!.Success.Should().BeTrue();
        resumenBody.Data.Should().NotBeNull();
        resumenBody.Data!.TotalPlanes.Should().Be(1);
        resumenBody.Data.TotalObjetivos.Should().Be(1);
        resumenBody.Data.PlanesPorEstado.Should().ContainSingle(x => x.Estado == "Activo" && x.Total == 1);
    }

    [Fact]
    public async Task GetPlans_WithoutBearerToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await _factory.ResetDatabaseAsync();

        var response = await client.GetAsync("/api/planesNacionales");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public sealed class MacroPlanificacionApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string JwtSecret = "macroplanificacion-integration-secret-key-1234567890";
    private const string JwtIssuer = "MacroPlanificacionIntegrationIssuer";
    private const string JwtAudience = "MacroPlanificacionIntegrationAudience";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public MacroPlanificacionApiWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                ["Jwt:SecretKey"] = JwtSecret,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:ExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            };

            configBuilder.AddInMemoryCollection(inMemorySettings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MacroPlanificacionDbContext>>();
            services.RemoveAll<IConfigureOptions<DbContextOptions<MacroPlanificacionDbContext>>>();

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = JwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddSingleton(_connection);
            services.AddDbContext<MacroPlanificacionDbContext, SqliteMacroPlanificacionDbContextForIntegration>((provider, options) =>
            {
                options.UseSqlite(provider.GetRequiredService<SqliteConnection>());
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MacroPlanificacionDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public string GenerateJwtToken(string actorId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, actorId),
            new(ClaimTypes.Name, "macro.integration"),
            new("role", "Administrador")
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class SqliteMacroPlanificacionDbContextForIntegration : MacroPlanificacionDbContext
    {
        public SqliteMacroPlanificacionDbContextForIntegration(DbContextOptions<MacroPlanificacionDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
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