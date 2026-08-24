using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using SistemaPlanificacionSNP.TestUtilities.Infrastructure;

namespace SistemaPlanificacionSNP.PlanificacionInstitucional.FunctionalTests.Infrastructure;

public class PlanificacionInstitucionalWebApplicationFactory : MsSqlWebApplicationFactoryBase<SistemaPlanificacionSNP.PlanificacionInstitucional.Api.Program, PlanificacionInstitucionalDbContext>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<MacroPlanificacionDbContext>>();
            services.RemoveAll<MacroPlanificacionDbContext>();
            services.AddDbContext<MacroPlanificacionDbContext>(options => options.UseSqlServer(GetConnectionString()));
        });
    }

    protected override async Task SeedAsync(IServiceProvider serviceProvider)
    {
        await base.SeedAsync(serviceProvider);
        var macroContext = serviceProvider.GetRequiredService<MacroPlanificacionDbContext>();
        await macroContext.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
        macroContext.PlanesNacionalesDesarrollos.Add(new PlanesNacionalesDesarrollo
        {
            PlanNacionalId = 1,
            Nombre = "Plan Nacional de Prueba",
            PeriodoPlanificacionId = 1,
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = "Activo",
            FechaCreacion = DateTime.UtcNow
        });

        await using (var transaction = await macroContext.Database.BeginTransactionAsync())
        {
            await macroContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [PlanesNacionalesDesarrollo] ON");
            await macroContext.SaveChangesAsync();
            await macroContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [PlanesNacionalesDesarrollo] OFF");
            await transaction.CommitAsync();
        }
    }
}
