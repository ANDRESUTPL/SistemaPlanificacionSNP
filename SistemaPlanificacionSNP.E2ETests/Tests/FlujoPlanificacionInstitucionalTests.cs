using FluentAssertions;
using Microsoft.Playwright;
using SistemaPlanificacionSNP.E2ETests.Infrastructure;
using SistemaPlanificacionSNP.E2ETests.Pages;

namespace SistemaPlanificacionSNP.E2ETests.Tests
{
    public sealed class FlujoPlanificacionInstitucionalTests : E2ETestBase, IClassFixture<PlaywrightFixture>
    {
        public FlujoPlanificacionInstitucionalTests(PlaywrightFixture fixture) : base(fixture)
        {
        }

        [Fact]
        [Trait("Category", "E2E")]
        public async Task FlujoCompleto_DebeCrearPei_Y_FormularProyectoVinculado()
        {
            // Arrange
            var loginPage = new LoginPage(Page);
            var instPage = new InstitucionesPage(Page);
            var macroPage = new MacroPlanificacionPage(Page);
            var peiPage = new PlanificacionInstitucionalPage(Page);

            var uniqueId = Guid.NewGuid().ToString()[..5];
            var anio = DateTime.Now.Year + 3;

            var siglaEntidad = $"ENT-{uniqueId}";
            var nombreEntidad = $"Ministerio E2E {uniqueId}";
            var nombrePlanNacional = $"Plan Nacional E2E {uniqueId}";
            var cupProyecto = $"PRY-{uniqueId}";
            var nombreProyecto = $"Proyecto de Inversión E2E {uniqueId}";

            // Act 1: Login
            await loginPage.NavigateAsync(Settings.BaseUrl);
            await loginPage.LoginAsync(Settings.Credentials.Username, Settings.Credentials.Password);
            if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

            // Act 2: Prerrequisito - Crear Período y Entidad
            await instPage.NavegarAInstitucionesAsync(Settings.NavigationTimeoutMs);
            if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

            await instPage.CrearPeriodoAsync($"P-{anio}", $"Período {anio}", $"{anio}-01-01", $"{anio}-12-31");
            (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(5))).Should().BeTrue("Prerrequisito falló: No se pudo crear el Período");
            await loginPage.DismissNotificationIfPresentAsync();

            await instPage.IrACrearEntidadAsync();
            await instPage.CrearEntidadAsync(siglaEntidad, nombreEntidad, siglaEntidad, "Ministerio", "Gobierno Central");
            (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(5))).Should().BeTrue("Prerrequisito falló: No se pudo crear la Entidad");
            await loginPage.DismissNotificationIfPresentAsync();

            // Act 3: Prerrequisito - Crear Plan Nacional
            await macroPage.NavegarAPlanesNacionalesAsync(Settings.NavigationTimeoutMs);
            if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

            await macroPage.IrACrearPlanAsync();
            await macroPage.CrearPlanAsync(nombrePlanNacional);
            (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(5))).Should().BeTrue("Prerrequisito falló: No se pudo crear el Plan Nacional");
            await loginPage.DismissNotificationIfPresentAsync();

            // Act 4: Crear el PEI vinculando la Entidad y el Plan Nacional
            await peiPage.NavegarAPlanificacionInstitucionalAsync(Settings.NavigationTimeoutMs);
            if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

            await peiPage.IrACrearPeiAsync();
            await peiPage.CrearPeiVinculadoAsync(nombreEntidad, nombrePlanNacional);

            // Assert PEI
            var alertPei = await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(5));
            alertPei.Should().BeTrue("Debe mostrar alerta de éxito al crear el PEI");
            await loginPage.DismissNotificationIfPresentAsync();

            // Act 5: Ir al Detalle del PEI recién creado y formular Proyecto
            await peiPage.IrADetallePeiAsync(nombreEntidad);
            // Pasamos el monto como "150000" (sin decimales) para evitar errores de validación de cultura
            await peiPage.CrearProyectoAsync(cupProyecto, "150000", nombreProyecto);

            // Assert Proyecto
            var alertProyecto = await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(5));
            alertProyecto.Should().BeTrue("Debe mostrar alerta de éxito al formular el proyecto");
            await loginPage.DismissNotificationIfPresentAsync();

            // Verificar que el proyecto aparezca en la grilla
            var proyectoEnGrilla = Page.Locator($"td:has-text('{cupProyecto}')");
            await proyectoEnGrilla.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await proyectoEnGrilla.CountAsync()).Should().BeGreaterThan(0, "El proyecto recién creado debe listarse en la cartera del PEI");
        }
    }
}