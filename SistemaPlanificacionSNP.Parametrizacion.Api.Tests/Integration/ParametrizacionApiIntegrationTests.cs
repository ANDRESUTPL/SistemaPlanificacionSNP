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

namespace SistemaPlanificacionSNP.Parametrizacion.Api.Tests.Integration;

public class ParametrizacionApiIntegrationTests : IClassFixture<ParametrizacionApiWebApplicationFactory>
{
    private readonly ParametrizacionApiWebApplicationFactory _factory;

    public ParametrizacionApiIntegrationTests(ParametrizacionApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCatalogoItemPeriodoEntidad_ThenGetLists_WithBearerToken_ShouldSucceed()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await _factory.ResetDatabaseAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.GenerateJwtToken("user-123"));

        var createCatalogoPayload = new CatalogoCreateDto
        {
            Codigo = "TIPO_ENTIDAD_INT",
            Nombre = "Tipos de entidad integración",
            Descripcion = "Catálogo creado desde integración"
        };

        var createCatalogoResponse = await client.PostAsJsonAsync("/api/catalogos", createCatalogoPayload);
        createCatalogoResponse.EnsureSuccessStatusCode();

        var createdCatalogoBody = await createCatalogoResponse.Content.ReadFromJsonAsync<ApiResponse<CatalogoDto>>();
        createdCatalogoBody.Should().NotBeNull();
        createdCatalogoBody!.Success.Should().BeTrue();
        createdCatalogoBody.Data.Should().NotBeNull();
        createdCatalogoBody.Data!.CatalogoId.Should().BeGreaterThan(0);
        createdCatalogoBody.Data.Codigo.Should().Be("TIPO_ENTIDAD_INT");

        var createItemPayload = new ItemCatalogoCreateDto
        {
            CatalogoId = createdCatalogoBody.Data.CatalogoId,
            Codigo = "MIN",
            Nombre = "Ministerio",
            Descripcion = "Entidad tipo ministerio",
            Orden = 1
        };

        var createItemResponse = await client.PostAsJsonAsync("/api/catalogos/items", createItemPayload);
        createItemResponse.EnsureSuccessStatusCode();

        var createdItemBody = await createItemResponse.Content.ReadFromJsonAsync<ApiResponse<ItemCatalogoDto>>();
        createdItemBody.Should().NotBeNull();
        createdItemBody!.Success.Should().BeTrue();
        createdItemBody.Data.Should().NotBeNull();
        createdItemBody.Data!.CatalogoId.Should().Be(createdCatalogoBody.Data.CatalogoId);
        createdItemBody.Data.Codigo.Should().Be("MIN");

        var getCatalogoResponse = await client.GetAsync("/api/catalogos/TIPO_ENTIDAD_INT");
        getCatalogoResponse.EnsureSuccessStatusCode();

        var catalogoBody = await getCatalogoResponse.Content.ReadFromJsonAsync<ApiResponse<CatalogoDto>>();
        catalogoBody.Should().NotBeNull();
        catalogoBody!.Success.Should().BeTrue();
        catalogoBody.Data.Should().NotBeNull();
        catalogoBody.Data!.Items.Should().ContainSingle(x => x.Codigo == "MIN");

        var createPeriodoPayload = new PeriodoPlanificacionCreateUpdateDto
        {
            Codigo = "P2030",
            Nombre = "Periodo 2030",
            FechaInicio = new DateTime(2030, 1, 1),
            FechaFin = new DateTime(2030, 12, 31),
            Activo = true
        };

        var createPeriodoResponse = await client.PostAsJsonAsync("/api/instituciones/periodos", createPeriodoPayload);
        createPeriodoResponse.EnsureSuccessStatusCode();

        var createdPeriodoBody = await createPeriodoResponse.Content.ReadFromJsonAsync<ApiResponse<PeriodoPlanificacionDto>>();
        createdPeriodoBody.Should().NotBeNull();
        createdPeriodoBody!.Success.Should().BeTrue();
        createdPeriodoBody.Data.Should().NotBeNull();
        createdPeriodoBody.Data!.PeriodoPlanificacionId.Should().Be(1);

        var createEntidadPayload = new EntidadPublicaCreateUpdateDto
        {
            Nombre = "Ministerio de Integración",
            Sigla = "MI",
            Tipo = "Ministerio",
            NivelGobierno = "Nacional"
        };

        var createEntidadResponse = await client.PostAsJsonAsync("/api/instituciones/entidades", createEntidadPayload);
        createEntidadResponse.EnsureSuccessStatusCode();

        var createdEntidadBody = await createEntidadResponse.Content.ReadFromJsonAsync<ApiResponse<EntidadPublicaDto>>();
        createdEntidadBody.Should().NotBeNull();
        createdEntidadBody!.Success.Should().BeTrue();
        createdEntidadBody.Data.Should().NotBeNull();
        createdEntidadBody.Data!.EntidadPublicaId.Should().BeGreaterThan(0);
        createdEntidadBody.Data.Sigla.Should().Be("MI");

        var getEntidadesResponse = await client.GetAsync("/api/instituciones/entidades");
        getEntidadesResponse.EnsureSuccessStatusCode();

        var entidadesBody = await getEntidadesResponse.Content.ReadFromJsonAsync<ApiResponse<List<EntidadPublicaDto>>>();
        entidadesBody.Should().NotBeNull();
        entidadesBody!.Success.Should().BeTrue();
        entidadesBody.Data.Should().ContainSingle(x => x.Sigla == "MI");
    }

    [Fact]
    public async Task GetCatalogos_WithoutBearerToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await _factory.ResetDatabaseAsync();

        var response = await client.GetAsync("/api/catalogos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public sealed class ParametrizacionApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string JwtSecret = "parametrizacion-integration-secret-key-1234567890";
    private const string JwtIssuer = "ParametrizacionIntegrationIssuer";
    private const string JwtAudience = "ParametrizacionIntegrationAudience";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ParametrizacionApiWebApplicationFactory()
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
            services.RemoveAll<ParametrizacionDbContext>();
            services.RemoveAll<DbContextOptions<ParametrizacionDbContext>>();
            services.RemoveAll<IConfigureOptions<DbContextOptions<ParametrizacionDbContext>>>();

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
            services.AddDbContext<ParametrizacionDbContext>((provider, options) =>
            {
                options.UseSqlite(provider.GetRequiredService<SqliteConnection>());
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParametrizacionDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public string GenerateJwtToken(string actorId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, actorId),
            new(ClaimTypes.Name, "parametrizacion.integration"),
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
}