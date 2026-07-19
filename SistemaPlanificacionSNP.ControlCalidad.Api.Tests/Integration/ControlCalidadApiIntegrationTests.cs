using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;

namespace SistemaPlanificacionSNP.ControlCalidad.Api.Tests.Integration;

public class ControlCalidadApiIntegrationTests : IClassFixture<ControlCalidadApiWebApplicationFactory>
{
    private readonly ControlCalidadApiWebApplicationFactory _factory;

    public ControlCalidadApiIntegrationTests(ControlCalidadApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateRevision_ThenGetById_ThenCreateAuditoria_WithBearerToken_ShouldSucceed()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await _factory.EnsureDatabaseCreatedAsync();

        var accessToken = _factory.GenerateJwtToken("Ana", "QA", "ana.qa");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createRevisionPayload = new RevisioneCreateDto
        {
            CodigoRevision = $"REV-INT-{Guid.NewGuid():N}"[..16],
            Modulo = "Planificacion",
            Estado = "Pendiente",
            Observaciones = "Integracion E2E"
        };

        var createRevisionResponse = await client.PostAsJsonAsync("/api/revisiones/crear", createRevisionPayload);
        createRevisionResponse.EnsureSuccessStatusCode();

        var createdRevisionBody = await createRevisionResponse.Content.ReadFromJsonAsync<ApiResponse<RevisioneDto>>();
        createdRevisionBody.Should().NotBeNull();
        createdRevisionBody!.Success.Should().BeTrue();
        createdRevisionBody.Data.Should().NotBeNull();

        var revisionId = createdRevisionBody.Data!.RevisionId;
        revisionId.Should().BeGreaterThan(0);

        var getRevisionResponse = await client.GetAsync($"/api/revisiones/{revisionId}");
        getRevisionResponse.EnsureSuccessStatusCode();

        var getRevisionBody = await getRevisionResponse.Content.ReadFromJsonAsync<ApiResponse<RevisioneDto>>();
        getRevisionBody.Should().NotBeNull();
        getRevisionBody!.Success.Should().BeTrue();
        getRevisionBody.Data.Should().NotBeNull();
        getRevisionBody.Data!.CodigoRevision.Should().Be(createRevisionPayload.CodigoRevision);

        var createAuditoriaPayload = new AuditoriaCreateDto
        {
            RevisionId = revisionId,
            Tipo = "Interna",
            Resultado = "Conforme"
        };

        var createAuditoriaResponse = await client.PostAsJsonAsync("/api/auditorias/crear", createAuditoriaPayload);
        createAuditoriaResponse.EnsureSuccessStatusCode();

        var createdAuditoriaBody = await createAuditoriaResponse.Content.ReadFromJsonAsync<ApiResponse<AuditoriaDto>>();
        createdAuditoriaBody.Should().NotBeNull();
        createdAuditoriaBody!.Success.Should().BeTrue();
        createdAuditoriaBody.Data.Should().NotBeNull();
        createdAuditoriaBody.Data!.RevisionId.Should().Be(revisionId);
        createdAuditoriaBody.Data.Responsable.Should().Be("Ana QA");
    }
}

public sealed class ControlCalidadApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private const string JwtSecret = "controlcalidad-integration-secret-key-1234567890";
    private const string JwtIssuer = "ControlCalidadIntegrationIssuer";
    private const string JwtAudience = "ControlCalidadIntegrationAudience";

    public ControlCalidadApiWebApplicationFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ControlCalidadApiIntegration_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
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
            services.RemoveAll<DbContextOptions<ControlCalidadDbContext>>();
            services.RemoveAll<IConfigureOptions<DbContextOptions<ControlCalidadDbContext>>>();

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

            services.AddDbContext<ControlCalidadDbContext>(options =>
            {
                options.UseSqlite(_connectionString);
            });
        });
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ControlCalidadDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public string GenerateJwtToken(string nombre, string apellido, string username)
    {
        var claims = new List<Claim>
        {
            new("Nombre", nombre),
            new("Apellido", apellido),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.NameIdentifier, "1"),
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
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch (IOException)
            {
                // Best effort cleanup for temp integration DB file.
            }
        }

        base.Dispose(disposing);
    }
}
