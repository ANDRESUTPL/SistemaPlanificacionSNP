using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SistemaPlanificacionSNP.TestUtilities.Security;
using Testcontainers.MsSql;
using Xunit;

namespace SistemaPlanificacionSNP.TestUtilities.Infrastructure;

public abstract class MsSqlWebApplicationFactoryBase<TProgram, TDbContext> : WebApplicationFactory<TProgram>, IAsyncLifetime
	where TProgram : class
	where TDbContext : DbContext
{
	protected MsSqlContainer? MsSqlContainer { get; private set; }

	public async Task InitializeAsync()
	{
		MsSqlContainer = new MsSqlBuilder()
			.WithImage("mcr.microsoft.com/azure-sql-edge:latest")
			.WithPassword("Test!12345password") // <-- Contraseña maestra de SQL Server, NO la de tu usuario
			.WithEnvironment("ACCEPT_EULA", "Y")
			.Build();

		await MsSqlContainer.StartAsync();

		using var scope = Services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
		await dbContext.Database.EnsureCreatedAsync();

		await SeedAsync(scope.ServiceProvider);
	}

	async Task IAsyncLifetime.DisposeAsync()
	{
		await DisposeAsync();
	}

	public override async ValueTask DisposeAsync()
	{
		if (MsSqlContainer is not null)
		{
			await MsSqlContainer.DisposeAsync();
		}

		await base.DisposeAsync();
	}

	protected virtual Task SeedAsync(IServiceProvider serviceProvider)
	{
		return Task.CompletedTask;
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

		builder.ConfigureTestServices(services =>
		{
			services.RemoveAll<DbContextOptions<TDbContext>>();
			services.RemoveAll<TDbContext>();

			services.AddDbContext<TDbContext>(options =>
			{
				options.UseSqlServer(GetConnectionString());
			});

			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
				options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
			}).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
		});
	}

	protected string GetConnectionString()
	{
		return MsSqlContainer?.GetConnectionString()
			?? throw new InvalidOperationException("El contenedor SQL Server de pruebas no se ha inicializado.");
	}
}