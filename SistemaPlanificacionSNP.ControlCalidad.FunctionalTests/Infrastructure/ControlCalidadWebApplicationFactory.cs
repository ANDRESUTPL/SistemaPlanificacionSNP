using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.TestUtilities.Infrastructure;

namespace SistemaPlanificacionSNP.ControlCalidad.FunctionalTests.Infrastructure;

public sealed class ControlCalidadWebApplicationFactory : MsSqlWebApplicationFactoryBase<Program, ControlCalidadDbContext>
{
}
