using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.TestUtilities.Infrastructure;

namespace SistemaPlanificacionSNP.Parametrizacion.FunctionalTests.Infrastructure;

public sealed class ParametrizacionWebApplicationFactory : MsSqlWebApplicationFactoryBase<Program, ParametrizacionDbContext>
{
}
