using FluentAssertions;
using Microsoft.Playwright;
using SistemaPlanificacionSNP.E2ETests.Infrastructure;
using SistemaPlanificacionSNP.E2ETests.Pages;

namespace SistemaPlanificacionSNP.E2ETests.Tests
{
	public sealed class FlujoMacroPlanificacionTests : E2ETestBase, IClassFixture<PlaywrightFixture>
	{
		public FlujoMacroPlanificacionTests(PlaywrightFixture fixture)
			: base(fixture)
		{
		}

		[Fact]
		[Trait("Category", "E2E")]
		public async Task Login_Y_AccesoAMacroPlanificacion_DebeMostrarDirectorioDePlanesNacionales()
		{
			// Arrange
			var loginPage = new LoginPage(Page);

			// Act
			await loginPage.NavigateAsync(Settings.BaseUrl);
			await loginPage.LoginAsync(Settings.Credentials.Username, Settings.Credentials.Password);

			// 1. Hacemos clic en el menú padre para desplegar las opciones
			var menuPadre = Page.GetByText("Macro Planificación", new PageGetByTextOptions { Exact = false }).First;
			await menuPadre.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await menuPadre.ClickAsync();

			// 2. Ahora que está desplegado, buscamos el submenú real y le damos clic
			var macroPlanLink = Page.GetByText("Plan Nacional (PND)", new PageGetByTextOptions { Exact = false }).First;
			await macroPlanLink.WaitForAsync(new LocatorWaitForOptions
			{
				State = WaitForSelectorState.Visible,
				Timeout = Settings.NavigationTimeoutMs
			});
			await macroPlanLink.ClickAsync();

			// Wait explicitly for async UI notifications (SweetAlert2) before final DOM assertions.
			var notificationDisplayed = await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(5));
			if (notificationDisplayed)
			{
				var popup = Page.Locator(".swal2-container .swal2-popup:visible, .swal2-popup.swal2-toast:visible").First;
				(await popup.IsVisibleAsync()).Should().BeTrue("la notificación asíncrona debe mostrarse antes de continuar");
				await loginPage.DismissNotificationIfPresentAsync();
			}

			// Assert
			var directorioTitle = Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
			{
				Name = "Directorio de Planes Nacionales"
			});

			if (await directorioTitle.CountAsync() == 0)
			{
				directorioTitle = Page.Locator("h6:has-text('Directorio de Planes Nacionales')");
			}

			await directorioTitle.First.WaitForAsync(new LocatorWaitForOptions
			{
				State = WaitForSelectorState.Visible,
				Timeout = Settings.NavigationTimeoutMs
			});

			(await directorioTitle.First.IsVisibleAsync())
				.Should()
				.BeTrue("el usuario autenticado debe visualizar el directorio de planes nacionales");
		}

		[Fact]
		[Trait("Category", "E2E")]
		public async Task CrearPlanNacional_DebeGuardarExitosamente_Y_RedirigirAlIndex()
		{
			// Arrange
			var loginPage = new LoginPage(Page);
			var macroPage = new MacroPlanificacionPage(Page);
			var instPage = new InstitucionesPage(Page); // <-- Agregamos el POM de Instituciones
			var nombrePlanUnico = $"Plan E2E {Guid.NewGuid().ToString()[..5]}";
			var anio = DateTime.Now.Year + 2;

			// Act: 1. Login
			await loginPage.NavigateAsync(Settings.BaseUrl);
			await loginPage.LoginAsync(Settings.Credentials.Username, Settings.Credentials.Password);
			if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

			// Act: 2. Crear Período como Prerrequisito
			await instPage.NavegarAInstitucionesAsync(Settings.NavigationTimeoutMs);
			if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

			await instPage.CrearPeriodoAsync($"PRE-{anio}", $"Período Base {anio}", $"{anio}-01-01", $"{anio}-12-31");
			if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(5))) await loginPage.DismissNotificationIfPresentAsync();

			// Act: 3. Navegar a Macro Planificación
			await macroPage.NavegarAPlanesNacionalesAsync(Settings.NavigationTimeoutMs);
			if (await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(3))) await loginPage.DismissNotificationIfPresentAsync();

			// Act: 4. Ir a Crear y llenar formulario (ahora es 100% seguro que el combobox tendrá datos)
			await macroPage.IrACrearPlanAsync();
			await macroPage.CrearPlanAsync(nombrePlanUnico);

			// Assert: Comprobar SweetAlert
			var notificationDisplayed = await loginPage.WaitForAsyncNotificationAsync(TimeSpan.FromSeconds(5));
			notificationDisplayed.Should().BeTrue("El sistema debe emitir una alerta de éxito al guardar el plan");

			var successTitle = Page.Locator(".swal2-title");
			(await successTitle.TextContentAsync()).Should().Contain("Éxito");

			await loginPage.DismissNotificationIfPresentAsync();

			// Assert Final: Verificar grilla
			var planEnGrilla = Page.Locator($"td:has-text('{nombrePlanUnico}')");
			await planEnGrilla.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			(await planEnGrilla.CountAsync()).Should().BeGreaterThan(0, "El plan recién creado debe listarse en la tabla del directorio");
		}
	}
}